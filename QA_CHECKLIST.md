# Stronghold Enterprise Estimating — QA Agent Checklist
# Feed this file into every QA agent run. Agents must verify ALL items below.
# When a new bug is found and fixed, add it to SECTION 3 as a regression check.

---

## SECTION 1 — BUSINESS RULES (must pass on every run)

### Estimates
- Estimates saved initially must default to **Draft** status.
- Estimate numbering must be sequential and company-scoped.
- Submitted estimate flow must generate a customer-facing PDF that excludes internal cost/profit/margin details.
- Valid estimate statuses: Draft, Submitted for Approval, Pending, Approved, Declined, Revision.
- Reports must reflect each status correctly in pipeline, revenue, and analytics views.
- Rate book must load correctly when assigned to an estimate.
- Cost book must load correctly and all labor calculations must use correct rates.
- All totals, margins, labor math, and rollups must reconcile across estimate, forecast, analytics, and reports.

### Staffing Plans
- Every staffing plan must receive a unique staffing plan number.
- Converting a staffing plan must generate an estimate number and retain a reference link back to the staffing plan.
- Once converted, the staffing plan status must be "Converted" and ConvertedEstimateId must be set.
- Converted staffing plans must NOT continue contributing manpower demand separately — the estimate is the source of truth.
- A converted staffing plan must not be deletable.

### Manpower Forecast
- Must count: active estimates/jobs, future estimates, staffing plans not yet converted.
- Must NOT double-count: converted staffing plans should not appear alongside their derived estimates.
- Worker count math must reconcile with source records.
- Date logic must correctly classify jobs as current vs. future.
- Staffing need (+/-) output must be accurate.

### Reports and Analytics
- Date range filters must actually filter the data (not be decorative).
- Quarterly, YTD, yearly, and future-year projections must show correct totals.
- Completed jobs must count as revenue.
- Approved jobs must count as projected revenue.
- Staffing plans + submitted + pending must count as pipeline where appropriate.
- Totals must reconcile with source records.

---

## SECTION 2 — UI/UX CHECKS (must verify on every run)

### Controls
- Every button must have a working @click handler — no silent voids.
- No button may show a "coming soon" or "not yet implemented" toast in a user-facing context.
- Every filter control must actually filter data — verify the v-model ref is consumed in a computed or watch.
- No dropdown may render with truncated text ("2..." or similar) due to insufficient width.
- No control may appear twice on the same screen doing the same thing (duplicate dropdowns, duplicate buttons).
- Tab controls must change visible content — clicking a tab must render a different view.

### Forms
- Required fields must have visible validation feedback.
- Status dropdowns must include all valid statuses (Draft, Submitted for Approval, Pending, Approved, Declined, Revision for estimates; Draft, Active, Approved for staffing plans).
- "Canceled" and "Pending" must have visually distinct status badges (not the same color).
- "Draft" status must have a visible badge (not unstyled/colorless).

### Layout
- No screen may open completely empty when it should show data by default.
- Sticky column layouts must not create visual gaps between left and right sticky panels.
- AppLayout padding must not clip scrollable content areas.

---

## SECTION 3 — REGRESSION CHECKS (specific bugs found and fixed — verify they stay fixed)

### REG-001: Duplicate Year Dropdown — RevenueForecastView
- **What broke:** Two `selectedYear` Dropdown components rendered simultaneously — one in the toolbar (line ~29) and one inside the FINANCIAL DASHBOARD section header (line ~51). Inner one truncated to "2...".
- **Fixed by:** Removing the inner `fd-inner-year-dropdown` from the section header.
- **Verify:** Open Revenue Forecast. Confirm exactly ONE year dropdown is visible (in the toolbar only). No year selector inside any section header.

### REG-002: AVG MARGIN = 0%, FUTURE PROFIT = $0 — RevenueForecastView
- **What broke:** `loadData()` called `/api/v1/analytics` which does not exist. Correct route is `/api/v1/analytics/dashboard`. Silent `.catch` returned null, so `analyticsAvgMargin` stayed 0.
- **Fixed by:** Correcting the API URL to `/api/v1/analytics/dashboard`.
- **Verify:** Open Revenue Forecast. AVG MARGIN must not be 0% and FUTURE PROFIT must not be $0 (unless the data genuinely shows no margin, which must be confirmed against seeded data).

