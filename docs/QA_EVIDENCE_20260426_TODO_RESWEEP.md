# QA Evidence - TODO Resweep

Date: 2026-04-26

Scope: read-only audit from `docs/LIVE_QA_TODO.md`. No source code was modified by Codex in this sweep.

Post-sweep decision: Joseph deferred P0-022 / QA-AI-014 until after the demo. Keep the finding as historical evidence, but do not treat it as a demo blocker unless Joseph explicitly reopens it.

## Build Verification

- `dotnet build --no-restore --configuration Release` passed.
  - Warning remains: `NU1903` AutoMapper 14.0.0 high severity advisory.
  - Warning remains: NSwag post-build command resolves as `run ../Api/nswag.json` and exits `9009`, but MSBuild reports success.
- `npm.cmd run build:dev` passed.

## Screenshot Evidence

All screenshots are under:

`docs/qa-evidence/QA_AUDIT_20260426_TODO_RESWEEP/`

- `revenue-forecast.png` - Revenue Forecast YTD, Avg Margin/Future Profit now non-zero.
- `revenue-awarded-drilldown.png` - Awarded Revenue KPI drilldown opens and lists source estimates.
- `estimates-list.png` - Estimate list baseline.
- `estimate-row-menu-open-1600.png` - Estimate row three-dot menu opens with fallback `Open`.
- `staffing-list.png` - Staffing list baseline with non-zero rough labor cards.
- `staffing-menu-check.png` - Staffing list has no ellipsis/dropdown menu (`ellipsis 0` in Playwright).
- `staffing-search-pw-test.png` - `PW-TEST` search returns no visible staffing plans.
- `estimate-new.png` - New estimate dark mode baseline.
- `crew-templates-light.png` - Crew templates light mode now readable and shows 3 templates.
- `estimate-new-light.png` - New estimate light mode now readable.

## Findings

| ID | Current Verdict | Evidence |
|----|-----------------|----------|
| P0-001 | OPEN / PARTIAL | Status length expanded and backend accepts `Submitted for Approval`, but seeded/list rows still display `Submitted`; list filter offers only `Submitted for Approval`, and server filtering is exact-match. |
| P0-004 | OPEN / PARTIAL | Backend now rejects `Lost` with no `LostReason`; still needs live API/AI workflow proof. |
| P0-005 | PASS / DONE | `revenue-forecast.png` shows `AVG MARGIN 31%`, `FUTURE REVENUE $5.7M`, `FUTURE PROFIT $1.8M`. |
| P0-006 | OPEN / PARTIAL | Chart has one top-level period control and separate `Staffing Pipeline`, but full period-by-period numeric reconciliation was not completed. |
| P0-011 | OPEN / PARTIAL | Awarded KPI drilldown works (`revenue-awarded-drilldown.png`), but Future Revenue is not drillable to staffing plan source rows. |
| P0-012 | DONE / ACCEPTED | Estimate list menu works with fallback Open. Joseph verified staffing list actions are intentionally hover-revealed instead of using a three-dot/dropdown menu. |
| P0-014 | OPEN / PARTIAL | Crew template page shows 3 seeded templates; applying templates to estimate/staffing was not verified. |
| P1-SP-002 | OPEN | Staffing status is now read-only, but Submit for Approval is visible on new unsaved plans, save errors can be swallowed before PATCH, and backend status patch lacks validation/converted guard. |
| P1-SP-003 | PASS / DONE | Search `PW-TEST` returns zero visible staffing plans. |
| P0-020 | OPEN / PARTIAL | Light mode is improved on Crew Templates and New Estimate; full-app light-mode sweep still required. |
| P0-021 | FAIL / OPEN | Estimate form still has `Save + Revision`; drawer still has `+ Create Revision`; drawer text still describes snapshots. |
| P0-022 | DEFERRED | Joseph deferred this until after the demo. Historical finding remains: `get_estimate_details` executes but is not exposed in `BuildDbTools()`. `Review with AI` still sends the old generic rate-anomaly prompt. |

## Highest Priority Fixes From This Sweep

1. Expose `get_estimate_details` in `BuildDbTools()` and update AI review prompt/system prompt for the Valero left-on-the-table demo.
2. Remove/replace the duplicate revision snapshot workflow with one `Create Revision` editable sequential revision flow.
3. Finish status normalization: decide persisted value vs display label, and make filters/badges/API/reporting match.
4. Finish staffing plan Submit for Approval guardrails.
5. Add Future Revenue drilldown to staffing plan source records.
