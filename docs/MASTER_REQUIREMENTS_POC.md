# Stronghold Enterprise Estimating POC Master Requirements

Document ID: `STH-EST-POC-MASTER-REQ-001`  
Version: `1.0.0`  
Status: `Frozen for POC delivery`  
Date: `2026-03-25`  
Owner: `Product + Engineering`  
Primary repo: `stronghold-enterprise-estimating`  

---

## 1) Purpose and Scope

This document is the single source of truth for POC behavior, data flow, and acceptance criteria for:

1. Estimating module
2. Staffing plan module and staffing-to-estimate conversion
3. Rate library
4. Cost book and internal cost analysis
5. Manpower forecast
6. AI sidecar agent
7. Analytics dashboard with financial forecast
8. Calendar schedule view

`REQ-GOV-001`: This file governs POC behavior.  
`REQ-GOV-002`: If implementation conflicts with this document, this document wins until amended.  
`REQ-GOV-003`: Each implementation task must cite requirement IDs from this document.  
`REQ-GOV-004`: Silent scope drift is not allowed.  
`REQ-GOV-005`: Any change to requirements must be logged in an amendment section before merge.

---

## 2) Non-Negotiable Boundaries

`REQ-GOV-010`: Legacy apps are read-only references.  
`REQ-GOV-011`: Legacy databases are not touched by this POC.  
`REQ-GOV-012`: All local testing uses isolated Docker SQL only.  
`REQ-GOV-013`: Runtime target remains `.NET 7`.  
`REQ-GOV-014`: No schema/data manipulation is allowed for the purpose of forcing tests to pass.  
`REQ-GOV-015`: If data is wrong, root-cause logic and fix the logic.  
`REQ-GOV-016`: Company data isolation is mandatory in UI, API, exports, and AI responses.

---

## 3) POC Objectives and Success Criteria

`REQ-OBJ-001`: Deliver demo-ready end-to-end flows for all in-scope modules.  
`REQ-OBJ-002`: Preserve enterprise template UX consistency.  
`REQ-OBJ-003`: Demonstrate deterministic financial calculations and forecast behavior.  
`REQ-OBJ-004`: Demonstrate AI-assisted estimate/staffing workflows with safe confirmation.

Demo-ready means:

1. User can create staffing plans and estimates from UI and AI.
2. User can convert staffing to estimate with traceable linkage.
3. User can load rate books and templates with customer/location-aware logic.
4. User can see internal cost and margin from cost-book data.
5. User can view manpower and financial forecast outcomes with filters.
6. User can show analytics-team financial dashboard behaviors.

Module pass/fail gates:

1. Estimating gate:
- Pass when user can create, save, reopen, revise, and status-change estimates with deterministic totals.
- Fail if reopened totals drift, required fields are bypassed, or won/lost logic does not flow to reports.
2. Rate Library gate:
- Pass when exact match load, nearest suggestion, and clone flow work with clear confirmation.
- Fail if customer/location rates are not correctly applied to template or manual rows.
3. Cost Book gate:
- Pass when direct/indirect labor, burden mix, and internal cost propagation produce stable margin outputs.
- Fail if internal costs are inferred from billing rates or burden formulas are opaque/incorrect.
4. Manpower Forecast gate:
- Pass when current/future demand, shortage/surplus, peak month/craft, and drill-down all reconcile.
- Fail if converted staffing is double-counted or jobs-today parity breaks across modules.
5. AI Sidecar gate:
- Pass when NL form-fill, clarification flows, rate-book intelligence, and scoped Q&A behave correctly.
- Fail if AI writes ambiguous actions without confirmation or crosses company data boundaries.
6. Analytics Financial Forecast gate:
- Pass when composition sources, filters, snapshots, and variance views are consistent and auditable.
- Fail if totals cannot trace to source records or multi-company permissions are violated.

---

## 4) Roles and Access (POC Policy)

`REQ-SEC-001`: Every read/write path is company-scoped.  
`REQ-SEC-002`: No company can view another company's operational data.  
`REQ-SEC-003`: Analytics cross-company visibility is restricted to analytics-role users only.  
`REQ-SEC-004`: Access denials must return explicit authorization errors.  
`REQ-SEC-005`: Company context must be selected before module interaction.

POC role defaults:

1. Estimator: full estimate/staffing/rate/cost usage for assigned company.
2. PM: read/write estimate statuses and planning views for assigned company.
3. Analytics User: portfolio reporting access across authorized companies.
4. Admin: configuration and master-data actions within authorized scope.

