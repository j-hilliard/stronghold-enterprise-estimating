# Stronghold Enterprise Estimating Gap Analysis

## Current State vs Required Future State

This gap analysis compares the current application codebase against the target workflow described in `REQUIREMENTS.md` and `docs/MASTER_REQUIREMENTS_POC.md`.

The goal is to identify what exists, what is missing, what is risky, and what should be fixed first for a stakeholder demo.

## Executive Summary

The application has a strong foundation. The major modules exist: estimates, staffing plans, rate books, cost books, crew templates, revisions, proposal PDF, AI sidecar, dashboards, revenue forecast, manpower forecast, and calendar. The problem is not that the app is empty. The problem is that several workflows are only partially connected, some key business rules are inconsistent, the local database/test setup is unsafe, and several pages are becoming too large and logic-heavy.

For a Monday demo, the biggest risks are:

1. The app is not yet safely wired to a clean SQL Express demo database.
2. Existing seed data is not realistic enough and has calculation/data-shape issues.
3. Playwright test setup can mutate data and should not be run against a shared DB.
4. AI exists but needs stronger business workflow integration for a high-impact demo.
5. Rate book and cost book concepts exist but are not fully enforced through the estimate-building workflow.
6. Forecasting and analytics screens exist, but several requirements are partial or aspirational.
7. Frontend architecture already shows the same "views doing too much" pattern that caused issues in the prior app.

## Priority Legend

`P0` means blocks safe recovery, demo confidence, or core business correctness.

`P1` means important for a credible stakeholder demo but can be scoped if time is short.

`P2` means important for production readiness but likely not required for Monday unless it is part of the demo storyline.

## 1. Database, Startup, And Environment

### Target State

The app should be able to create a fresh isolated SQL Express demo database using EF Core migrations. Schema creation should come from migrations, not `EnsureCreated`. Required seed data and demo seed data should be separated. No workflow should touch another app's existing database.

### Current State

The app still points at old Docker SQL in local settings and design-time fallback. Startup uses `EnsureCreated()` and then runs `DbInitializer`. That can create tables, but it bypasses EF migration history and is not an audited Code First lifecycle. Docker is not installed on the current machine. SQL Express exists, but the app has not yet been safely pointed at a new isolated database.

### Gaps

| Priority | Gap | Impact |
|---|---|---|
| P0 | Startup uses `EnsureCreated()` instead of migrations. | New DBs are not migration-audited and future migrations can become messy. |
| P0 | Local connection string still targets `localhost,14331` Docker SQL. | App cannot run on this machine without config changes. |
| P0 | No confirmed isolated SQL Express DB name yet. | Cannot safely seed realistic demo data. |
| P0 | Seeder creates dev data on startup. | Startup is not a clean empty DB bootstrap. |
| P1 | Design-time context has hardcoded Docker fallback. | EF commands may target the wrong database. |
| P1 | .NET SDK mismatch exists: repo targets/pins .NET 7 but machine has .NET 10 only. | Backend build is blocked until SDK/runtime decision is made. |

### Required Fix

Create a new DB such as `StrongholdEstimating_Demo`, switch local config to SQL Express, replace local startup bootstrap with EF migrations, and separate required reference seeding from demo data seeding.

## 2. Demo Data

### Target State

Demo data should look like real turnaround work: refinery clients, customer-specific rate books, regional cost books, crew templates, awarded jobs active today, future jobs, pending customer approval, lost bids, staffing plans, converted staffing, revisions, equipment, expenses, FCOs, and enough repeated history for benchmarks.

### Current State

`DevSeedController` creates some realistic-looking rate books, estimates, staffing plans, and revisions. However, the seed is incomplete and has important calculation/data issues. It is also tied to destructive reset behavior for `CSL`.

### Gaps

