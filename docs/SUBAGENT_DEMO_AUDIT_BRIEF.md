# Subagent Demo Audit Brief

Use this brief before assigning any Codex/AI subagent to audit, test, or improve the Stronghold Enterprise Estimating demo. This is a safety and workflow document. It does not authorize code changes or database writes by itself.

## QA Orchestration Authority

Codex is the QA orchestrator for this project. Claude may implement fixes/features, but Claude does not get to certify its own work as complete.

- Only Joseph or Codex may check off items in `docs/LIVE_QA_TODO.md`.
- Claude may write implementation notes and mark an item `Ready for Codex verification`.
- If Claude checks an item complete without Joseph/Codex verification, reopen it and write: `Reopened reason: checked by implementer without independent verification`.
- Claude may only close an item directly if the note explicitly states `Joseph waived this requirement` or `Joseph accepted this as-is`, with date, rationale, and evidence.
- Codex audits Claude's changes, captures screenshots, updates TODO status, and writes evidence handoffs.

## Platform Expansion Contract

Portal / Planning / Scheduling work must also follow:

- `docs/PLATFORM_EXPANSION_MASTER_CONTRACT.md`

That document defines the one-portal/three-app architecture, app boundaries, scheduling demand rules, planning step-out expectations, actual-vs-estimate delta requirements, FCO document requirements, EF/bootstrap rules, and phase gates.

Any audit of platform work must verify:

- The implementation used the real repo paths (`Api/`, `Data/`, `Shared/`, `webapp/`) and did not invent paths.
- Estimating still works after platform changes.
- Portal remains a thin shell/read-model surface.
- Planning owns step-out/work-package/FCO/actuals workflow.
- Scheduling owns resources, availability, assignments, conflicts, coverage gaps, ending-soon, and available-soon.
- Converted staffing plans do not count separately from linked estimates.
- Approved unconverted staffing plans do count as future demand.

## Hard Rules

- Do not delete application code.
- Do not revert user changes.
- Do not edit, delete, overwrite, move, or create application source files.
- Do not apply patches or auto-fix issues during QA/audit assignments.
- Do not reset, reseed, truncate, wipe, recreate, or "clean up" SQL Express data.
- Do not delete or regenerate Cost Books.
- Do not run any endpoint, script, Playwright flow, or test setup that seeds, resets, or mutates live SQL Express data unless Joseph explicitly approves that exact action.
- Do not click Save, Submit, Apply status update, Clone rate book, Reset Defaults, Delete, or similar write actions during UI testing unless the task explicitly says the record is disposable and approved for mutation.
- If a finding requires a code change, report it first with file paths, why it matters, risk level, and recommended fix. Do not make the fix unless assigned an implementation task.
- A live TODO item is not permission to fix code. Treat it as a finding or decision item that must be investigated and reported.
- If Joseph accepts behavior as-is, honor that decision and report the item as `ACCEPTED AS-IS`; do not keep re-flagging it as a defect unless new evidence changes the risk.
- If a test creates temporary records, name them clearly with `QA_TEST_<timestamp>` and clean up only those records if cleanup is explicitly approved.

## Sub-Agent Contract

Every subagent is a read-only QA sub-agent unless a later human instruction explicitly assigns implementation work.

Subagents may:

- Inspect code.
- Inspect UI.
- Run the app.
- Click buttons.
- Enter test data.
- Save test data only when the task explicitly permits disposable records, and only with a `QA_TEST_<timestamp>` prefix.
- Capture screenshots.
- Analyze workflows.
- Write findings.

Subagents may not:

- Edit code.
- Delete code.
- Apply patches.
- Change configs.
- Update dependencies.
- Alter database schema.
- Remove seeded data.
- Delete anything they did not create during the current session.

Subagent output is a report to the orchestrator. Subagents do not fix anything themselves.

## Theme Audit Requirement

The visible theme toggle is not cosmetic. It is a required UI test path.

Any UI audit claiming "every page", "every button", "every click", or "full app audit" is incomplete unless it:

- Clicks the top-bar theme toggle.
- Tests every primary page in dark mode and light mode.
- Captures light-mode screenshots for every primary demo page.
- Checks buttons, disabled buttons, text buttons, outlined buttons, links, form labels, placeholders, page titles, subtitles, cards, table headers, empty states, dialogs, and AI sidebar readability in both themes.
- Reports light-mode contrast/readability failures separately from dark-mode failures.

Known light-mode failure evidence from 2026-04-26:

- `docs/qa-evidence/QA_LIGHT_MODE_20260426/crew-templates-light.png`
- `docs/qa-evidence/QA_LIGHT_MODE_20260426/new-estimate-light.png`

Specific current failures include white page titles on pale backgrounds, disabled buttons barely visible, low-contrast action links/buttons, and hardcoded dark-theme utility classes leaking into light mode.

## Environment Assumptions

- Local demo database is SQL Server Express, not Docker.
- Demo data is valuable and must be preserved.
- The project already has a working in-app AI sidebar.
- The Azure AI Foundry agent work is in progress, but the current safe demo path is the in-app sidebar unless `/api/agent` has been secured and reviewed.
- Treat public config secrets and local AI keys as sensitive. Do not print or expose them in screenshots or reports.

## Domain Truth

### Cost Book

The Cost Book is the internal company cost model. It should drive burdened labor cost, equipment cost, expense assumptions, overhead, margin analysis, and job cost truth.

Important expectations:

- Cost Book data must not be deleted by tests.
- Cost Book data must not be silently overwritten by "reset defaults."
- Cost Book rates are internal, not customer-facing.
- Proposal PDFs and customer bid documents must not expose internal cost, burden, gross profit, or margin.
- Job Cost Analysis should make the selected/default Cost Book visible.
- Missing cost rates should be flagged clearly instead of silently producing misleading margin.

### Rate Book

The Rate Book is the client/location/customer billing rate model. It should drive the billable estimate price shown to the customer.

Important expectations:

- Loading a Rate Book should populate matching labor, equipment, and expense billing rates.
- AI-added labor rows should use the active Rate Book where possible.
- Fuzzy matching can suggest Rate Books, but cloning a Rate Book creates data and should be avoided in safe demo testing unless approved.
- Rate Book and Cost Book are related but not interchangeable.

## Core Workflows To Understand

### Estimate Workflow

An estimator should be able to:

1. Create a new estimate.
2. Fill the header with client, site, location, branch, dates, shifts, duration, and status.
3. Load the correct Rate Book.
4. Add labor rows or crews with correct positions, quantities, shifts, schedules, and ST/OT/DT bill rates.
5. Add equipment rows with correct unit type and billing math.
6. Add expenses such as per diem, travel, lodging, consumables, or other direct costs.
7. Review Job Cost Analysis using the Cost Book.
8. Review Summary totals, customer price, internal cost, and margin.
9. Save as Draft.
10. Generate/export a customer-safe proposal PDF.
11. Submit/send the bid.
12. Move through status lifecycle: Draft, Submitted, Pending Approval, Approved/Won/Awarded, Lost/Declined, Revision.

Known audit concerns:

- Equipment weekly/monthly/hourly billing math must be verified.
- Expense `type` should persist after save and revision restore.
- Revision workflow target: one `Create Revision` action creates the next sequential editable revision copy. The user edits that revision, clicks Save, and the saved row counts/totals belong to that revision number.
- Do not pass an implementation that still has two different revision creation behaviors, such as toolbar `Save + Revision` plus drawer `+ Create Revision` snapshot paths. Prior revisions may be viewed/restored, but creation must be one clear workflow.
- If the form shows 8 labor rows and a new Grand Total after saving Rev 2, Revision History must show Rev 2 with 8 labor rows and that same Grand Total after refresh.
- Status names must be consistent across UI, API, AI, reports, and analytics.
- Summary values should not blindly trust stale client-side calculations.

### Estimate Status Lifecycle Rules

Status is a consequence of a business action, not a manual data-entry field.

Required behavior:

- The estimate header must show status as a read-only badge, not an editable dropdown.
- Saving an unsent estimate must create or keep it as Draft automatically.
- "Submit for Approval" must be a dedicated action button, not a dropdown selection.
- Submit flow must save the latest estimate, generate/open the customer-safe proposal PDF for review, then update status automatically after the user confirms the submission action.
- Customer-facing PDF/email flow must not expose internal Cost Book rates, internal cost, gross profit, or margin.
- Awarded/Won, Lost/Declined, and Revision actions should be list-page workflow actions so the estimator does not open the full estimate just to manually change status.
- Lost/Declined must require a reason before status changes.
- The status vocabulary must be consistent across frontend, API, AI tools, analytics, manpower forecast, PDFs, and reports.
- Backend accepted status values must match the frontend labels or be explicitly mapped; "Submitted for Approval" must not fail because the API only accepts "Submitted."

### Staffing Plan And Manpower Workflow