---

## 5) Domain Model and Glossary

`REQ-DM-001`: Canonical aggregates are:

1. `Estimate`
2. `StaffingPlan`
3. `EstimateRevision`
4. `RateBook`
5. `CostBook`
6. `CrewTemplate`
7. `FCOEntry`
8. `ForecastSnapshot`
9. `Projection`
10. `WIPImport`

`REQ-DM-002`: Key glossary terms are standardized:

1. `Awarded`: won work counted as active revenue/workload.
2. `Proposed/Pending`: pipeline work not yet won/lost.
3. `Lost`: excluded from positive revenue forecast and reason-coded.
4. `Converted`: staffing record retained but deduped where linked estimate exists.
5. `Burden`: internal labor overhead model.
6. `Baseline vs Adjustment`: original value vs change entries with audit trail.

---

## 6) Numbering Contract

`REQ-NUM-001`: Estimate number format is `{job_letter?}-{YY}-{sequence}-{client_code}`.  
`REQ-NUM-002`: Staffing number format is `SP-{job_letter?}-{YY}-{sequence}-{client_code}`.  
`REQ-NUM-003`: Sequence is collision-safe and scoped by company and year.  
`REQ-NUM-004`: `client_code` is sourced from customer/rate-book configuration.  
`REQ-NUM-005`: Number generation must be deterministic and auditable.

Examples:

1. `H-26-0001-SHL`
2. `SP-H-26-0001-SHL`

---

## 7) Status Model and Transitions (POC Defaults)

`REQ-STAT-001`: Estimate statuses are `draft`, `proposed`, `awarded`, `lost`, `archived`.  
`REQ-STAT-002`: Staffing statuses are `draft`, `active`, `converted`, `archived`.  
`REQ-STAT-003`: Transition to `lost` requires reason code and optional free-text detail.  
`REQ-STAT-004`: Conversion updates staffing status to `converted` and retains source record.  
`REQ-STAT-005`: Status transitions are logged with actor and timestamp.

---

## 8) Estimating Module Workflow

`REQ-EST-001`: User can create an estimate from blank.  
`REQ-EST-002`: Required header fields are validated before save/submit actions.  
`REQ-EST-003`: Billing sections include labor, equipment, and per diem/expenses.  
`REQ-EST-004`: Internal job cost section auto-populates from cost-book logic.  
`REQ-EST-005`: Save/reopen preserves all rows, settings, and totals.  
`REQ-EST-006`: Estimate log stores records with search/filter support.  
`REQ-EST-007`: Proposal generation is supported in POC form.  
`REQ-EST-008`: Email action is supported in POC-safe mode (configurable non-prod behavior).  
`REQ-EST-009`: Won/lost state affects downstream reporting logic.  
`REQ-EST-010`: Revision create/compare/restore is required for estimates.

Section order is fixed:

1. Header
2. Labor
3. Equipment
4. Per Diem/Expenses
5. Internal Job Cost Analysis
6. Summary

`REQ-EST-011`: Sections are collapsible without data loss.  
`REQ-EST-012`: Labor schedule views are projections of one canonical dataset, not independent copies.

---

## 9) Staffing Module Workflow

`REQ-SP-001`: User can create staffing plans with labor-planning intent.  
`REQ-SP-002`: Staffing plans have independent numbering.  
`REQ-SP-003`: Staffing plans store enough detail for manpower forecasting.  
`REQ-SP-004`: Staffing plans can convert to estimate while preserving original staffing record.  
`REQ-SP-005`: Converted estimate stores staffing plan reference ID and number.  
`REQ-SP-006`: Converted staffing demand is excluded where linked estimate demand is included.  
`REQ-SP-007`: Toggle between estimate/staffing modes must be explicit and reliable.

---

## 10) Rate Library Contract

`REQ-RATE-001`: Rate books are customer/location specific.  
`REQ-RATE-002`: Exact customer/location matches prompt to load rates.  
`REQ-RATE-003`: If no exact match exists, nearest candidates are suggested.  
`REQ-RATE-004`: User can clone nearest rate book to create new customer/location rate book.  
`REQ-RATE-005`: Templates apply using active selected customer rates.  
`REQ-RATE-006`: For multi-rate items, system asks user to choose a specific rate or all options.  
`REQ-RATE-007`: Missing-rate behavior must guide user to create a rate book or use standard baseline.