| Priority | Gap | Impact |
|---|---|---|
| P0 | Current seed/reset deletes `CSL` estimates, staffing, rate books, cost books, templates. | Dangerous unless DB is disposable. |
| P0 | Gross margin seed values use fraction-style values while UI/analytics expect percent-style values. | Dashboard margins can display incorrectly and fall into wrong buckets. |
| P0 | Staffing seed rows do not fully populate schedules, hours, subtotals, or rough labor totals. | Manpower forecast and revenue forecast can look empty or wrong. |
| P1 | Existing seed lacks realistic equipment rows and expense rows. | Estimates do not fully demonstrate industrial turnaround scope. |
| P1 | Current seed lacks FCO demo data. | Cannot show post-award revenue capture, which is important. |
| P1 | Current seed does not provide enough repeated awarded history for rate benchmarks. | AI/analytics benchmark story is weak. |
| P1 | Scenarios may be hidden from normal estimate list because scenario records are excluded. | Scenario demo can look broken or confusing. |

### Required Fix

Create an idempotent demo seed path for a disposable database. Seed 10 client/site rate books, 1 default cost book, 6 crew templates, 12-14 estimates, 5 staffing plans, revisions, FCOs, equipment, expenses, and benchmark history.

## 3. Estimate Numbering And Job Codes

### Target State

Estimate numbers should follow a company/division/job-letter pattern such as `H26-001-VCC`, where `H` identifies the company/division such as ETS, `26` is the year, `001` is the sequence, and `VCC` is the client/location code.

Cat-Spec may not use a job letter. Other divisions do. The job-letter list should come from a spreadsheet and become app configuration.

### Current State

There is an `EstimateNumberService` and sequence table. It supports company/year/type/client-code numbering in principle, but job-letter configuration is not fully modeled, and the concurrency strategy needs hardening.

### Gaps

| Priority | Gap | Impact |
|---|---|---|
| P0 | Job-letter/company-code rules are not fully modeled from the business spreadsheet. | Generated numbers may not match business expectations. |
| P1 | Estimate sequence concurrency is not strongly protected with a rowversion/concurrency token. | Duplicate numbers are possible under simultaneous creation. |
| P1 | Client code source is embedded in rate/estimate data rather than managed as a first-class configuration. | Users may type inconsistent codes. |

### Required Fix

Add a configuration source for job letters and client codes. Confirm whether this should be seeded from the spreadsheet or manually entered for demo. Harden sequence generation.

## 4. Estimate Header And Status Workflow

### Target State

The estimate header should define customer, site, branch, job type, job letter, placeholder MSA number, dates, shift, overtime rules, status, confidence, and lost reason. `Pending` means pending customer approval. Lost status should require a lost reason.

### Current State

The estimate header exists. The API can create and update estimates. Status and confidence are stored. Lost reason fields exist. However, server-side validation is thin, and status vocabulary is inconsistent between requirements and implementation.

### Gaps

| Priority | Gap | Impact |
|---|---|---|
| P0 | Required fields are not consistently enforced server-side. | Bad records can be saved and reporting can become unreliable. |
| P1 | Status vocabulary is not finalized. | Dashboards/tests/stakeholders may see inconsistent language. |
| P1 | Lost reason is not clearly required before Lost save. | Win/loss analytics will be weak. |
| P2 | MSA is placeholder only. | Acceptable for now if stakeholders understand it is future work. |

### Required Fix

Standardize status values for the demo, enforce minimum server validation, and require lost reason when status is Lost.

## 5. Rate Books

### Target State

Rate books represent customer contract billing rates. They should drive labor, equipment, per diem, travel, hotel, lodging, and other billable items. When a customer/location is selected, exact rate book matches should be offered. If none exists, nearest matches and clone flow should be offered. Expired rate books must warn the user.

### Current State

Rate book CRUD exists. Rate book lookup exists. Rate book UI exists. Rate book models support effective/expiration dates, labor, equipment, and expenses. However, rate application is not fully enforced across estimate entry, crew templates, AI, and missing-rate handling.

### Gaps

| Priority | Gap | Impact |
|---|---|---|
| P0 | Loading a rate book does not fully govern all downstream estimate/crew-template rate population. | Estimators can build estimates with missing or wrong rates. |
| P1 | Expired rate-book warning is required but not confirmed across workflows. | Users may bid from expired contracts. |
| P1 | Missing position behavior is not strong enough. | App may silently allow unsupported positions/rates. |
| P1 | AI must answer rate questions from the active rate book. | AI demo can be weak or inaccurate if it does not respect active context. |

### Required Fix

Make active rate book selection explicit in estimate/staffing flow. Add warnings for expired/missing rates. Ensure crew templates and AI use active rate book context.