A staffing plan forecasts future manpower before an estimate exists.

An estimator/planner should be able to:

1. Create a staffing plan for a future job or opportunity.
2. Add manpower by craft, role, date range, and quantity.
3. Save the staffing plan.
4. See it in manpower forecast as future demand.
5. Convert the staffing plan to an estimate.
6. See the created estimate number and a durable link/reference back to the staffing plan.
7. Stop counting the staffing plan separately after conversion, because the estimate becomes the source of truth.

Forecast expectations:

- Current awarded/active work should count.
- Future staffing plans not converted to estimates should count.
- Unconverted staffing plans whose start date has arrived or passed are stale/expired opportunities. They must be flagged for review and must not silently count as clean future revenue or ordinary future demand.
- Stale/expired staffing plans should not be auto-marked Lost by the system. A human should deliberately convert, archive, or mark lost/declined with a reason.
- Converted staffing plans should not double-count beside their estimates.
- Future estimates should count according to approved status rules.
- Date filters such as calendar year 2026 should show expected revenue/manpower for that period.
- Reports should help answer manpower needed, available, and coverage/gap.

### Revenue Forecast Rules

The Revenue Forecast page must have one source of truth for period/date filtering.

Required behavior:

- Top period tabs control both KPI cards and the Revenue vs Cost chart.
- There must be no separate chart-level period filters such as Monthly / Quarterly / Cumulative unless they are purely display modes and cannot contradict the selected business period.
- Preferred demo target: remove separate chart-level period controls and derive chart buckets from the top period tab.
- Top period tabs should include MTD, Q1, Q2, Q3, Q4, QTD, YTD, and ALL.
- Q1 means Jan-Mar, Q2 means Apr-Jun, Q3 means Jul-Sep, Q4 means Oct-Dec.
- QTD means current quarter-to-date for KPI totals, but the chart must still show the full quarter structure so future months are visible at $0 or projected/pipeline values.
- Selecting a future quarter must be possible and must show projection/pipeline context, not a blank or clipped chart.
- KPI totals and chart totals for Awarded, Pending/Pipeline, and Lost must reconcile for the same selected period.
- If KPI cards show revenue while the chart visually communicates $0, report a High severity defect even if a single point technically contains the value.

### AI Demo Workflow

The demo must showcase AI capabilities without risking live data.

Preferred safe path:

- Use the in-app AI sidebar.
- Let AI fill the form or stage actions.
- Use Apply for client-side form changes only.
- Do not Save unless the record is disposable and approved.

AI should demonstrate:

- Natural language estimate header fill.
- Natural language staffing plan header fill.
- Date math and typo tolerance.
- Rate Book suggestion/load.
- Crew/labor assembly from natural language on both estimates and staffing plans.
- Crew template application where available.
- RFQ PDF/DOCX parsing with ambiguous scope warnings.
- Business Q&A from live database through safe, company-scoped app tools.
- Jobs active today based on estimate start/end date and status.
- Jobs coming up in the next two weeks based on estimate start dates.
- Customer/location history questions with job count, each job duration, and total revenue.
- Review with AI on a populated estimate/staffing plan.
- Status update confirmation on a disposable estimate only.

Avoid until fixed/approved:

- `/api/agent` raw SQL bridge.
- AI app navigation/control if routes are stale.
- Rate Book clone actions.
- AI status update Apply on real estimates.
- Customer-sensitive RFQ uploads.
- AI `left on the table` Valero historical-comparison scenario. Joseph deferred P0-022 until after the demo.

Safe example prompts:

- `New estimate for Shell Deer Park TX, turnaround, both shifts, 14 days starting June 10, 2026. Set branch Gulf Coast and job letter S.`
- `Do we have Shell Deer Park TX rates? Load the exact match if available; otherwise suggest the closest existing rate book.`
- `Add a boilermaker crew: 1 General Foreman, 4 Boilermaker Journeymen, 2 Boilermaker Helpers, day shift.`
- `Create a staffing plan for Dow Deer Park TX starting January 6, 2027 for 21 days, day shift, load matching or nearest rates, and add a turnaround crew/template.`
- `What's our total pipeline value by status?`
- `Show awarded jobs active today.`
- `What jobs are coming up in the next two weeks?`
- `How many jobs have we done for Dow in Deer Park Texas over the last year? Tell me the number of jobs, duration of each one, and total revenue generated.`
- `What's our win rate for Shell?`

### Joseph's Exact Boss-Demo Prompt Pack