---

## 11) Cost Book Contract

`REQ-COST-001`: Cost book includes direct labor and indirect labor sections.  
`REQ-COST-002`: Burden supports mixed inputs (`%` and `$ per hour`).  
`REQ-COST-003`: Labor/equipment/expense internal rates are sourced from cost book only.  
`REQ-COST-004`: Internal section auto-populates from estimate top sections.  
`REQ-COST-005`: Burden formulas and totals are visible and traceable.  
`REQ-COST-006`: Cost assumption changes are revision-traceable in affected estimates.  
`REQ-COST-007`: POC includes standard baseline cost book per company.

---

## 12) Calculation and Logic Contracts

`REQ-CALC-001`: Schedule model is single canonical date-based structure.  
`REQ-CALC-002`: OT/DT logic is deterministic and shared by billing and internal cost views.  
`REQ-CALC-003`: Billing subtotal formula is deterministic from section totals.  
`REQ-CALC-004`: Discount is applied before tax.  
`REQ-CALC-005`: Grand total is deterministic after rounding rules.  
`REQ-CALC-006`: Internal total uses cost-book mapping only.  
`REQ-CALC-007`: Profit and margin are derived from grand total minus internal total.  
`REQ-CALC-008`: Save/reopen must preserve exact totals (within defined rounding precision).

Rounding default:

1. Currency rounded to 2 decimals.
2. Percent display to 2 decimals.
3. Stored precision may exceed display precision but recomputation must remain stable.

---

## 13) AI Sidecar Agent Contract

`REQ-AI-001`: Agent supports natural-language create/update for staffing and estimates.  
`REQ-AI-002`: Agent must ask clarifying questions before ambiguous write actions.  
`REQ-AI-003`: Agent handles misspellings and varied phrasing.  
`REQ-AI-004`: Agent normalizes location patterns (`city state`, `city, ST`, `Location: city, ST`).  
`REQ-AI-005`: Agent auto-checks rate-book availability using extracted customer/location context.  
`REQ-AI-006`: Agent prompts to use exact rates when available.  
`REQ-AI-007`: Agent suggests nearest rates and clone flow when exact match is missing.  
`REQ-AI-008`: Agent uses active customer rates when applying templates.  
`REQ-AI-009`: Agent answers data-grounded questions for rates, jobs today, manpower, and forecast.  
`REQ-AI-010`: Agent responses are company-scoped.  
`REQ-AI-011`: Irreversible actions require explicit confirmation.  
`REQ-AI-012`: Agent returns structured confirmation of changed fields after write actions.

Required clarification example:

1. Input: "add a helper"
2. Agent behavior: asks which helper type (`pipefitter`, `welding`, `boilermaker`, etc.)
3. Agent applies selection only after user confirmation.

---

## 14) Calendar Contract

`REQ-CAL-001`: Calendar renders inclusive start/end spans for staffing and estimates.  
`REQ-CAL-002`: Calendar supports day/week/month views.  
`REQ-CAL-003`: Calendar filters include company, status, customer, shift, craft, and time window.  
`REQ-CAL-004`: Calendar entries display source and status badges.  
`REQ-CAL-005`: Clicking calendar item opens source record in correct mode.  
`REQ-CAL-006`: Jobs-today logic is shared with reports/manpower and AI answer logic.  
`REQ-CAL-007`: Converted staffing records are visually tagged and logically deduped.

---

## 15) Manpower Forecast Contract

`REQ-MF-001`: Current headcount derives from active awarded work.  
`REQ-MF-002`: Future demand is computed by month/craft/position from staffing + estimate pipeline rules.  
`REQ-MF-003`: Forecast displays shortage/surplus versus current baseline.  
`REQ-MF-004`: Forecast identifies peak month and peak craft demand.  
`REQ-MF-005`: Forecast supports drill-down to contributing jobs/plans.  
`REQ-MF-006`: Converted staffing-to-estimate dedupe is enforced in forecast calculations.

Visualization requirements:

1. Tabular forecast view with filters.
2. Line chart or equivalent trend view.
3. Clear indicators for positive/negative staffing variance.

---

## 16) Analytics Dashboard Contract (Financial Forecast)

`REQ-AN-001`: Dashboard supports revenue forecast composition from:

1. Awarded estimates
2. Proposed/pending pipeline
3. Non-converted staffing where configured
4. Projections
5. Adjustments
6. FCO entries
7. WIP inputs