## 6. Cost Books And Profitability

### Target State

Cost books represent internal cost by region/division. The estimator should use them to understand labor burden, internal equipment cost, internal expenses, breakeven, gross profit, and margin before submitting a bid.

### Current State

Cost book models and UI exist. Cost book reset/default data exists. Job cost analysis exists, mostly around labor burden. However, internal equipment/expense costing and revision traceability are incomplete.

### Gaps

| Priority | Gap | Impact |
|---|---|---|
| P0 | Gross margin units are inconsistent between seed/API/UI/analytics. | Profitability dashboards can be wrong. |
| P1 | Equipment and expense internal costs are not fully demonstrated. | Cost book story is incomplete. |
| P1 | Regional/division cost book selection is not fully integrated into estimate flow. | Users may not see the regional cost difference story. |
| P2 | Cost assumption changes are not revision/audit-traced. | Production auditability gap. |

### Required Fix

Fix gross margin units first. Then strengthen selected cost book flow and demo internal labor/equipment/expense cost impact.

## 7. Labor, Schedule, OT, And DT

### Target State

Labor scheduling and OT/DT rules must support contract-specific logic: after 40 weekly, after 8 daily, after 8 daily plus 40 weekly, double time weekends, and double time Sunday only.

### Current State

Labor grid exists. Stores and components contain schedule/total logic. But logic is duplicated and buried in UI components, which violates the architecture rule.

### Gaps

| Priority | Gap | Impact |
|---|---|---|
| P0 | OT/DT logic is not clearly centralized and tested. | Financial calculations can be inconsistent. |
| P0 | Labor schedule logic is duplicated in components and stores. | Hard to maintain and easy to break. |
| P1 | Contract-specific OT/DT rule selection needs stronger user guidance. | Estimator may choose wrong customer rule. |

### Required Fix

Extract schedule and labor calculations into shared domain logic/composables and use the same logic in estimate, staffing, cost, forecast, and AI.

## 8. Equipment And Expenses

### Target State

Equipment and expense rows should be first-class parts of an industrial estimate. They should support customer billing from rate books and internal cost from cost books.

### Current State

Equipment and expense sections exist. API row endpoints exist. But seed/demo data does not currently showcase them well, and internal cost integration is incomplete.

### Gaps

| Priority | Gap | Impact |
|---|---|---|
| P1 | Demo data lacks realistic equipment and expense rows. | Stakeholders may see the app as labor-only. |
| P1 | Equipment/expense internal cost mapping is incomplete or not obvious. | Profitability story is incomplete. |
| P2 | Missing-rate/missing-cost warnings are not mature. | Production risk. |

### Required Fix

Seed realistic equipment/expenses and ensure summary/job cost/proposal outputs show them clearly.

## 9. Revisions

### Target State

Revisions should be explicit customer/internal snapshots. Users need to compare revisions and restore a previous revision when a customer prefers an earlier version.

### Current State

Revision endpoints and UI exist. Snapshot/restore exists. However, there are duplicate revision endpoints in the API, and comparison is summary-level rather than full row-level.

### Gaps

| Priority | Gap | Impact |
|---|---|---|
| P0 | Duplicate revision routes can cause ambiguous API behavior. | Revision demo/API can fail unpredictably. |
| P1 | Revision comparison is not detailed enough. | Stakeholder story is weaker. |
| P1 | Tests for revision workflow are broken/misclassified. | No reliable evidence. |

### Required Fix

Remove duplicate route conflict, keep one safe revision controller, and make revision compare/restore demo reliable.

## 10. Scenarios

### Target State

Scenarios should allow internal what-if comparison without affecting revenue forecast until promoted.

### Current State

Clone-as-scenario exists in partial form. Scenario records may be excluded from the normal list and there is no complete side-by-side compare/promote workflow.

### Gaps

| Priority | Gap | Impact |
|---|---|---|
| P1 | Scenario workflow is not clearly understood by the business yet. | Risk of building the wrong thing. |
| P1 | Scenario records may disappear from normal estimate browsing. | Demo confusion. |
| P2 | Side-by-side compare and promote are incomplete. | Production feature gap. |

### Required Fix

