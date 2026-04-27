# QA Evidence - Codex Verification Sweep

Date: 2026-04-26

This file is a handoff for Claude or any QA agent. Do not treat this as permission to edit app source. Fixes still need Joseph approval or an explicit implementation request.

## Screenshot Folder

All screenshots copied here so Claude can view them without hunting through Playwright output:

- `docs/qa-evidence/QA_AUDIT_20260426_verify/revenue-wait.png`
- `docs/qa-evidence/QA_AUDIT_20260426_verify/estimates-wait.png`
- `docs/qa-evidence/QA_AUDIT_20260426_verify/new-estimate-wait.png`
- `docs/qa-evidence/QA_AUDIT_20260426_verify/staffing-wait.png`

Original Playwright output is also still under:

- `webapp/test-results/qa/QA_AUDIT_20260426_verify/`

## Build Evidence

- `dotnet build --no-restore --configuration Release` passed.
  - Warning: `NU1903` AutoMapper 14.0.0 high severity advisory.
  - Warning: NSwag post-build command resolved as `run ../Api/nswag.json...` and exited 9009, but MSBuild still reported success.
- `npm.cmd run build:dev` passed.

## Finding 1 - Revenue Forecast Avg Margin / Future Profit Still Wrong

Status: FAIL / OPEN

Evidence:

- Screenshot: `docs/qa-evidence/QA_AUDIT_20260426_verify/revenue-wait.png`
- Visible values:
  - Awarded Revenue: `$3.1M`
  - Future Revenue: `$7.2M`
  - AVG MARGIN: `0%`
  - FUTURE PROFIT: `$0`

What is wrong:

The dashboard has real awarded revenue and future revenue, but margin remains zero. Future profit then multiplies future revenue by zero, so it also shows zero.

Why it is wrong:

The frontend still reads the wrong response shape:

- Frontend reads: `analyticsResp.data?.avgMarginPct`
- API returns: `data.kpis.avgMarginPct`

Likely files:

- `webapp/src/modules/estimating/features/analytics/views/RevenueForecastView.vue`
- `Api/Controllers/AnalyticsController.cs`

How to fix:

Read `analyticsResp.data?.kpis?.avgMarginPct` instead of `analyticsResp.data?.avgMarginPct`. Add a defensive fallback only after checking the nested value.

Regression test:

Open Revenue Forecast for 2026. If Awarded Revenue is non-zero, AVG MARGIN must not silently remain `0%` unless the underlying awarded estimates truly have zero margin. FUTURE PROFIT must equal Future Revenue multiplied by AVG MARGIN.

## Finding 2 - Submit For Approval Status Contract Still Mismatched

Status: FAIL / OPEN

Evidence:

- Screenshot: `docs/qa-evidence/QA_AUDIT_20260426_verify/new-estimate-wait.png`
- Positive evidence: New estimate form now shows a read-only `DRAFT` badge and no editable estimate status dropdown.
- Code evidence:
  - Frontend submit flow sends `Submitted for Approval`.
  - Backend `PatchStatus` accepts `Submitted`, not `Submitted for Approval`.

What is wrong:

The UI can look correct and still fail when the user clicks Submit for Approval because the final status PATCH sends a value the API rejects.

Why it is wrong:

The app is mixing display labels with persisted status values. The recommended strategy is:

- Store/API value: `Submitted`
- UI display label: `Submitted for Approval`

Likely files:

- `Api/Controllers/EstimatesController.cs`
- `webapp/src/modules/estimating/features/estimate-form/views/EstimateFormView.vue`
- `webapp/src/modules/estimating/features/estimate-list/views/EstimateListView.vue`
- `webapp/src/modules/estimating/features/analytics/utils/forecast.ts`
- `webapp/src/modules/estimating/stores/aiChatStore.ts`

How to fix:

Normalize the workflow value. Either make the frontend PATCH `Submitted`, or make the API accept `Submitted for Approval` and normalize it to `Submitted` before storing. Also update all reporting/forecast/AI status normalization so submitted estimates are counted as pipeline.

Regression test:

Create or open a Draft estimate, click Submit for Approval, confirm the PDF/review step, then verify:

- API returns success.
- Header badge displays Submitted for Approval.
- Estimate list row displays submitted styling.
- Revenue Forecast counts it as pipeline.
- Manpower Forecast includes submitted estimates when pending estimates are included.

## Finding 3 - Lost Reason Not Enforced Server-Side

Status: FAIL / OPEN

Evidence:

- Backend `PatchStatus` only copies `LostReason` if supplied.
- It does not reject `Status = Lost` with an empty or missing reason.

What is wrong:

The UI dialog requires a reason, but API/AI can still bypass that and mark an estimate Lost with no reason.

Why it is wrong:

Lost reason is audit-critical. If one path can bypass it, the reports and historical win/loss analysis lose business context.

Likely files:

- `Api/Controllers/EstimatesController.cs`
- `webapp/src/modules/estimating/stores/aiChatStore.ts`
- `Api/Services/AiService.cs`

How to fix:

In backend `PatchStatus`, if normalized status is `Lost`, reject the request unless `LostReason` is non-empty. AI and UI should pass the same required reason.

Regression test:

Call PATCH status Lost without a reason and verify 400. Call again with a reason and verify success.

## Finding 4 - Revenue KPI / Chart Definitions Still Do Not Reconcile

Status: FAIL / OPEN

Evidence:

- Screenshot: `docs/qa-evidence/QA_AUDIT_20260426_verify/revenue-wait.png`
- Positive evidence: separate Monthly/Quarterly/Cumulative chart controls are gone.
- Positive evidence: chart is controlled by the top period selector.
- Code evidence: KPI Pending uses raw estimate totals, while chart Pipeline uses confidence-weighted `pipelineAmount`. Chart also does not display staffing amount as part of the visible pipeline line.

What is wrong:

The dashboard can still tell two different financial stories on the same page.

Why it is wrong:

Users will compare KPI cards against chart lines. If both are labeled as pipeline but one is raw and one is weighted or excludes staffing, they will not reconcile.

Likely files:

- `webapp/src/modules/estimating/features/analytics/views/RevenueForecastView.vue`
- `webapp/src/modules/estimating/features/analytics/utils/forecast.ts`

How to fix:

Pick explicit categories and labels:

- Raw Pending Pipeline
- Weighted Pipeline
- Future Staffing Plan Revenue

Either make the chart use the same raw values as the KPI cards, or rename the chart datasets so the difference is obvious and drillable.

Regression test:

For each top period filter, record Awarded/Pending/Lost KPI values and sum the matching chart series. Any difference must be explained by label and visible UI, not hidden code logic.

## Finding 5 - Staffing Plan Submit Flow Still Allows Manual Status Editing

Status: OPEN

Evidence:

- Screenshot: `docs/qa-evidence/QA_AUDIT_20260426_verify/staffing-wait.png` verifies the staffing list loads.
- Code evidence: staffing form still renders an editable `STATUS` dropdown with `Draft`, `Active`, `Approved`.

What is wrong:

Staffing plans now have Submit for Approval, but the form still exposes a status dropdown that can bypass the intended workflow.

Why it is wrong:

Same business rule as estimates: status should be the result of an action, not manual data entry.

Likely files:

- `webapp/src/modules/estimating/features/staffing-form/views/StaffingFormView.vue`
- `Api/Controllers/StaffingPlansController.cs`

How to fix:

Make the staffing plan status a read-only badge. Keep Submit for Approval as the action. Validate the backend status PATCH instead of accepting any string.

Regression test:

Open saved Draft staffing plan. Status is visible as a badge only. Submit for Approval changes it through the action. No editable status dropdown exists.

## Finding 6 - Expired Unconverted Staffing Plans Are Not Flagged

Status: OPEN

Evidence:

- No stale/expired detection was found in staffing list or forecast logic.
- Revenue Forecast still counts non-converted staffing plans in Future Revenue without checking whether their start date is already past.

What is wrong:

An unconverted staffing plan whose start date has arrived or passed should be flagged for review, not quietly counted as normal future work.

Why it is wrong:

Staffing plans are opportunities/forecasts until converted to estimates. If the planned start date is here and no estimate exists, the business needs to review it as expired/stale, not count it as likely revenue.

Likely files:

- `webapp/src/modules/estimating/features/staffing-list/views/StaffingListView.vue`
- `webapp/src/modules/estimating/features/analytics/views/RevenueForecastView.vue`
- `webapp/src/modules/estimating/features/analytics/utils/forecast.ts`
- `Api/Controllers/StaffingPlansController.cs`

How to fix:

Add a derived stale/expired flag for unconverted staffing plans where `startDate <= today`. Show a visible badge/filter lane and exclude or separately categorize them from future revenue.

Regression test:

Find or create a test staffing plan with labor rows, no converted estimate, and start date on/before today. It must show as Needs Review/Expired and must not silently inflate ordinary future revenue.