`REQ-AN-002`: Dashboard supports time filters (`day`, `month`, `quarter`, `year`, `YTD`, future years).  
`REQ-AN-003`: Dashboard supports dimensions (`company`, `customer`, `site`, `code`, `work type`, `confidence`, `director/VP`).  
`REQ-AN-004`: Work type taxonomy includes `lumpsum`, `T&M`, `maintenance`, `installation`, `consulting`, `repair`, `other`.  
`REQ-AN-005`: Forecast ties to job number and quote/PO traceability where data exists.  
`REQ-AN-006`: Snapshot/archive and variance comparison are required.  
`REQ-AN-007`: Separate analytics-team portfolio view is required for authorized users.  
`REQ-AN-008`: Lost bids reduce income forecast and retain reason analytics.

---

## 17) Additional Enterprise Fields and Metadata

`REQ-FLD-001`: MSA number.  
`REQ-FLD-002`: Contract summary.  
`REQ-FLD-003`: Billing summary.  
`REQ-FLD-004`: Payment terms.  
`REQ-FLD-005`: Invoice delivery method (`mail` or `email`).  
`REQ-FLD-006`: Customer price group.  
`REQ-FLD-007`: Discount structure metadata.  
`REQ-FLD-008`: Craft code mapping references (internal and customer craft code links).  
`REQ-FLD-009`: Percent-or-dollar adjustment input per line where applicable.

---

## 18) Data Flow and Integration Map

`REQ-FLOW-001`: Canonical data path is `UI -> API -> DB -> forecast/analytics -> AI`.  
`REQ-FLOW-002`: Derived calculations recompute on relevant event triggers only.  
`REQ-FLOW-003`: Recompute triggers and cache invalidation events are documented.  
`REQ-FLOW-004`: Financially sensitive writes produce audit entries with actor/time/old/new context.

---

## 19) API Contract Appendix (POC-Level)

`REQ-API-001`: Endpoint groups include:

1. Session/tenant context
2. Estimates
3. Staffing plans
4. Revisions
5. Rate books and templates
6. Cost books
7. Calendar
8. Manpower forecast
9. Financial analytics
10. FCO and WIP
11. AI orchestration

`REQ-API-002`: APIs return clear validation error payloads.  
`REQ-API-003`: APIs enforce company scope checks server-side, not just UI-side.  
`REQ-API-004`: APIs support deterministic read-after-write behavior for POC workflows.

---

## 20) Playwright and QA Contract

`REQ-QA-001`: End-to-end tests must cover all in-scope modules.  
`REQ-QA-002`: Tests verify UI behavior, business logic, and persisted data correctness.  
`REQ-QA-003`: Visual regression coverage is required for key screens/states.  
`REQ-QA-004`: AI test suite includes high-variance natural-language prompts and misspellings.  
`REQ-QA-005`: Isolation tests verify company data segregation.  
`REQ-QA-006`: Evidence artifacts are mandatory for acceptance:

1. Playwright traces
2. Screenshots/videos
3. API/DB reconciliation evidence

`REQ-QA-007`: No result is accepted if data was manipulated purely to pass tests.

Suggested scenario families:

1. Create estimate from NL prompt with both shifts.
2. Ambiguous helper disambiguation and selection.
3. Exact rate-book match load flow.
4. Missing match nearest suggestion and clone flow.
5. Template apply with customer-specific rates.
6. Staffing-to-estimate conversion and dedupe validation.
7. Revenue/manpower forecast reconciliation under filters.
8. Analytics snapshot and variance flow.

---

## 21) Performance, Reliability, and Operational Safeguards

`REQ-NFR-001`: POC target response time for standard reads is under 2 seconds in local dev conditions.  
`REQ-NFR-002`: Long-running aggregates provide progress or clear completion state.  
`REQ-NFR-003`: Failures return actionable errors and do not corrupt record state.  
`REQ-NFR-004`: Partial failures must preserve data integrity and auditability.  
`REQ-NFR-005`: Retry behavior must avoid duplicate financial writes.

---

## 22) Change Control and Traceability

`REQ-CC-001`: Every implementation item references requirement IDs.  
`REQ-CC-002`: Requirement amendments are logged before behavior changes are merged.  
`REQ-CC-003`: Traceability map links `Requirement -> Implementation -> Tests -> Evidence`.  
`REQ-CC-004`: Unmapped code changes are not considered complete.

Work item template (required):