Clarify scenario use case. For Monday, either hide scenario from demo or show a simple internal what-if story.

## 11. Field Change Orders

### Target State

FCOs should capture out-of-scope work after the original bid. They should be tied to the original estimate/job, sent back for approval, tracked by status, and included in revenue reporting.

### Current State

`FcoEntry` exists in the model and estimate reads, but there is no obvious complete FCO UI/controller workflow.

### Gaps

| Priority | Gap | Impact |
|---|---|---|
| P1 | No complete FCO create/edit/approve workflow. | Important business revenue capture story missing. |
| P1 | FCO revenue not clearly included in dashboards. | Missed revenue visibility. |
| P2 | FCO proposal/customer approval output not built. | Future workflow gap. |

### Required Fix

For demo, create at least a visible FCO section or seed/read-only FCO example. For production, add full FCO workflow.

## 12. Staffing Plans And Conversion

### Target State

Staffing plans can be created years in advance, feed manpower/revenue forecasts, convert to estimates, become non-editable once converted, and no longer count once the estimate exists.

### Current State

Staffing CRUD and conversion exist. Converted plans link to estimates. Forecast utility includes dedupe logic. But seeded staffing rows are incomplete and converted edit-lock behavior needs verification.

### Gaps

| Priority | Gap | Impact |
|---|---|---|
| P0 | Seeded staffing rows do not fully support forecast math. | Manpower dashboard can look wrong. |
| P1 | Converted staffing should be locked from editing. | Source-of-truth rule may be violated. |
| P1 | Forecast should clearly show staffing vs estimate source and dedupe. | Stakeholder trust issue. |

### Required Fix

Fix staffing seed shape, confirm converted lock behavior, and make manpower forecast story explicit.

## 13. Crew Templates

### Target State

Crew templates should be reusable packages of positions/quantities that populate estimates/staffing using the active rate book and cost book.

### Current State

Crew template model/controller/UI exist. However, navigation placement and estimate/staffing application flow need polish. Some "Load Crew" controls appear disconnected.

### Gaps

| Priority | Gap | Impact |
|---|---|---|
| P1 | Crew Templates need clear navigation placement, likely Library. | Users may not find them. |
| P1 | Load Crew button appears not wired in some forms. | Visible broken control. |
| P1 | Template application must use active rate book/cost book. | Template story weak if rates do not populate. |

### Required Fix

Wire Load Crew and put Crew Templates in a clear Library/setup location.

## 14. AI Sidecar

### Target State

AI should showcase major app capability: create estimates/staffing from natural language, parse RFQs, ask clarifying questions, load rate books, apply templates, warn about expired rates, analyze margin, compare historical jobs, summarize revision deltas, draft FCOs, and answer manpower/revenue questions.

### Current State

AI chat and RFQ parse endpoints exist. AI sidebar exists. The app has AI service logic and suggestions. But AI company scoping and deep workflow integration need review.

### Gaps

| Priority | Gap | Impact |
|---|---|---|
| P0 | AI appears to use client-supplied company context in some places. | Company data isolation risk. |
| P1 | AI does not yet fully drive rate-book lookup/template application/FCO/revision workflows. | Demo may feel like chat instead of real app intelligence. |
| P1 | AI tests are not safe/reliable today. | No evidence for AI claims. |
| P2 | Durable acknowledgement for AI anomaly warnings is not built. | Production audit gap. |

### Required Fix

For Monday, focus AI on a few polished flows: create estimate from prompt, find/load rate book, apply template, ask clarifying OT question, explain margin, compare revisions, and answer manpower question.

## 15. Financial Dashboard

### Target State

The financial dashboard should show what leadership cares about: pipeline value, awarded revenue, pending approval, lost revenue, win rate, average margin, low-margin bids, breakeven risk, future revenue, top clients, lost reasons, forecast variance, FCO revenue, and expired rate-book exposure.

### Current State

Dashboard endpoint and UI exist. KPIs include pipeline, won YTD, margin, win rate, jobs by status, and margin buckets. However, data correctness depends on seed quality and margin unit consistency.

### Gaps