### REG-003: Dead View Tabs — RevenueForecastView
- **What broke:** "All Jobs" and "Compare" tabs set `activeView` but no template content was conditionally rendered based on `activeView`. Clicking tabs did nothing visible.
- **Fixed by:** Either hiding unimplemented tabs or adding content panels.
- **Verify:** Every tab in Revenue Forecast either shows distinct content or is not rendered.

### REG-004: Ghost Filter Controls — RevenueForecastView
- **What broke:** `selectedBranch` and `selectedCustomer` dropdowns were bound to refs never consumed in any computed or filter logic. Changing them had zero effect.
- **Fixed by:** Wiring the refs into filter logic or removing the controls.
- **Verify:** Branch and Customer filter dropdowns either affect visible data when changed, or are not rendered.

### REG-005: Export PNG Stub — ManpowerForecastView
- **What broke:** "Export PNG" button showed "PNG export coming soon" toast. Visible, enabled, but fake.
- **Fixed by:** Hiding the button or implementing the export.
- **Verify:** No visible "Export PNG" button unless it is fully functional.

### REG-006: Load Crew Dead Button — StaffingFormView
- **What broke:** LaborGrid emits `loadCrew` but StaffingFormView had no `@loadCrew` listener. Button clicked, nothing happened.
- **Fixed by:** Wiring `@loadCrew` handler or hiding button in staffing context.
- **Verify:** In Staffing Form labor grid, "Load Crew" button either opens a crew picker or is not visible.

### REG-007: Import/Export Stub Buttons — RateBookView
- **What broke:** "Import" and "Export" buttons showed "not yet implemented" toasts.
- **Fixed by:** Hiding both buttons.
- **Verify:** No Import or Export buttons visible on the Rate Books page unless fully functional.

### REG-008: DT Weekends Billing Logic — useOtCalculator
- **What broke:** Sunday was always charged DT regardless of the DT Weekends setting (hardcoded `if (dow === 0) return DT`). With "No DT Weekends" selected, Sundays still showed double-time — billing dispute risk.
- **Fixed by:** Changing `dtWeekends` from boolean to 3-option string: `'none'` (no DT on weekends), `'sun_only'` (DT Sundays only), `'sat_sun'` (DT both days). Logic now checks the string value.
- **Verify:** 
  - Open estimate with DT W/E = "No DT Weekends" → Sundays must show OT, not DT.
  - Change to "Sundays Only" → Sundays show DT, Saturdays show OT.
  - Change to "Sat & Sun" → Both Sat and Sun show DT.

### REG-009: Weekly View Gap — LaborGrid
- **What broke:** When a week tab had fewer than 7 job days (e.g., a partial last week), only the job days were rendered as columns. Sticky left/right panels created a large visual gap.
- **Fixed by:** Always rendering a full Mon–Sun 7-column week, with out-of-job-range days grayed out and disabled.
- **Verify:** On an estimate with a partial last week, the week tab shows 7 day columns. Days outside the job date range are visually dimmed and not editable.

### REG-010: ConfirmDialog Not Resolving — Multiple Views
- **What broke:** Delete buttons in EstimateListView, EstimateFormView, CostBookView, and CrewTemplatesView used `ConfirmDialog` in templates but it was never imported. Vue warned "Failed to resolve component: ConfirmDialog" and deletes silently failed.
- **Fixed by:** Adding explicit `import ConfirmDialog from 'primevue/confirmdialog'` to each view.
- **Verify:** Delete action on an estimate shows a confirmation dialog before deleting. No Vue console warning about unresolved ConfirmDialog.

### REG-011: Manpower Forecast Opens Empty
- **What broke:** All KPI cards showed "—" on first load. User had to manually click "Run Forecast" before any data appeared.
- **Fixed by:** Auto-running forecast on mount with default date range.
- **Verify:** Manpower Forecast page shows populated KPI cards and chart immediately on load without requiring user action.

### REG-012: Equipment Math Ignores Rate Type — EquipmentSection
- **What broke:** Equipment rows with rate type "hourly", "weekly", or "monthly" all calculated as `rate × qty × days` regardless of type. A monthly-rate piece of equipment on a 30-day job was billed as if it were a daily rate.
- **Fixed by:** Making the calculation rate-type-aware (hourly = rate × qty × hours, weekly = rate × qty × weeks, monthly = rate × qty × months).
- **Evidence:** `EquipmentSection.vue` line 122.
- **Verify:** Add an equipment row with type "Monthly", rate $1000, qty 1, on a 30-day job. Subtotal must be $1000 (1 month), NOT $30,000 (30 days × $1000).