1. Work item ID
2. Requirement IDs
3. In-scope files
4. Out-of-scope statement
5. Test IDs
6. Evidence links
7. Amendment reference (if any)

---

## 23) Ask-Before-Assume Protocol

`REQ-ASK-001`: Implementer must ask before assuming on financially material ambiguity.  
`REQ-ASK-002`: Implementer must ask before assuming on access boundary ambiguity.  
`REQ-ASK-003`: Implementer must ask before assuming on AI write behavior ambiguity.

Mandatory ask-before-assume topics:

1. Forecast weighting defaults.
2. Nearest-rate matching threshold.
3. Analytics cross-company permission boundaries.
4. Email/proposal external delivery mode in POC.
5. Any behavior that changes financial totals.

Default if unanswered:

1. Choose safest auditable behavior.
2. Log assumption in `Open Decisions`.
3. Highlight assumption in change summary.

---

## 24) Open Decisions Table (POC)

| Decision ID | Topic | Current POC Default | Escalate When |
|---|---|---|---|
| `OD-001` | Forecast confidence weighting | Equal weighting unless explicit per-record confidence provided | Financial output differs materially from user expectation |
| `OD-002` | Nearest-rate match tolerance | Conservative string/location similarity with explicit confirmation | Multiple near-equal candidates appear |
| `OD-003` | Analytics role boundary specifics | Restrict multi-company view to analytics-role users | Additional role groups request access |
| `OD-004` | Proposal/email outbound behavior | POC-safe mode with non-prod constraints | Production delivery or legal controls required |
| `OD-005` | Rounding edge cases | 2-decimal currency display, stable deterministic totals | Regulatory or contract-specific rounding policy required |

---

## 25) Implementation Flow While Coding Continues

`REQ-IMP-001`: Freeze this master doc first.  
`REQ-IMP-002`: Use `REQ-<DOMAIN>-<###>` IDs in all new implementation items.  
`REQ-IMP-003`: Map active backend/frontend tasks to IDs before completion.  
`REQ-IMP-004`: Keep unresolved items in `Open Decisions` and escalate quickly.  
`REQ-IMP-005`: Continue coding in parallel as long as ID mapping and safeguards are followed.

---

## 26) Assumptions and Defaults

1. This is a highly detailed POC contract, not final enterprise policy lock.
2. Runtime stays `.NET 7`.
3. Legacy systems remain untouched.
4. Isolated Docker SQL is mandatory for test runs.
5. If conflict occurs, this master contract governs until amended.

---

## 27) AI Prompt Variation Appendix (Seed Cases)

Use these for QA and regression:

1. "create new estimate for BP in Bakersfield California starting June 6th for 12 days branch 300 both shifts"
2. "new estimate Valero branch 300 both shifts 12h start 5/31/26 8 days"
3. "new staffing plan Shell Pasadena CA use turnaround template start date x for 13 days"
4. "add a helper" (must clarify helper type)
5. "what jobs are going on today"
6. "how many pipefitters are in the field right now"
7. "do we have BP Pasadena rates, if not use Houston and clone"
8. Misspelled variation cases (`bakersfeild`, `pasedena`, `boilermakr`) with expected robust parsing.

---

## 28) Amendment Log (Start Here)

| Amendment ID | Date | Requested By | Scope | Summary | Approved By |
|---|---|---|---|---|---|
| `AMD-20260325-001` | 2026-03-25 | Product | Initial freeze | Created comprehensive POC master requirements and governance contract | Pending |
| `AMD-20260325-002` | 2026-03-25 | Product/Engineering | Enterprise feature gap analysis | Added §29 Historical Benchmarking, §30 Scenario Comparison, §31 Win/Loss Analytics, §32 Proposal Generation, §33 RFQ Document Parsing, §34 Rate Anomaly Detection | Approved |
| `AMD-20260327-001` | 2026-03-27 | Engineering | Status vocabulary drift | REQ-STAT-001 specifies `proposed`/`archived`; implementation uses `Pending`/`Canceled`. Tests currently assert against implementation values. Vocabulary reconciliation deferred to pre-production sprint. Sentinel test in `live-estimates.spec.ts` will alert when alignment occurs. | Pending |

---

## 29) Historical Benchmarking