| Priority | Gap | Impact |
|---|---|---|
| P0 | Gross margin data inconsistency affects dashboard truth. | Leadership KPIs wrong. |
| P1 | FCO revenue not clearly represented. | Missed revenue story absent. |
| P1 | Expired rate-book exposure not visible. | Contract risk story absent. |
| P1 | Lost reason analytics are shallow without required lost reasons. | Win/loss story weak. |

### Required Fix

Fix margin units, seed meaningful won/lost/pending data, and add or simulate FCO/expired-rate-book indicators for demo.

## 16. Revenue Forecast

### Target State

Revenue forecast should combine awarded estimates, weighted pending estimates, optionally staffing plans, and FCO revenue. It should support filters and eventually snapshots/variance/WIP imports.

### Current State

Revenue Forecast page exists. Some forecast logic exists. Some values are stubbed or incomplete. Projections/WIP/snapshot concepts are not implemented.

### Gaps

| Priority | Gap | Impact |
|---|---|---|
| P1 | Margin in revenue forecast appears stubbed or incomplete. | Forecast confidence reduced. |
| P1 | Snapshot/variance workflow missing. | Requirement gap. |
| P1 | Staffing/estimate dedupe must be verified with realistic data. | Forecast may double count. |
| P2 | WIP import and adjustments missing. | Future production gap. |

### Required Fix

For Monday, show a credible forecast with awarded/pending/staffing data and dedupe. Defer WIP/snapshot unless already easy to restore.

## 17. Manpower Forecast

### Target State

Manpower forecast should show current and future labor demand by position/craft and date range, including freed-up workers from ending jobs and future hiring gaps.

### Current State

Manpower Forecast page exists. Forecast utility includes logic for estimates/staffing and converted dedupe. But seed data is not rich enough and some filters appear incomplete.

### Gaps

| Priority | Gap | Impact |
|---|---|---|
| P0 | Current seed does not populate staffing schedule/hours enough. | Forecast may look empty/wrong. |
| P1 | Drill-down to contributing jobs/plans is limited. | Harder for stakeholders to trust numbers. |
| P1 | Filters may be present but inert/incomplete. | Demo risk if clicked. |

### Required Fix

Seed realistic date-ranged labor demand and verify forecast output manually before demo.

## 18. Calendar

### Target State

Calendar should show estimates and staffing plans over time with day/week/month views, filtering, status/source badges, and click-through.

### Current State

Calendar page exists and can show records, but day/week views and richer filtering are incomplete.

### Gaps

| Priority | Gap | Impact |
|---|---|---|
| P1 | Day/week views missing or incomplete. | Requirement gap. |
| P1 | Filters by craft/shift/customer/status are incomplete. | Demo limitation. |
| P2 | Converted staffing visual tagging needs verification. | Dedupe transparency gap. |

### Required Fix

For Monday, use month view if it works and seed data with visible spans.

## 19. Proposal PDF And Print

### Target State

Proposal PDF should be customer-safe and exclude internal margin/cost. App should support dark/light mode and print-friendly output.

### Current State

Proposal PDF service exists and appears to exclude internal cost/margin. Light mode has contrast risks. Print readiness is not fully verified.

### Gaps

| Priority | Gap | Impact |
|---|---|---|
| P1 | Light mode contrast issues exist in shared headers. | Visual quality risk. |
| P1 | Print-friendly views not verified. | Demo/usage risk. |
| P2 | Proposal email/staged send workflow not built. | Future requirement gap. |

### Required Fix

Verify PDF output after app runs. Fix obvious light-mode contrast before screenshots.

## 20. Frontend Architecture

### Target State

Views orchestrate. Components render UI. Composables own reusable behavior. Services own API access. Stores own shared state. Utilities own shared helpers.

### Current State

Multiple views/components call APIs directly and contain business logic. RateBookView, CostBookView, RevenueForecastView, and ManpowerForecastView are especially large. Labor/schedule/summary math is duplicated in UI components.

### Gaps

| Priority | Gap | Impact |
|---|---|---|
| P0 | Direct API calls are widespread in views/components. | Repeats prior app audit issue. |
| P0 | Large multi-concern views are already forming. | Maintenance/testing risk. |
| P0 | Business calculations live in components. | Hard to test and easy to break. |
| P1 | Screen shells and empty/loading/error states are inconsistent. | Visual quality risk. |
| P1 | Mobile/responsive issues likely. | Demo risk on varied screens. |