These are not optional examples. Treat them as the exact prompt pack Joseph may ask live, with wording variations.

1. `Create new estimate for DOW Chemical in Deer Park Texas, starting June 10 2026 and lasting 10 days. It will be both day and night shift 10 hours each with OT after 40 no dt. start me off with a PM and a foreman on both days and nights and add me a pipefitter crew to start`
2. `Create estimate for P66 in Linden NJ Starting oct 1, 2026 and lasting 30 days. It will be day shift only 12 hour shifts with ot after 8 and dt on sundays. Start me off with a ndt crew and a foreman. add standard per diem for this job.`
3. `What jobs do i have coming up?`
4. `What jobs are going on today?`
5. `Whats my estimated revenue for this quarter?`
6. `How many jobs have we done for Shell this year?`

Acceptance notes:

- DOW prompt must map "lasting 10 days" to June 10 through June 19, 2026 inclusive.
- DOW prompt must map "OT after 40 no dt" to weekly 40 overtime and no DT weekends.
- DOW prompt must create both day and night PM/Foreman coverage plus pipefitter crew rows.
- P66 prompt must map October 1 through October 30, 2026 inclusive.
- P66 prompt must map "ot after 8" to daily 8 overtime and "dt on sundays" to Sundays-only double time.
- P66 prompt must add standard per diem if the active/matched rate book has per diem items. If the AI cannot add expenses/per diem yet, report that as a demo-blocking gap rather than pretending it worked.
- Q&A prompts must be reconciled to DB/API source records. No answer is accepted on wording alone.

### Boss Demo Acceptance Criteria

For tomorrow's demo, subagents must explicitly PASS/FAIL/NOT VERIFIED these items:

1. AI accurately fills an estimate header, loads/suggests rates, adds crew/labor, and produces non-zero totals.
2. AI accurately fills a staffing plan header, loads/suggests rates, adds crew/template labor, and produces non-zero Rough Labor.
3. AI answers "what jobs are going on today" from DB records where `startDate <= today <= endDate`.
4. AI answers "what jobs are coming up in the next two weeks" from DB records with start dates in the next 14 days.
5. AI answers customer/location/year history questions with job count, duration of each job, and total revenue.
6. Review with AI works and references current form values.
7. Demo data covers 25 realistic 2025 estimates, 2026 current work/pipeline, and 10 populated 2027 staffing plans.
8. KPI values reconcile and can drill down to individual source records/proposals/invoices where available.
9. Estimate list three-dot menu opens outside clipping containers and includes fallback `Open`; staffing list hover actions are visible and accepted by Joseph.
10. LaborGrid TOTAL column stays sticky, visible, and aligned while horizontally scrolling.
11. Crew template dialog has a populated demo state with at least 3 usable templates or a clear empty state in non-demo environments.
12. AI provider config is present for the demo path, but reports/screenshots must never expose the key value.

## Required Subagent Roles

### Estimating Workflow Auditor

Focus on estimate creation, Rate Book loading, labor/equipment/expense rows, Cost Book margin, Summary, PDF, save/reopen, revision, and status lifecycle.

Report:

- What works.
- What is missing.
- What is dangerous for demo.
- What files likely need changes.
- Which tests are safe read-only and which mutate data.

### Staffing And Forecast Auditor

Focus on staffing plans, conversion to estimate, manpower forecast, calendar, revenue forecast, dedupe, and future demand.

Report:

- Whether converted plans double-count.
- Whether converted estimate references survive refresh.
- Whether forecasts include the right statuses and date ranges.
- Whether screen values and exported CSV/report values agree.

### AI Demo Auditor

Focus on in-app AI and Foundry bridge safety.

Report:

- Which AI capabilities are already wired.
- Which prompts are safe for demo.
- Which actions write data.
- Whether tenant/company scoping is enforced.
- Whether provider config can block the demo.
- Whether `/api/agent` is safe enough to use.

### UI/UX Auditor

Focus on normal-person usability.

Report:

- Layout clipping, overlap, scrolling, hidden totals, confusing labels.
- Whether controls behave predictably.
- Whether week tabs, totals, and date ranges are understandable.
- Whether AI pending changes show enough before/after information.
- Whether Rate Book and Cost Book state is obvious to the user.

## Report Format

Each subagent report must begin with:

`Read-only audit complete. No files edited, no DB writes, no seed/reset, no deletes.`

Then include:

- Findings ordered by demo risk.
- A checklist verdict table for every assigned item from `docs/QA_REGRESSION_CHECKLIST.md`: `ID | PASS/FAIL/NOT VERIFIED | evidence`.
- A live TODO verdict table for every relevant item from `docs/LIVE_QA_TODO.md`: `ID | OPEN/DONE/BLOCKED/REOPENED/ACCEPTED AS-IS | evidence`.
- File references with paths and line numbers where possible.
- Manual test steps split into `Safe read-only` and `Creates/updates data`.
- Recommended fixes with expected impact.
- Any uncertainty or assumptions.

Reject any subagent report that does not include explicit checklist and live TODO verdicts. A general "looks good" report is not acceptable.

## Demo Priority Order

1. Preserve Cost Book and demo data.
2. Make the in-app AI sidebar reliable and impressive.
3. Ensure Rate Book rates populate AI-created labor rows correctly.
4. Make labor grid totals and week tabs understandable.
5. Make Cost Book/Internal Cost/Margin visible and credible.
6. Fix staffing-plan conversion double-count risk.
7. Align status lifecycle across UI, API, AI, PDF, and reports.
8. Improve reporting and forecast clarity.
9. Secure or avoid Foundry `/api/agent` until it is no longer raw SQL/open surface.

## Regression Checklist Requirement

Every subagent run must also read and verify `docs/QA_REGRESSION_CHECKLIST.md` and `docs/LIVE_QA_TODO.md`.

High-priority checks that must never be missed:

- Analytics agents must reconcile KPI cards against chart totals after MTD/QTD/YTD/ALL and year changes.
- Staffing agents must prove staffing plans have labor rows and non-zero rough labor before treating them as valid test coverage.

Live TODO checks that must never be missed:

- For every relevant item in `docs/LIVE_QA_TODO.md`, report **OPEN**, **DONE**, **BLOCKED**, **REOPENED**, or **ACCEPTED AS-IS**.
- If a live TODO is checked but verification fails, report it as **REOPENED** and explain exactly why.
- If an item is in `Accepted As-Is`, do not fail it again. Mention it only as accepted behavior unless the app has changed or new evidence creates a new risk.
- New defects found during testing should be reported to the orchestrator so the live TODO can be updated.

- Read the entire checklist before testing.
- For every item in your lane (domain prefix), produce an explicit **PASS**, **FAIL**, or **NOT VERIFIED** verdict.
- Never skip an item silently — if you could not verify it, say so.
- If you find a new bug not yet in the checklist, report it. Do not add it yourself — the fix owner adds it after the fix lands.

---

## Coordinator Prompt Template

When starting a subagent, include this:

```text
Before testing anything, read both files completely:
- docs/SUBAGENT_DEMO_AUDIT_BRIEF.md  (business rules, demo-safety, what you must not do)
- docs/LIVE_QA_TODO.md               (current live blockers and checkboxes - verify relevant items)
- docs/QA_REGRESSION_CHECKLIST.md    (every bug we've found — verify ALL items in your lane)
- docs/PLATFORM_EXPANSION_MASTER_CONTRACT.md (required for Portal/Planning/Scheduling work)

Your task is read-only unless explicitly told otherwise. Do not edit files. Do not seed/reset/delete data. Do not mutate SQL Express. Do not run destructive tests.

For each checklist item in your lane, produce an explicit PASS, FAIL, or NOT VERIFIED verdict in a table. Never skip silently. Include screenshot paths or concrete evidence for UI/reporting items.

For each live TODO item in your lane, produce OPEN, DONE, BLOCKED, or REOPENED. If an item is checked but fails verification, mark it REOPENED in your report with evidence.

For analytics/revenue forecast testing, reconcile numbers. If KPI cards and the chart communicate different totals for the same selected year/period, report a High severity defect. This includes the inverse case: KPI cards show revenue but the chart visually appears to show $0 or only a single unreadable point.

For estimate status lifecycle testing, status must be read-only in the estimate header. Save, Submit for Approval, Award/Won, Lost/Declined, and Revision must be explicit workflow actions with automatic status changes.

For staffing-plan testing, do not create header-only staffing plans. A staffing plan with Rough Labor = $0 is not valid coverage unless the scenario explicitly tests an empty plan.

For platform-expansion testing, treat Claude as the implementer and Codex/Joseph as the closer. Do not mark a TODO complete unless Joseph or Codex verified it. If Claude checked a box without independent evidence, report it as REOPENED.

Audit only your assigned lane. Report findings with file paths, demo risk, safe manual checks, and recommended fixes. Treat Cost Book preservation as a hard requirement.
```