`REQ-BENCH-001`: System maintains a queryable history of all saved estimates per company.
`REQ-BENCH-002`: When building an estimate, user can select past estimates as benchmarks from a lookup.
`REQ-BENCH-003`: System compares current labor rates, hours, and subtotals against selected benchmarks in real-time.
`REQ-BENCH-004`: Deviations beyond a configurable threshold (default 20%) are flagged visually on the affected row.
`REQ-BENCH-005`: AI sidecar can answer historical benchmark questions: "what did we average for Pipefitter Foreman on BP turnarounds?"
`REQ-BENCH-006`: Benchmark data is strictly company-scoped and never crosses company boundaries.
`REQ-BENCH-007`: Benchmark comparison is non-blocking — user can proceed without selecting a benchmark.

---

## 30) Scenario / What-If Comparison

`REQ-SCEN-001`: User can clone any saved estimate as an alternate "Scenario" from the estimate form or list.
`REQ-SCEN-002`: Scenario estimates are linked to the parent with a visible "Scenario of #{parent}" badge.
`REQ-SCEN-003`: Side-by-side comparison view shows grand total delta and per-section deltas between parent and scenario.
`REQ-SCEN-004`: Scenario can be promoted to become the primary estimate; the relationship badge updates accordingly.
`REQ-SCEN-005`: Scenario estimates appear in the estimate list with a "Scenario" badge.
`REQ-SCEN-006`: Scenario estimates are excluded from analytics and revenue forecasts until promoted to primary.
`REQ-SCEN-007`: Multiple scenarios per parent are supported.

---

## 31) Win/Loss Pricing Analytics

`REQ-WLA-001`: Analytics dashboard includes a dedicated win/loss analysis view.
`REQ-WLA-002`: Win/loss view filters by client, work type, branch, date range, and margin band.
`REQ-WLA-003`: System computes win rate percentage grouped by margin band (e.g., under 15%, 15–25%, over 25%).
`REQ-WLA-004`: System identifies clients and work types with the highest and lowest win rates.
`REQ-WLA-005`: Lost reason codes (per REQ-STAT-003) are aggregated and ranked in this view.
`REQ-WLA-006`: This view is available to Estimator, PM, and Admin roles only.
`REQ-WLA-007`: Win/loss view data is company-scoped; cross-company analytics require Analytics role per REQ-SEC-003.

---

## 32) Proposal Generation

`REQ-PROP-001`: User can generate a formatted proposal document from any estimate in non-lost, non-archived status.
`REQ-PROP-002`: Proposal output includes: company branding, estimate number, client info, job dates/location, scope summary, billing subtotals by section (labor, equipment, expenses), grand total, and standard terms.
`REQ-PROP-003`: Proposal must not expose internal cost, burden calculations, or gross margin data.
`REQ-PROP-004`: Primary output format is PDF; Word document export is secondary.
`REQ-PROP-005`: POC-safe email mode: proposal is staged for user review before any external delivery action.
`REQ-PROP-006`: Proposal template is configurable per company (logo, terms, contact block).

---

## 33) RFQ Document Parsing (AI Extension)

`REQ-RFQ-001`: User can upload a scope-of-work PDF or Word document to the AI sidecar while on an estimate or staffing form.
`REQ-RFQ-002`: AI extracts where present: job name, client, location, start date, duration/end date, shift requirements, and position/craft list.
`REQ-RFQ-003`: Extracted fields are presented as a structured confirmation dialog before any fields are populated.
`REQ-RFQ-004`: User can edit any extracted value before confirming.
`REQ-RFQ-005`: Confirmed positions trigger the rate book lookup flow defined in REQ-AI-005 through REQ-AI-008.
`REQ-RFQ-006`: Parsing is non-destructive — existing estimate data is not overwritten without explicit user confirmation.
`REQ-RFQ-007`: If extraction confidence is low for a field, that field is left blank with a note rather than populated with a guess.

---

## 34) Rate Anomaly Detection (AI Extension)

`REQ-ANOM-001`: AI sidecar monitors entered labor rates against historical average rates for the same position and client combination.
`REQ-ANOM-002`: Rates deviating more than a configurable threshold (default 20%) from the historical average trigger an inline warning on the affected row.
`REQ-ANOM-003`: Warning message includes the historical average and the deviation percentage.
`REQ-ANOM-004`: User can acknowledge the warning (suppresses it for the session) or override it; all acknowledgments are logged with actor and timestamp.
`REQ-ANOM-005`: Anomaly detection is non-blocking — user can save the estimate regardless of active warnings.
`REQ-ANOM-006`: Anomaly detection requires at least 3 historical data points for a position/client pair to fire; below that threshold, no warning is shown.