### Required Fix

Do not start a broad rewrite before Monday. For active demo flows, extract services/composables only where needed, and stop adding new logic directly to giant views.

## 21. Test And QA

### Target State

Mocked tests should not hit the DB. Live tests should run only against an isolated disposable DB. Visual tests should produce evidence. Test agents should block unsafe tests.

### Current State

All configured Playwright commands run `globalSetup`, which logs in, seeds data, and calls cost-book reset. Some tests are mislabeled as mocked but hit live APIs. Live estimate tests use selectors that do not exist.

### Gaps

| Priority | Gap | Impact |
|---|---|---|
| P0 | No configured Playwright command is DB-safe today. | Cannot safely run tests against shared DB. |
| P0 | Test setup mutates data before every run. | Existing database could be changed. |
| P1 | Live estimate selectors are broken. | Tests will fail even if app works. |
| P1 | Mocked vs live test classification is inaccurate. | False confidence risk. |

### Required Fix

Split mocked/live configs, disable global setup for mocked tests, quarantine live tests until isolated DB exists, then fix selectors.

## 22. Demo Readiness Gap

### Where We Are

The app has enough surface area to tell a good story, but it is not yet safe or polished enough to trust live without controlled setup.

### Where We Need To Be For Monday

The demo should show:

1. Dashboard with realistic revenue, margin, win/loss, and manpower data.
2. Current awarded turnaround job active around April 24, 2026.
3. Estimate with header, rate book, labor, equipment, expenses, cost book, margin.
4. AI creating or assisting an estimate from natural language.
5. AI loading/explaining rate book and asking clarifying questions.
6. Revision save, compare, and restore.
7. Staffing plan conversion to estimate.
8. Manpower forecast dedupe after conversion.
9. Revenue forecast and lost revenue.
10. FCO revenue capture story.
11. Proposal PDF.

### Biggest Demo Blockers

| Priority | Blocker | Required Action |
|---|---|---|
| P0 | No isolated DB confirmed. | Create/use `StrongholdEstimating_Demo`. |
| P0 | Backend build blocked by .NET SDK mismatch. | Install .NET 7 SDK or upgrade cleanly. |
| P0 | Local connection strings still Docker-based. | Point local config to SQL Express demo DB. |
| P0 | Margin units/data inconsistent. | Fix before seeding/demo dashboard. |
| P0 | Duplicate revision endpoints. | Fix before revision demo. |
| P1 | Seed data incomplete. | Add realistic dataset. |
| P1 | AI needs focused demo flows. | Polish 3-5 AI moments instead of everything. |
| P1 | Test setup unsafe. | Do not run live tests until fixed. |

## 23. Recommended Fix Order

1. Confirm isolated SQL Express DB name and rules.
2. Fix local config and EF migration bootstrap.
3. Fix gross margin precision/units.
4. Fix duplicate revision routes.
5. Fix frontend API port mismatch.
6. Create idempotent realistic demo seed for isolated DB.
7. Seed realistic rates, costs, estimates, staffing, revisions, equipment, expenses, and FCO examples.
8. Verify dashboard, estimate form, revision flow, staffing conversion, manpower forecast, revenue forecast, and proposal PDF manually.
9. Focus AI on high-value demo workflows.
10. Split safe mocked tests from DB-mutating live tests.
11. Only run live tests against isolated DB.
12. Begin architecture cleanup on the flows touched for demo.

## 24. What Can Wait Until After Monday

1. Full scenario compare/promote workflow.
2. Full WIP import.
3. Full forecast snapshot/archive/variance engine.
4. Full FCO approval lifecycle if a simplified FCO demo is enough.
5. Full production-grade audit log.
6. Full role/permission matrix.
7. Full light-mode/print polish beyond obvious blockers.
8. Broad frontend architecture refactor.
9. Full Playwright live suite stabilization.

## 25. What Should Not Wait

1. Isolated DB.
2. Safe seed path.
3. Gross margin fix.
4. Revision route fix.
5. Frontend/backend URL alignment.
6. Realistic demo data.
7. AI demo storyline.
8. No test runs against non-disposable DB.
9. Clear explanation of what is built vs partial.