### REG-013: Cost Book / JCA Only Covers Labor — Internal Cost Understated
- **What broke:** Job Cost Analysis only applied cost book rates to labor rows. Equipment and expense rows had no internal cost applied, meaning gross margin was overstated for any estimate with equipment or expenses.
- **Fixed by:** Applying cost book logic to equipment and expense rows as well.
- **Evidence:** `JobCostAnalysis.vue` line 202.
- **Verify:** Create an estimate with labor + equipment + expenses. Internal cost must reflect all three, not just labor. Gross margin must be derived from total bill minus total internal cost.

### REG-014: Staffing Plan Labor Rows Required for Meaningful Data
- **What broke:** Staffing plans created with header fields only (no labor rows added) showed Rough Labor Total = $0. Downstream manpower forecast showed zeros for those plans.
- **Fixed by:** Ensuring labor rows are added before saving/converting a staffing plan.
- **Evidence:** `StaffingPlansController.cs` line 319 (labor upsert is a separate endpoint — header save and labor save are two separate calls).
- **Verify:** A newly created staffing plan with labor rows must show a non-zero Rough Labor Total. Manpower Forecast must count workers from those labor rows.

### REG-015: Manpower Forecast Shows Failure After Run
- **What broke:** After clicking "Run Forecast," UI showed "failed to load manpower data" and all values were zero. Root cause: staffing plans had no labor rows (header-only), causing the forecast computation to produce empty results.
- **Fixed by:** Fixing REG-014 (labor rows required) and verifying the forecast API handles empty-row plans gracefully.
- **Verify:** Run Manpower Forecast with date range covering seeded staffing plans that have labor rows. All KPI cards must show non-zero worker counts. No "failed to load" error must appear.

### REG-016: Submitted Status Missing from Estimate List Filter and Status Badge Map
- **What broke:** Estimate status filter dropdown in EstimateListView only included: Draft, Pending, Awarded, Lost, Canceled. "Submitted" was missing. If an estimate had status "Submitted," it rendered with no badge color and could not be filtered.
- **Fixed by:** Adding "Submitted for Approval" to `statusOptions` and adding `'Submitted': 'info'` to `statusSeverity` map.
- **Evidence:** `EstimateHeader.vue` line 268, `EstimatesController.cs` line 485.
- **Verify:** Estimate list filter dropdown includes "Submitted for Approval". An estimate in Submitted status shows a colored (info/blue) badge.

### REG-017: PDF/Email Bid Submission Workflow Incomplete
- **What broke:** The submit estimate flow only offered "Export PDF." There was no workflow to email the PDF to the customer or formally submit the estimate for approval.
- **Fixed by:** Implementing a submit dialog that generates a customer-facing PDF (without internal cost/margin) and sends it to the customer email on file.
- **Evidence:** `EstimateFormView.vue` line 24.
- **Verify:** On an estimate in Draft status, clicking "Submit" opens a dialog to confirm customer email and sends. Estimate status changes to "Submitted for Approval." PDF sent to customer excludes internal cost, cost rates, and gross margin fields.

### REG-018: Sidebar/Content Overlap at 1280px Viewport
- **What broke:** At 1280px screen width, the left navigation sidebar overlapped the main page content, covering the page title and top portion of estimate forms.
- **Fixed by:** Adjusting breakpoint or content offset so the main area always clears the sidebar width.
- **Verify:** At exactly 1280px viewport width, open an estimate form. The page title and all content must be fully visible with no sidebar overlap.

---

## SECTION 4 — AGENT INSTRUCTIONS

When running as a QA agent against this codebase:

1. Read this entire file before beginning your audit.
2. Verify every item in Section 1 (Business Rules) against the current code.
3. Verify every item in Section 2 (UI/UX Checks) against the current templates and logic.
4. For every item in Section 3 (Regression Checks), explicitly confirm it is still fixed — do not assume.
5. Report any new issues not yet in this file.
6. Do NOT edit, delete, or modify any source code. Report only.
7. For each finding, provide: ID, title, severity, file:line, description, expected behavior, actual behavior, likely cause, recommended fix.

Every regression that appears in Section 3 must produce a PASS or FAIL verdict in your report.
