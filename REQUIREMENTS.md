# Stronghold Enterprise Estimating — Requirements

> **Visual Reference:** Every section below has a corresponding screenshot in
> [`docs/legacy-app/README.md`](docs/legacy-app/README.md). Exact rates, field names, and UI
> layouts are captured there from live legacy-app screenshots.
>
> **Demo Deadline:** Monday April 27, 2026.
> **Legacy App:** Cat-Spec / Bid Manager v2.0 — READ ONLY, never modify.

---

## How To Read This Document

This file is written so that anyone — a developer who has never seen the app, a new estimator,
a project manager, or a stakeholder — can understand exactly what the app does and why.

Every section follows this pattern:
1. **What it is** — a plain-English explanation of the feature
2. **Why it matters** — why the business needs it
3. **Exact behavior** — specific fields, rules, calculations, and UI details
4. **Examples** — real numbers or scenarios so there is zero ambiguity

---

## Table of Contents

1. [Business Domain](#1-business-domain)
2. [Terminology & Key Concepts](#2-terminology--key-concepts)
3. [Login & Company Context](#3-login--company-context)
4. [Settings & Configuration](#4-settings--configuration)
5. [Estimate Numbering](#5-estimate-numbering)
6. [Staffing Plan Numbering](#6-staffing-plan-numbering)
7. [Estimate Form — Header](#7-estimate-form--header)
8. [Estimate Form — Toolbar & Actions](#8-estimate-form--toolbar--actions)
9. [Estimate Form — Lock & Edit Behavior](#9-estimate-form--lock--edit-behavior)
10. [Estimate Form — Labor Section](#10-estimate-form--labor-section)
11. [Estimate Form — Equipment Section](#11-estimate-form--equipment-section)
12. [Estimate Form — Expenses (Billable) Section](#12-estimate-form--expenses-billable-section)
13. [Estimate Form — Consumables Section](#13-estimate-form--consumables-section)
14. [Estimate Form — MOB / DEMOB Section](#14-estimate-form--mob--demob-section)
15. [Estimate Form — Job Cost Analysis (JCA)](#15-estimate-form--job-cost-analysis-jca)
16. [Estimate Form — Grand Total Footer Bar](#16-estimate-form--grand-total-footer-bar)
17. [Estimate Statuses](#17-estimate-statuses)
18. [Overtime & Double-Time Logic](#18-overtime--double-time-logic)
19. [Revisions](#19-revisions)
20. [Field Change Orders (FCO)](#20-field-change-orders-fco)
21. [Rate Library](#21-rate-library)
22. [Cost Book](#22-cost-book)
23. [Staffing Plans](#23-staffing-plans)
24. [Staffing Plan Form](#24-staffing-plan-form)
25. [Staffing Plan Conversion](#25-staffing-plan-conversion)
26. [Crew Templates](#26-crew-templates)
27. [Financial Dashboard](#27-financial-dashboard)
28. [Revenue Forecast](#28-revenue-forecast)
29. [Manpower Forecast](#29-manpower-forecast)
30. [Calendar](#30-calendar)
31. [AI Assistant](#31-ai-assistant)
32. [Proposal PDF](#32-proposal-pdf)
33. [Dark Mode / Light Mode](#33-dark-mode--light-mode)
34. [Navigation Structure](#34-navigation-structure)
35. [Architecture Rules](#35-architecture-rules)
36. [Seed Data Requirements](#36-seed-data-requirements)
37. [Playwright Test Strategy](#37-playwright-test-strategy)
38. [Open Decisions](#38-open-decisions)

---

## 1. Business Domain

### What Stronghold Does

Stronghold sends crews of skilled tradespeople to oil refineries and chemical plants to do
maintenance, repairs, and inspections during planned shutdowns — called **turnarounds**.

Think of it like this: a refinery is like a giant machine. Once every few years, the refinery
shuts everything down so workers can go inside the pipes, vessels, and structures to check,
repair, and upgrade them. That shutdown window is the turnaround. Stronghold provides the
crews — pipefitters, welders, boilermakers, inspectors, safety workers, and supervisors — who
do that work.

### Why the Estimate Is Everything

Before a single worker shows up, Stronghold has to win the job by submitting a price to the
refinery (the customer). That price is the **estimate**. The estimate is not just a number —
it is a detailed document that says: "We will send X workers with these skills for Y days at
these billing rates, plus this equipment and these expenses, for a total of $Z."

The estimate drives:
- **What the customer pays** — every line item on the invoice comes from the estimate
- **What it costs us internally** — cost book rates applied to the same labor and equipment
- **Whether the job is profitable** — the spread between what we charge and what we pay
- **How many workers to hire or reassign** — manpower planning comes from estimate schedules
- **Cash flow forecasting** — leadership knows future revenue from awarded and pending work
- **Proposal document** — the estimate is formatted and sent to the customer for approval

### The Flow of a Typical Job

1. Estimator walks the refinery site with the customer's contact.
2. Estimator builds a **staffing plan** — a rough crew need for the future job.
3. Months (or a year+) later, the customer says the job is a go.
4. Estimator **converts** the staffing plan into a full **estimate** — adds equipment, expenses,
   mob/demob, and gets the billing rates from the customer's rate book.
5. Estimate gets submitted to the customer as a formal proposal PDF.
6. Customer awards the job or rejects it.
7. If awarded, work begins. If scope changes during the job, an **FCO** captures the extra work.
8. After the job, lost bid analytics capture why we didn't win similar bids in the future.

---

## 2. Terminology & Key Concepts

Understanding these terms is required before reading the rest of this document.

**Company / company context**
The app can serve multiple Stronghold companies (CSL, ETS, etc.). Each company's data — estimates,
rate books, cost books, staffing plans — is completely separate. A user logged into CSL cannot
see ETS data unless they have special portfolio access. The server enforces this — it is not
just a UI filter.

**Client code**
A short abbreviation for a customer. Examples: `SHELL` = Shell Oil Company, `BP` = British
Petroleum, `XOM` = ExxonMobil, `VLO` = Valero, `CVX` = Chevron. Client codes appear in estimate
numbers and are used to find the right rate book.

**Estimate**
The formal bid package for a job. Contains the customer, site, dates, all labor rows with billing
rates, equipment, expenses, consumables, mob/demob, grand total, internal cost analysis, status,
and revision history. The estimate is the source of truth for everything once a job is active.

**Staffing plan**
A future labor planning record. It looks like an estimate but only has labor — no equipment,
expenses, consumables, or mob/demob sections. Staffing plans are built months or years before
a job starts to forecast future crew needs. They convert into estimates when the job goes active.

**Rate book**
What the **customer agrees to pay** per the contract. Every customer has negotiated rates for
each craft position. Shell pays different rates than BP. A rate book says: "For this customer
at this site, a Pipefitter Journeyman is billed at $78/ST, $117/OT, $156/DT." Rate books also
include equipment billing rates, per diem, travel, and lodging. Rate books have expiration dates
— when a contract expires, the rate book is marked EXPIRED and a warning appears.

**Cost book**
What **Stronghold actually pays** internally. This is never shown to the customer. The cost book
says: "A Pipefitter Journeyman costs us $42/ST plus 40.2% in labor burden (FICA, workers comp,
insurance, benefits, G&A)." The difference between what the customer pays and what we pay is our
gross profit.

**Labor burden**
The additional cost on top of the base wage. Examples: FICA (7.65%), workers comp (8.5%),
general liability insurance (2.5%), health benefits (6%), 401k match (3%). These add up to
40.2% of the base wage in the default cost book. So a worker earning $42/hr actually costs
Stronghold $42 × 1.402 = ~$58.88/hr total.

**FCO (Field Change Order)**
Out-of-scope work added after the original bid or after the job starts. Example: the refinery
calls and says "we found more corroded pipe than expected — can your crew stay 3 extra days?"
That extra work becomes an FCO. FCOs are tracked against the original estimate and their revenue
is captured separately so it does not get lost.

**JCA (Job Cost Analysis)**
The internal profitability view on the estimate. Shows cost book rates vs. billing rates, gross
profit, and margin. This section is marked INTERNAL USE ONLY and is NEVER included in the
customer-facing proposal PDF.

**Revision**
A named snapshot of an estimate at a specific point in time. The estimator creates revisions
intentionally — for example, before sending to the customer, or after the customer asks for
scope changes. Revisions can be compared and restored.

**Crew template**
A reusable preset crew — for example, "Shutdown Crew" = 1 Project Manager + 1 General Foreman
+ 1 Foreman + 10 direct workers. Templates do NOT store rates — they store positions and headcount
only. When a template is applied, the currently active rate book supplies the rates.

**MODE toggle**
The estimate form and staffing plan form are THE SAME UI form with a MODE dropdown. Switch it
to ESTIMATE and you see all sections. Switch it to STAFFING PLAN and equipment/expenses/consumables/
MOB/DEMOB sections disappear, and the cost section renames to "Revenue & Cost Forecast."

---

## 3. Login & Company Context

When a user opens the app they see a login page. They enter their username and password.

After login:
- The user is associated with a company (e.g. CSL).
- Every piece of data they create, view, or edit is scoped to that company.
- The server enforces this on every API request — it is not just a UI-level filter.
- The user's name and company appear in the top bar.
- On successful login, the user lands on the Estimates list or Financial Dashboard.

Security rule: The app must never trust a company code sent from the browser. The server looks
up the company from the authenticated user's JWT token and uses that. A malicious user cannot
spoof another company's context.

---

## 4. Settings & Configuration

> **Legacy reference:** `docs/legacy-app/README.md` — Section 3 (Settings Page)

### What Settings Does

Settings is where an administrator configures how the app behaves for their company. Think of it
as the control panel that decides what branch offices exist, what customer codes are valid, what
the default estimate format looks like, and what the default hours/shift are.

### 4.1 Page Layout

- **Title:** Settings
- **Subtitle:** "Manage branches, company info, customer codes, and system preferences"
- **Toolbar actions:** Save All Settings | Export Settings | Import Settings
- **Dark mode toggle:** Top-right corner — a toggle switch (not a button). Persists to localStorage.

### 4.2 Branches / Locations

Branches are the company's office locations. Each estimate and staffing plan is assigned to a
branch. This allows reports to be filtered by location (e.g. "show me all Houston jobs").

Each branch has:
| Field | Example |
|---|---|
| Branch Number | 100 |
| Name | Houston Operations |
| City, State | Houston, TX |

**Default branches seeded in the app:**
- 100 — Houston Operations — Houston, TX
- 200 — Beaumont Division — Beaumont, TX
- 300 — Lake Charles — Lake Charles, LA

UI: Each branch row has Edit and Delete buttons. An **+ Add Branch** button creates a new one.

### 4.3 Company Information

| Field | Default Value |
|---|---|
| Company Name | Stronghold Companies |
| Address | 10344 New Decade Dr, Pasadena TX 77507 |
| Phone | (555) 123-4567 |
| Email | info@stronghold.com |

This information appears on the proposal PDF header.

### 4.4 Customer Codes

Customer codes are short abbreviations that identify customers. They appear in estimate numbers
and are used to look up the correct rate book. Example: when you type "Shell" as the customer,
the app knows the code is "SHELL" and looks for rate books matching that code.

**Default codes:**
| Code | Full Customer Name |
|---|---|
| BP | British Petroleum |
| SHELL | Shell Oil Company |
| OXY | Occidental Petroleum |
| XOM | ExxonMobil |
| VLO | Valero Energy |
| CVX | Chevron |

UI: Each code row has a Delete (×) button. An **+ Add Code** button creates a new code.

### 4.5 Job Number Format

The job number format is configurable. An admin can choose from named format patterns via a
dropdown. The app generates estimate numbers automatically using the selected pattern.

**Observed format in the legacy estimates database:** `YY-NNNN-CLIENTCODE`

Example: `26-0025-SHELL`
- `26` = year 2026 (2-digit)
- `0025` = sequence number, 4 digits, zero-padded, resets each year
- `SHELL` = customer code

The settings page also shows a Date-Branch-Client-Seq format (`260424-100-BP-001`) as an
alternative. Both are valid — the chosen format is whatever is selected in Settings.

**Important rules the app must enforce:**
- Estimate numbers are ALWAYS generated by the server — never by the browser.
- Two users creating estimates at the exact same moment must never receive the same number.
- This requires an atomic counter on the server, not a MAX(id)+1 query.

### 4.6 Estimate Defaults

These defaults pre-fill every new estimate and staffing plan when created. The estimator can
change them per-estimate, but these are the starting values.

| Setting | Default | Meaning |
|---|---|---|
| Hours Per Shift | 10 | Each shift is 10 hours long |
| OT After (hours) | 8 | Overtime starts after 8 hours in a day |
| Default Duration (days) | 7 | New estimates start with a 7-day duration |
| DT on Weekends | Yes — Double Time | Weekend hours are billed at 2× the base rate |
| Tax Rate (%) | 0 | No sales tax applied by default |

**What this means in practice:** A worker on a 10-hour day earns 8 hours of straight time (ST)
and 2 hours of overtime (OT). On a Saturday or Sunday under this default, all hours are double
time (DT = 2× the ST rate).

---

## 5. Estimate Numbering

### Why This Matters

Every estimate needs a unique, permanent identifier. This number appears on the proposal sent to
the customer, on invoices, and in all internal reporting. If two estimates accidentally get the
same number, records get confused and revenue gets miscounted. The system must prevent this.

### 5.1 Format

Primary format: `YY-NNNN-CLIENTCODE`

| Part | Meaning | Example |
|---|---|---|
| YY | 2-digit year | `26` for 2026 |
| NNNN | 4-digit sequence, zero-padded, resets Jan 1 each year | `0025` |
| CLIENTCODE | Customer code from Settings | `SHELL` |

Full example: `26-0025-SHELL`

### 5.2 Generation Rules

- Server-side only. The browser never generates or suggests an estimate number.
- Atomic counter: uses a database row lock or sequence to guarantee uniqueness even if two
  users click "New Estimate" at the same millisecond.
- The format is controlled by Settings → Job Number Format.
- Sequence resets to 0001 at the start of each new year (Jan 1).

### 5.3 Staff Plan Reference on Converted Estimates

When an estimate is created by converting a staffing plan, the estimate header shows a read-only
reference field:
```
Staff Plan#: SP-S-26-0004-SHELL
```
This tells everyone "this estimate originated from that staffing plan." It is for tracking and
auditing only — it does not change any behavior.

---

## 6. Staffing Plan Numbering

### Format

```
SP-S-26-NNNN-CLIENTCODE
```

Example: `SP-S-26-0004-SHELL`

| Part | Meaning |
|---|---|
| SP | Staffing Plan — identifies this as a plan, not an estimate |
| S | Stronghold division marker |
| 26 | 2-digit year |
| 0004 | 4-digit sequence, separate counter from estimate sequences |
| SHELL | Customer code |

**Rules:** Same collision-safety rules as estimate numbering. Staffing plan sequence is
independent from estimate sequence — they never share counters or overlap.

---

## 7. Estimate Form — Header

> **Legacy reference:** `docs/legacy-app/README.md` — Section 7.3–7.6

### What the Header Does

The header is the identity card of the estimate. It captures everything about WHO the job is
for, WHERE it is, WHEN it starts and ends, HOW shifts and overtime work, and the current STATUS.
Every other section of the estimate (labor rates, manpower forecast, proposal PDF) depends on
values set in the header.

### 7.1 Top Area (Above Header Fields)

The very top of the estimate form shows:

```
[COMPANY LOGO]                                         MODE: ESTIMATE ▼
                                              26-0024-SHELL
                                              April 24, 2026
                                              [Rev 2]
                          Staff Plan#: SP-S-26-0004-SHELL
[==========================] 75% complete
[🔒 This estimate is locked. Click Edit to make changes.]     [✏ Edit]
```

| Element | Behavior |
|---|---|
| MODE dropdown | ESTIMATE or STAFFING PLAN — same form, same page, just toggled |
| Estimate number | System-generated, read-only, top right corner |
| Date | Date the estimate was created (auto-populated) |
| Revision badge | "Rev 2" dark pill — shows which revision is currently active |
| Staff Plan reference | Shows source plan number when converted from a staffing plan |
| Progress bar | Horizontal fill bar showing % complete — manually set |
| Lock banner | Yellow/gold — prevents accidental edits to submitted estimates |
| Edit button | Click to unlock and enter edit mode |

### 7.2 Header Fields — Row 1

| Field | Type | Explanation |
|---|---|---|
| JOB NAME | Free text | Short description of the job, e.g. "Shell Deer Park Turnaround" |
| CLIENT | Free text / autocomplete | The customer's name, e.g. "Shell Oil Company" |
| JOB TYPE | Dropdown | Type of work: turnaround, maintenance, inspection, etc. |
| MSA # | Text | Master Service Agreement number — the contract reference. Placeholder: MSA-XXXX |
| QUOTE NUMBER | Text | Customer's quote reference number. Placeholder: Quote# |

### 7.3 Header Fields — Row 2

| Field | Type | Explanation |
|---|---|---|
| BRANCH | Dropdown | Which Stronghold office is running this job (from Settings → Branches) |
| CITY | Free text | City where the job site is located |
| STATE | Dropdown | 2-letter state abbreviation |
| COUNTY | Free text | County of the job site |
| START DATE | Date picker | First day of work |
| END DATE | Date picker | Last day of work |
| DAYS | Auto-calculated | (End Date − Start Date) + 1. Read-only. Example: May 1 → May 14 = 14 days |

### 7.4 Header Fields — Row 3

| Field | Type | Explanation |
|---|---|---|
| SHIFTS | Dropdown | Both (day + night) / Day only / Night only |
| HRS/SHIFT | Number | How many hours each shift runs. Default: 10 |
| OVERTIME RULES | Dropdown | When overtime kicks in. Options: Daily after 8 hrs / Daily 8 + Weekly 40 / Weekly 40 |
| DT W/E | Dropdown | Yes = weekend hours are double time. No = weekends are billed at normal OT or ST rates |

### 7.5 Header Fields — Row 4

| Field | Type | Explanation |
|---|---|---|
| VP | Free text | Name of the VP who owns this account |
| DIRECTOR | Free text | Name of the Director assigned |
| REGION | Free text | Region label, e.g. "Gulf" |
| SITE | Free text | The specific plant or facility name |
| QUOTE # | Text | Internal quote reference. Placeholder: Q-XXXX |
| CONFIDENCE (0-100) | Number | Estimator's gut feeling of winning this bid, 0–100%. Used in weighted pipeline calculations on the dashboard |

### 7.6 Active Rate Book Indicator

Below the header fields, the form shows which rate book is currently loaded:

```
Rates: BP Baytown, TX 2024  [Change]
```

- If no rate book is loaded: "No rate book loaded — [Load Rates]"
- If the rate book is expired: a red warning badge/icon appears next to the name
- **[Change]** opens the Rate Book Fallback Picker (see Section 21.6) — a manual selector that
  works even when the AI sidebar is offline

### 7.7 Active Cost Book Indicator

The form also shows which cost book is applied to the JCA calculations:

```
Cost Book: Default Cost Book  [Change]
```

- **[Change]** opens a cost book selector dropdown
- This allows estimators to switch from the default cost book to a regional one (e.g. Gulf Coast)
  before submitting, ensuring profitability is calculated correctly for the job's location

### 7.8 Multi-User Viewing Badge

When two or more users have the same estimate open at the same time:

```
[👤 jsmith] [👤 mcarter] [2 viewing]
```

Avatar icons and a "N viewing" count badge appear in the toolbar area (top right). This prevents
one user from unknowingly overwriting another's work.

---

## 8. Estimate Form — Toolbar & Actions

> **Legacy reference:** `docs/legacy-app/README.md` — Section 7.2

The toolbar runs across the top of the estimate form below the header. It contains every action
the estimator can take on the estimate.

```
Open | Save | Locked | Submit Bid | Upload Contract | Extract Rates | Standard Rates |
Export CSV | Export Excel | Preview | Revisions | Change Orders | Pending ▼ | Search | Settings
[🔔 N] [avatar1] [avatar2] [N viewing] [AD]
```

| Button | What It Does |
|---|---|
| **Open** | Opens a different estimate from the list |
| **Save** | Saves all current changes. Keyboard shortcut: Ctrl+S |
| **Locked** | Shows when the estimate is locked. Click to unlock (same as the Edit button) |
| **Submit Bid** | Marks the estimate as Submitted, meaning it has been sent to the customer for approval. Triggers a confirmation dialog before changing status |
| **Upload Contract** | Lets the user attach a signed contract document (PDF or DOCX) to the estimate |
| **Extract Rates** | Parses an uploaded contract document and pulls out rate data automatically |
| **Standard Rates** | Loads the Standard (Baseline) rate book — useful as a starting point before client rates are available |
| **Export CSV** | Downloads the estimate data as a .csv file |
| **Export Excel** | Downloads the estimate data as an Excel workbook |
| **Preview** | Opens a PDF preview of the customer-facing proposal — shows exactly what the customer will see (no JCA) |
| **Revisions** | Opens the revision history drawer on the right side |
| **Change Orders** | Opens the FCO (Field Change Order) panel |
| **Pending ▼** | Dropdown for approval workflow actions |
| **Search** | Filter/search within the current estimate |
| **Settings** | Estimate-specific settings |
| **🔔 N** | Notification bell with unread notification count |

### 8.1 Keyboard Shortcuts

| Key Combo | Action |
|---|---|
| Ctrl+S | Save the estimate immediately |
| Ctrl+Z | Undo the last change |
| ? | Open a keyboard shortcut reference popup |

---

## 9. Estimate Form — Lock & Edit Behavior

### Why Locking Exists

Once an estimate has been submitted to a customer or awarded, it should not be accidentally
changed. If an estimator accidentally edits a submitted bid, the numbers sent to the customer
no longer match what's in the system. Locking prevents this.

### 9.1 Locked State

- A yellow/gold banner appears at the top of the form:
  `"This estimate is locked. Click Edit to make changes."`
- All form fields are read-only while locked.
- An **Edit** button appears in the top right.
- The toolbar shows the word "Locked" as a button.

### 9.2 Edit (Unlocked) Mode

- Clicking **Edit** or clicking **Locked** in the toolbar unlocks the form.
- The yellow banner disappears.
- All fields become editable.
- A normal **Save** button replaces the **Locked** button.

### 9.3 When Estimates Auto-Lock

- After the estimator clicks **Submit Bid** — the estimate locks to preserve the submitted version.
- Awarded estimates are locked by default — changes after award go through FCOs, not direct edits.
- Lost estimates are locked and read-only permanently.

---

## 10. Estimate Form — Labor Section

> **Legacy reference:** `docs/legacy-app/README.md` — Section 7.7

### What the Labor Section Is

This is where the estimator builds the crew. Each row represents one type of worker (position).
The estimator enters how many of that worker are needed on each day of the job. The app
automatically calculates how many hours are straight time, overtime, and double time, then
multiplies by the billing rates from the active rate book to get the total billable amount.

### 10.1 Section Header

```
▼ LABOR       [All] [Wk1] [Wk2] [Wk3]              [🧑 Load Crew] [+ Add Employee]
```

- **[All] [Wk1] [Wk2] [Wk3]** — week filter buttons. Clicking Wk2 shows only Week 2's daily
  columns in the date grid, making long jobs easier to read.
- **[🧑 Load Crew]** — opens the crew template picker modal.
- **[+ Add Employee]** — adds a new blank labor row.

### 10.2 Column Headers

```
EMPLOYEE | HRS | ST | OT | DT | SHFT | [daily date grid] | ST hrs | OT hrs | DT hrs | ST $ | OT $ | DT $ | TOTAL | [×]
```

| Column | Color | Meaning |
|---|---|---|
| EMPLOYEE | — | The position name, e.g. "Pipefitter Journeyman" |
| HRS | — | Hours per day for this position (usually the hours/shift from the header) |
| ST | Green | Straight-time billing rate from the active rate book |
| OT | Orange | Overtime billing rate (typically ST × 1.5) |
| DT | Red | Double-time billing rate (typically ST × 2.0) |
| SHFT | — | Which shift: D = Day, N = Night |
| [daily date grid] | — | One column per calendar day — the number entered is the headcount |
| ST hrs | — | Total straight-time hours across all days for this row |
| OT hrs | — | Total overtime hours across all days |
| DT hrs | — | Total double-time hours across all days |
| ST $ | — | ST hours × ST rate |
| OT $ | — | OT hours × OT rate |
| DT $ | — | DT hours × DT rate |
| TOTAL | — | ST$ + OT$ + DT$ for this row |
| [×] | — | Delete this row |

### 10.3 The Daily Headcount Grid (Most Important Part of Labor)

Each labor row has one column for every calendar day of the job. The number entered in each cell
is how many workers of that type are on-site that day.

**Example — a 14-day job:**
```
Pipefitter Journeyman | 10 | $78 | $117 | $156 | D | 0 0 2 2 2 2 2 2 2 2 2 2 1 1 | ...
                                                     Mon Tue Wed Thu Fri Sat Sun Mon...
```
This means: 0 pipefitters Monday/Tuesday (mobilizing), 2 pipefitters Wednesday through the
following Sunday, then 1 pipefitter for the last two days (wrapping up).

This **ramp-up/ramp-down** capability is critical for turnaround work. Crew sizes change as
different phases of work start and finish. The daily grid captures this exactly.

Weekend columns are highlighted in yellow to remind the estimator that those days may trigger
double-time rates.

### 10.4 Row Type Badge

Each labor row shows a colored badge:
- `DIR` (green) — Direct labor. This craft is directly doing the hands-on work.
- `IND` (yellow) — Indirect labor. This position supervises or supports (PM, foreman, safety watch).

### 10.5 Rates Auto-Fill Behavior

When the estimator selects a position name from the dropdown:
1. The app looks up that position in the currently active rate book.
2. The ST, OT, and DT billing rates fill in automatically.
3. If the position is NOT in the rate book, the app shows a warning and leaves the rate fields
   empty with a flag icon — it never silently uses a wrong rate.

### 10.6 Labor Subtotal Row

At the bottom of the labor section:
```
Labor Subtotal (D: $62,100 | I: $80,040)   728  364  168  $64,272  $48,204  $29,664  $142,140.00
```

- `D: $62,100` = total direct labor billing
- `I: $80,040` = total indirect labor billing
- Then total ST/OT/DT hours followed by ST/OT/DT dollar totals
- The rightmost number ($142,140.00) is the full labor subtotal

### 10.7 Turnaround Craft Positions

**DIRECT LABOR — what we charge the customer for hands-on work:**
Pipefitter Journeyman, Pipefitter Helper, Boilermaker Journeyman, Boilermaker Helper,
Welder Journeyman, Welder Helper, Millwright Journeyman, Electrician Journeyman,
Instrument Tech, Crane Operator, Rigger, Scaffold Builder, NDT Technician, Driver/Teamster

**INDIRECT LABOR — supervision and safety support:**
Project Manager, General Foreman, Foreman, Safety Watch, Fire Watch, Hole Watch

---

## 11. Estimate Form — Equipment Section

> **Legacy reference:** `docs/legacy-app/README.md` — Section 7.8

### What the Equipment Section Is

This captures equipment that Stronghold rents or provides and bills to the customer. Examples:
cranes, manlifts, welding machines, generators. The customer pays for this equipment at the
rates in their rate book.

### 11.1 Section Header

```
▼ EQUIPMENT       [All] [Wk1] [Wk2] [Wk3]                       [+ Add Equipment]
```

### 11.2 Column Headers

```
EQUIPMENT | QTY | RATE | TYPE | [daily date grid] | DAYS | TOTAL
```

| Column | Meaning |
|---|---|
| EQUIPMENT | Name of the piece of equipment, e.g. "Crane - 50 Ton" |
| QTY | How many units |
| RATE | Billing rate — auto-filled from the rate book based on TYPE |
| TYPE | How the customer is billed: Hourly / Daily / Weekly / Monthly |
| [daily date grid] | Same calendar grid as labor — marks which days equipment is on-site |
| DAYS | Calculated total billing periods (days, weeks, or months depending on TYPE) |
| TOTAL | QTY × RATE × DAYS |

### 11.3 Billing Type Behavior

When the estimator selects TYPE:
- **Hourly** → the hourly rate from the rate book fills in
- **Daily** → the daily rate fills in
- **Weekly** → the weekly rate fills in
- **Monthly** → the monthly rate fills in

Changing the TYPE automatically changes the RATE. The estimator can override the rate manually
if needed.

### 11.4 Rate Book Equipment Rates (Standard Baseline)

| Equipment | Daily | Weekly | Monthly |
|---|---|---|---|
| Forklift 5K | $185 | $750 | $2,400 |
| Crane - 50 Ton | $1,200 | $5,000 | $16,000 |
| Crane - 100 Ton | $1,800 | $7,500 | $24,000 |
| Manlift 40ft | $220 | $900 | $2,800 |
| Welding Machine 400amp | $95 | $380 | $1,200 |

---

## 12. Estimate Form — Expenses (Billable) Section

> **Legacy reference:** `docs/legacy-app/README.md` — Section 7.9

### What the Expenses Section Is

These are costs that pass directly to the customer — things like per diem (daily meal/lodging
allowance), travel, and rental cars. They are billed at cost or a fixed markup. The section header
shows "Charged to Client" to remind the estimator these expenses appear on the customer's invoice.

### 12.1 Section Header

```
▼ EXPENSES (BILLABLE)                                     Charged to Client →
```

The "Charged to Client" tag (green, right side) confirms these are pass-through expenses.

### 12.2 Column Headers

```
DESCRIPTION | BILL RATE | DAYS/QTY | PEOPLE | BILLABLE
```

| Column | Meaning |
|---|---|
| DESCRIPTION | What the expense is (editable text) |
| BILL RATE | The per-day or per-unit rate charged to the customer |
| DAYS/QTY | Number of days (for per diem) or quantity (for fixed costs) |
| PEOPLE | How many workers this expense applies to (e.g. 6 workers on per diem) |
| BILLABLE | Calculated: BILL RATE × DAYS/QTY × PEOPLE |

**Example:** Standard Per Diem, $65/day, 14 days, 6 people = $65 × 14 × 6 = $5,460 billable.

### 12.3 Empty State Message

When no expenses have been added:
```
No expenses added — use buttons below for out-of-town jobs
```

### 12.4 Button: [+ Auto Per Diem]

This blue button opens a dropdown with the most common per diem options. Click one to instantly
add a row pre-filled with the description and rate:

| Option | Rate | When Used |
|---|---|---|
| Standard Per Diem | $65.00/day | Standard local job per diem |
| Per Diem - High Cost Area | $85.00/day | Jobs in expensive cities |
| Meals Only | $45.00/day | Just meals, no lodging allowance |
| Direct | $100.00/day | For direct labor workers out of town |
| Indirect | $120.00/day | For indirect/supervisory workers out of town |

After clicking, the row is added. The estimator fills in DAYS/QTY and PEOPLE; BILLABLE calculates.

### 12.5 Button: [+ Expense]

This green button opens a full dropdown with grouped expense presets:

**PER DIEM group:**
- Standard Per Diem — $65/day
- Per Diem - High Cost Area — $85/day
- Meals Only — $45/day
- Direct — $100/day
- Indirect — $120/day

**TRAVEL group:**
- Mileage Reimbursement — $0.67/mile
- Airfare - Domestic — $500/trip
- Rental Car — $75/day
- Fuel Allowance — $50/day
- (additional travel items below — list scrolls)

---

## 13. Estimate Form — Consumables Section

> **Legacy reference:** `docs/legacy-app/README.md` — Section 7.10

### What Consumables Are

Consumables are physical supplies used on the job that are billed to the customer — things like
welding rod, PPE, safety supplies, grinding wheels, or small tools. This is distinct from
per diem/travel (Section 12) and from MOB/DEMOB (Section 14).

### 13.1 Section Header

```
▼ CONSUMABLES                                          Materials & Supplies →
```

The "Materials & Supplies" tag (orange, right side).

### 13.2 Column Headers

```
DESCRIPTION | UNIT | UNIT COST | QTY | SUBTOTAL
```

| Column | Meaning |
|---|---|
| DESCRIPTION | Name of the consumable item (e.g. "Welding Rod E7018") |
| UNIT | Unit of measure (e.g. per lb, per box, per unit) |
| UNIT COST | Cost per unit |
| QTY | How many units |
| SUBTOTAL | UNIT COST × QTY |

### 13.3 Empty State Message

```
No consumables added — use buttons below for materials & supplies
```

### 13.4 Action Buttons

- **[+ Add Consumable] (orange)** — adds a single blank row for a custom item
- **[+ Common Items] (orange)** — opens a preset picker with frequently used consumable items
  (safety supplies, PPE, small tools, etc.)

---

## 14. Estimate Form — MOB / DEMOB Section

> **Legacy reference:** `docs/legacy-app/README.md` — Section 7.11

### What MOB/DEMOB Is

Mobilization (MOB) is the cost to get the crew and equipment TO the job site. Demobilization
(DEMOB) is the cost to return them. This includes truck rentals, trailer rentals, fuel for
the round trip, tool shipments, and rig-up/rig-down labor. These are fixed costs that the
customer pays as a lump sum, separate from labor and equipment billing.

### 14.1 Section Header

```
▼ MOB / DEMOB                                  Mobilization & Demobilization →
```

The "Mobilization & Demobilization" tag (purple, right side).

### 14.2 Column Headers

```
DESCRIPTION | COST | QTY | SUBTOTAL
```

The DESCRIPTION column has placeholder text: "e.g., Truck Rental - 1 Ton"

### 14.3 Action Buttons

- **[+ Add Item] (purple)** — adds a blank MOB/DEMOB row
- **[+ Common Items] (dark/purple)** — opens a preset list of common items

### 14.4 Common MOB/DEMOB Preset Items

These are the most frequently used mob/demob costs. Selecting a preset adds a row pre-filled
with the description and typical cost:

| Description | Typical Cost | Qty | Total |
|---|---|---|---|
| Truck Rental - 1 Ton | $250 | 1 | $250.00 |
| Trailer Rental | $150 | 1 | $150.00 |
| Fuel (round trip) | $500 | 1 | $500.00 |
| Tool Shipment | $800 | 1 | $800.00 |
| Rig-Up Labor (crew) | $2,500 | 1 | $2,500.00 |
| Rig-Down Labor (crew) | $2,500 | 1 | $2,500.00 |
| **SUBTOTAL** | | | **$6,700.00** |

The estimator can edit any cost, quantity, or description after adding a preset.

---

## 15. Estimate Form — Job Cost Analysis (JCA)

> **Legacy reference:** `docs/legacy-app/README.md` — Section 7.12

### What the JCA Is

The JCA is the internal profitability view. It answers: "After paying our workers and covering
our overhead, how much money do we actually make on this job?"

It uses COST BOOK rates (what Stronghold pays) — NOT the billing rates the customer sees.
**The JCA is NEVER shown to the customer. It is NEVER included in the proposal PDF.**

### 15.1 Section Header

```
▼ JOB COST ANALYSIS                                      INTERNAL USE ONLY →
```

The "INTERNAL USE ONLY" badge (red, right side) is a hard requirement and must be visually
prominent. No proposal export, preview, or PDF generation may include this section.

### 15.2 Labor Burden Header Line

At the top of the JCA, a live-updating line shows the current burden configuration:

```
Labor Burden: 11.80% + $7.65/hr
```

This reflects the current cost book's burden setup in real time. It updates immediately when
burden rates are changed in the cost book (no page refresh needed).

### 15.3 Labor Cost Breakdown Subsection

This is a mirror of the Labor section (Section 10) but showing INTERNAL costs instead of billing
rates. The estimator can see exactly what each position costs per day.

**Column headers:**
```
POSITION | QTY | ST | OT | DT | SHFT | [daily grid] | ST hrs | OT hrs | DT hrs | ST$ | OT$ | DT$ | BURDEN | TOTAL
```

| Column | Color | Meaning |
|---|---|---|
| ST / OT / DT | Purple | Cost book rates — what Stronghold PAYS the worker |
| BURDEN | — | The burden dollars for this row (FICA + insurance + benefits etc.) |
| TOTAL | — | (ST$ + OT$ + DT$) + BURDEN — the true all-in cost for this row |

**Example cost calculations (from legacy estimate 26-0001-SHL):**

| Position | QTY | ST Cost | ST hrs | OT hrs | DT hrs | Burden | Total |
|---|---|---|---|---|---|---|---|
| Project Manager (Day) | 1 | $65 | 104 | 52 | 24 | $1,764 | $16,714 |
| Pipefitter Journeyman (Day) | 1 | $42 | 104 | 52 | 24 | $1,140 | $10,800 |
| Welder Journeyman (Night) | 1 | $46 | 104 | 52 | 24 | $1,248 | $11,828 |

**Cost Subtotal row:** Shows total hours, total ST/OT/DT cost dollars, total burden, and grand
cost total across all positions.

### 15.4 Expense Costs Subsection

Lists all billable expenses with their INTERNAL costs (from the cost book), showing what it
actually costs Stronghold to provide per diem, travel, and lodging — not what the customer pays.

### 15.5 Summary KPI Cards (4 boxes)

These four boxes summarize the profitability of the entire estimate:

| Card | Color | What It Shows | Example |
|---|---|---|---|
| BILLED TO CLIENT | Blue | Grand total the customer will pay | $148,840.00 |
| OUR ACTUAL COST | Purple | Total internal cost (labor + burden + expenses) | $84,341.92 |
| GROSS PROFIT | Green | Billed to Client minus Our Actual Cost | $64,498.08 |
| PROFIT MARGIN | Yellow/Orange | (Gross Profit ÷ Billed to Client) × 100 | 43.3% |

The PROFIT MARGIN is displayed as an animated semi-circle gauge chart (like a speedometer).
The needle and the percentage value change color from red → orange → green as margin improves.

### 15.6 Live Recalculation Rule

**This is a critical behavior.** When an administrator edits and saves burden rates in the cost
book (e.g. changes workers comp from 8.5% to 9.0%), the JCA on EVERY estimate that uses that
cost book must immediately recalculate — costs, gross profit, and profit margin all update
without the estimator having to reopen or refresh the estimate.

This means: if 50 estimates use the Default Cost Book, and the admin changes one burden line,
all 50 estimates' JCA sections reflect the new number immediately.

### 15.7 Profit Source Note

The primary driver of gross profit is labor. Expenses, consumables, and MOB/DEMOB are billed
to the customer at or near cost (pass-through). The spread between the billing rate and the
cost book rate on labor hours is where most of the profit comes from.

---

## 16. Estimate Form — Grand Total Footer Bar

> **Legacy reference:** `docs/legacy-app/README.md` — Section 7.13

### What It Is

A sticky bar pinned to the bottom of the estimate form. It is always visible regardless of how
far the estimator has scrolled. It shows the running total of every section plus the final
amount that will be billed to the customer.

### 16.1 Visual Layout

```
LABOR        EQUIPMENT    EXPENSES     CONSUMABLES  MOB/DEMOB    DISCOUNT  DISCOUNT AMT   TAX RATE  TAX       GRAND TOTAL
$142,140.00  $0.00        $0.00        $0.00        $6,700.00    % ▼       -$0.00         0%        $0.00     $148,840.00
```

The GRAND TOTAL is highlighted in teal/blue and is the largest number.

### 16.2 Calculation Order (Step by Step)

```
Step 1: Labor subtotal               = $142,140.00
Step 2: + Equipment subtotal         = +$0.00
Step 3: + Expenses (billable)        = +$0.00
Step 4: + Consumables                = +$0.00
Step 5: + MOB/DEMOB                  = +$6,700.00
         ─────────────────────────
         Billing Subtotal            = $148,840.00

Step 6: − Discount (% or $)         = −$0.00
         Discounted Subtotal         = $148,840.00

Step 7: × Tax Rate                   = × 0%
         Tax Amount                  = $0.00
         ─────────────────────────
Step 8:  GRAND TOTAL                 = $148,840.00
```

### 16.3 Field Behaviors

| Field | Behavior |
|---|---|
| LABOR | Read-only. Auto-updates when any labor row changes. |
| EQUIPMENT | Read-only. Auto-updates when any equipment row changes. |
| EXPENSES | Read-only. Auto-updates when any expense row changes. |
| CONSUMABLES | Read-only. Auto-updates when any consumable row changes. |
| MOB/DEMOB | Read-only. Auto-updates when any mob/demob row changes. |
| DISCOUNT | User enters a value. A toggle (% ▼) switches between percentage discount and flat dollar discount. |
| DISCOUNT AMT | Read-only. Calculated: if % mode, Billing Subtotal × %. If $ mode, just the entered value. |
| TAX RATE | User-editable percentage. Default: 0%. |
| TAX | Read-only. Discounted Subtotal × Tax Rate. |
| GRAND TOTAL | Read-only. Discounted Subtotal + Tax. Highlighted in teal/blue. |

### 16.4 On Staffing Plans

The footer bar on staffing plans uses the label **ESTIMATED COST** (teal) instead of GRAND TOTAL.
The EQUIPMENT, EXPENSES, CONSUMABLES, and MOB/DEMOB columns show $0 because staffing plans do
not have those sections.

---

## 17. Estimate Statuses

An estimate moves through different statuses as it progresses from idea to job completion.
The status controls what dashboards count it toward and what actions are available.

| Status | Pill Color | Meaning | What It Counts Toward |
|---|---|---|---|
| **Draft** | Light gray | Being built. Not yet submitted. | Nothing — does not appear in pipeline |
| **Pending** | Yellow/orange | Submitted and waiting for customer response | Pending Pipeline on dashboard |
| **Submitted** | Gray | Sent to customer (may overlap with Pending in some views) | Pending Pipeline |
| **Awarded** | Green | Customer said yes. Job is happening. | Awarded Revenue, Manpower Forecast |
| **Lost** | Red/pink | Customer said no. We did not win the job. | Lost Revenue analytics |
| **Archived** | — | No longer active (vocabulary TBD) | Removed from active pipeline |

### 17.1 Lost Status — Required Fields

When an estimate is marked Lost, the app must:
1. Prompt the user to enter a **Lost Reason** (dropdown) before allowing the save.
2. Optionally allow a **Lost Notes** text field for additional detail.
3. Lock the estimate — Lost estimates are read-only.

**Lost Reason values (observed):**
- Not specified
- Went with competitor
- (additional values should be configurable in Settings)

Lost reasons feed the Lost Bid Analysis donut chart on the Financial Dashboard.

### 17.2 Status Transition Rules

- **Submitted** → locks the estimate
- **Awarded** → locks the estimate; enables FCO management
- **Lost** → locks the estimate; requires a lost reason
- An admin can unlock any estimate if needed (override)

---

## 18. Overtime & Double-Time Logic

> This logic is the most critical calculation in the app. It must be centralized in one place.
> The same logic must be used by: labor billing calculations, JCA cost calculations, staffing
> plan revenue/cost forecasts, the AI assistant when answering hour questions, and all exports.
> If this logic is duplicated in multiple components and they disagree, reports will be wrong.

### What OT/DT Logic Does

When workers are on a turnaround job, they typically work long days (10–12 hours) and sometimes
7 days a week. Contracts specify when overtime pay kicks in and when double-time kicks in. The
app must calculate the right mix of ST/OT/DT hours for every worker on every day.

### 18.1 Overtime Methods

**Daily (after 8 hrs):**
- Any hours in a single day beyond 8 are overtime.
- Example: 10-hour day = 8 ST + 2 OT.
- Example: 12-hour day = 8 ST + 4 OT.

**Daily 8 + Weekly 40:**
- OT after 8 hrs in a day, AND OT after 40 hrs in a week — whichever triggers first.
- Example: Worker does 10 hrs/day × 5 days = 50 total hours in a week.
  - Each day: 8 ST + 2 OT = 40 ST + 10 OT by end of day 5. (Daily threshold hit each day.)
  - The weekly threshold (40) does not add additional OT on top because the daily threshold
    already produced OT hours. Implementation must handle the interaction correctly.

**Weekly 40:**
- No daily threshold. OT only after the worker exceeds 40 hours in a week.
- Example: Worker does 8 hrs/day × 5 days = 40 ST, no OT.
- Example: Worker does 8 hrs/day × 6 days = 40 ST + 8 OT (day 6 kicks in OT).

### 18.2 Double-Time Rule

- **DT W/E = Yes:** Workers on Saturday and/or Sunday are billed at DT rate (2× the ST rate).
- **DT W/E = No:** Weekend hours follow the same OT rules as weekdays.
- Weekend columns in the daily grid are highlighted yellow to remind the estimator.

### 18.3 Default Estimate Settings

Under the app defaults (from Settings → Estimate Defaults):
- HRS/SHIFT = 10
- OT after = 8 hrs (daily method)
- DT on weekends = Yes

This means: every 10-hour day = **8 ST hrs + 2 OT hrs**. Every Saturday/Sunday = **all DT**.

### 18.4 Calculation Flow (Step by Step)

For each labor row on each calendar day:
1. Read the headcount from the daily grid cell (e.g. 2 workers).
2. Read the hours/shift for this row (e.g. 10 hrs).
3. Apply the OT method: 10-hr day under daily-8 rule = 8 ST hrs + 2 OT hrs per worker.
4. Check if the day is a weekend. If DT W/E = Yes, override: all 10 hrs become DT hrs.
5. Multiply hours by headcount: 2 workers × 8 ST hrs = 16 ST hrs total for this cell.
6. Repeat for every day in the range.
7. Sum all days → total ST hours, OT hours, DT hours for this labor row.
8. Multiply totals by billing rates → billing dollars (for the Labor section).
9. Multiply totals by cost book rates + burden → internal cost dollars (for the JCA section).

---

## 19. Revisions

> **Legacy reference:** `docs/legacy-app/README.md` — Section 10

### What Revisions Are

A revision is a saved snapshot of the entire estimate at a specific moment. Think of it like
hitting "Save As" in a word processor — you get a numbered copy you can go back to later.

Revisions are NOT created automatically on every save. The estimator creates them on purpose —
for example, before sending an estimate to the customer, or after the customer requests changes.

**Why revisions matter:** Customers frequently ask for changes. "Add two more welders." "Remove
the crane." By the time the estimate has gone back and forth several times, there might be 5
different versions. The estimator needs to be able to compare them ("what changed between Rev 3
and Rev 5?") and restore a previous version ("the customer liked Rev 4 better, let's go back
to that").

### 19.1 Revision History Drawer

Opened via the **Revisions** toolbar button. Slides in from the right side of the screen.

**Drawer header:**
```
📋 Revision History                                            [×]
[+ Create Revision]   [🔀 Compare]
```

### 19.2 Revision Card Layout

Each revision in the drawer shows:

```
Rev 2                                                        [CURRENT]
rev 2
📅 Mar 25, 2026, 07:27 PM   👥 7 labor   ⚙ 0 equip
Total: $142,140  L: $142,140  E: $0
                       [👁 View]  [↺ Restore]  [🗑 Delete]
```

- **CURRENT badge** — only the active revision gets this badge
- **Date/time** — when this revision was created
- **Labor count and equipment count** — how many rows were in this revision
- **Total / L: / E:** — grand total, labor subtotal, equipment subtotal
- **View** — open a read-only view of this revision
- **Restore** — create a new revision that is a copy of this one (makes it current)
- **Delete** — permanently remove this revision

### 19.3 Auto-Generated Revision Notes

When an estimate is created from a staffing plan conversion:
```
"Converted from plan SP-S-26-0004-SHELL"
```
This auto-note is applied to the first revision (Rev 1). Rev 1 typically starts at $0 because
labor rates have not been added yet — the estimator builds out the estimate in subsequent revisions.

### 19.4 What a Revision Captures (Complete Snapshot)

- Full estimate header (all fields from Section 7)
- All labor rows: positions, rates, daily headcount grid, calculated hours, dollar totals
- All equipment rows: items, rate types, quantities, billing amounts
- All expense rows
- All consumable rows
- All MOB/DEMOB rows
- Grand total
- JCA summary (billed total, cost total, gross profit, margin %)
- The user who created the revision
- Date and time of creation
- Revision note (auto-generated or manually entered)

### 19.5 Compare Two Revisions

**[🔀 Compare]** button in the drawer header.

The user selects two revisions and the app shows a side-by-side comparison table showing:
- Grand total difference
- Labor row count change
- Equipment row count change
- Gross profit difference
- Margin % difference

Future enhancement: row-level diffs (exactly which positions were added, removed, or changed).

### 19.6 Restore a Revision

When the user clicks **Restore** on a revision:
1. A NEW revision is created that is a copy of the selected one.
2. This new revision becomes CURRENT.
3. The original revision history is preserved — nothing is deleted.
4. The restore action itself is auditable (new revision with a note like "Restored from Rev 4").

---

## 20. Field Change Orders (FCO)

### Why FCOs Exist

Once a job is awarded and work begins, things change. The refinery finds more corroded pipe than
expected. They want Stronghold to add 3 more welders for 5 extra days. This extra work was not
in the original estimate.

If Stronghold just does the work without capturing it, the extra revenue is lost. The original
estimate total stays at $174,636 but Stronghold did $195,000 worth of work. That $20,364 just
disappears.

FCOs prevent this. When out-of-scope work is requested, the estimator opens an FCO, documents
the extra work, gets customer approval, and captures the extra revenue formally.

### 20.1 Where FCOs Live

- **Change Orders** button in the estimate toolbar opens the FCO panel.
- FCOs are only enabled when the estimate status is **Awarded**.
- A collapsible FCO section appears on awarded estimates.

### 20.2 FCO Fields

| Field | Type | Explanation |
|---|---|---|
| FCO Number | Auto-generated | Format: FCO-YY-NNNN-001, tied to the parent estimate |
| Parent Estimate | Read-only | The estimate this FCO is attached to |
| Description | Text area | What changed — the added scope description |
| Requested By | Text | Name of the customer's contact who requested the change |
| Date Requested | Date picker | When the customer asked for this change |
| Labor Impact | Embedded labor rows | Optional — add extra labor rows in the same format as the estimate labor section |
| Equipment Impact | Embedded equipment rows | Optional — extra equipment needed |
| Expense Impact | Embedded expense rows | Optional — extra expenses |
| Price Impact | Auto-calculated | Sum of all extra labor + equipment + expense from this FCO |
| Approval Status | Dropdown | Pending / Approved / Rejected |
| Customer Approval Date | Date picker | When the customer signed off |
| Notes | Text area | Any additional context |

### 20.3 FCO Behavior Rules

- An approved FCO adds its Price Impact to the estimate's total (on top of the original bid).
- A pending FCO is visible but does NOT count toward awarded revenue until approved.
- Each estimate can have multiple FCOs (FCO-001, FCO-002, FCO-003...).
- FCO revenue appears as a separate line in financial reporting.
- One seeded example FCO must be attached to an awarded estimate for the demo.

---

## 21. Rate Library

> **Legacy reference:** `docs/legacy-app/README.md` — Section 4

### What the Rate Library Is

The rate library is the collection of all customer contracts — specifically, what each customer
agreed to pay for each type of labor, equipment, per diem, travel, and lodging. Every customer
(Shell, BP, ExxonMobil, Valero, Chevron) has negotiated different rates. A Shell job uses Shell
rates. A BP job uses BP rates.

Rate books have expiration dates — when a contract expires, the rate book is marked EXPIRED and
warnings appear on any estimates using it.

### 21.1 Page Layout

- **Title:** Rate Library
- **Subtitle:** "Client rate sheets & pricing"
- **Left sidebar:** All rate sets grouped by client, with ACTIVE/EXPIRED badges
- **Main area:** Tabs for the selected rate set: Labor | Equipment | Per Diem | Travel | Lodging | Templates
- **Toolbar:** Rate Set | Duplicate | Delete | Search | Save | Versions | **Sync to API**

### 21.2 Rate Set Card (Sidebar)

Each rate set in the sidebar shows:
- Rate set name (e.g. "BP Baytown, TX 2024")
- Status badge: **ACTIVE** (green) or **EXPIRED** (red)
- Effective date and Expiration date
- Labor count, Equipment count, etc.

**All rate sets loaded in the legacy app:**

| Client | Rate Set Name | Status |
|---|---|---|
| BP | BP - El Reno, OK | EXPIRED (Exp 2026-02-27) |
| BP | BP - Houston TX | EXPIRED (Exp 2026-02-27) |
| BP | BP Baytown, TX 2024 | ACTIVE (Exp 2026-12-31) |
| BP | BP Texas City, TX 2024 | ACTIVE (Exp 2026-12-31) |
| BP | BP Whiting, IN 2024 | ACTIVE (Exp 2026-12-31) |
| BP Refinery | BP Refinery - Bakersfield, CA | — |
| Chevron | Chevron El Segundo, CA 2024 | ACTIVE (Exp 2026-12-31) |
| Chevron | Chevron Pascagoula, MS 2024 | ACTIVE (Exp 2026-12-31) |
| Chevron | Chevron Richmond, CA 2024 | ACTIVE (Exp 2026-12-31) |
| ExxonMobil | ExxonMobil - Houston | — |
| ExxonMobil | ExxonMobil Baton Rouge, LA 2024 | ACTIVE |
| ExxonMobil | ExxonMobil Baytown, TX 2024 | ACTIVE |
| ExxonMobil | ExxonMobil Beaumont, TX 2024 | ACTIVE |

### 21.3 Standard (Baseline) Rate Set — Labor

The Standard rate set is the default loaded before a client-specific rate book is selected.
All estimates start with these rates until a client rate book is loaded.

**DIRECT LABOR (13 positions):**

| Position | ST Rate | OT Rate (1.5×) | DT Rate (2×) |
|---|---|---|---|
| Pipefitter Journeyman | $78.00 | $117.00 | $156.00 |
| Pipefitter Helper | $52.00 | $78.00 | $104.00 |
| Boilermaker Journeyman | $82.00 | $123.00 | $164.00 |
| Boilermaker Helper | $54.00 | $81.00 | $108.00 |
| Welder Journeyman | $85.00 | $127.50 | $170.00 |
| Welder Helper | $55.00 | $82.50 | $110.00 |
| Millwright Journeyman | $80.00 | $120.00 | $160.00 |
| Electrician Journeyman | $82.00 | $123.00 | $164.00 |
| Instrument Tech | $88.00 | $132.00 | $176.00 |
| Crane Operator | $95.00 | $142.50 | $190.00 |
| Rigger | $72.00 | $108.00 | $144.00 |
| Scaffold Builder | $65.00 | $97.50 | $130.00 |
| NDT Technician | $95.00 | $142.50 | $190.00 |

**INDIRECT LABOR (7 positions):**

| Position | ST Rate | OT Rate | DT Rate |
|---|---|---|---|
| Project Manager | $125.00 | $187.50 | $250.00 |
| General Foreman | $98.00 | $147.00 | $196.00 |
| Foreman | $85.00 | $127.50 | $170.00 |
| Safety Watch | $48.00 | $72.00 | $96.00 |
| Fire Watch | $45.00 | $67.50 | $90.00 |
| Hole Watch | $45.00 | $67.50 | $90.00 |
| Driver/Teamster | $58.00 | $87.00 | $116.00 |

Note: OT = ST × 1.5, DT = ST × 2.0 (standard multipliers; client-specific rates may differ
from this pattern as confirmed by the Chevron Richmond example).

### 21.4 Equipment Tab — Billing Rates

Columns: EQUIPMENT | HOURLY | DAILY | WEEKLY | MONTHLY

| Equipment | Daily | Weekly | Monthly |
|---|---|---|---|
| Forklift 5K | $185.00 | $750.00 | $2,400.00 |
| Forklift 10K | $250.00 | $1,000.00 | $3,200.00 |
| Crane - 50 Ton | $1,200.00 | $5,000.00 | $16,000.00 |
| Crane - 100 Ton | $1,800.00 | $7,500.00 | $24,000.00 |
| Manlift 40ft | $220.00 | $900.00 | $2,800.00 |
| Manlift 60ft | $280.00 | $1,150.00 | $3,600.00 |
| Scissor Lift | $150.00 | $600.00 | $1,900.00 |
| Welding Machine 400amp | $95.00 | $380.00 | $1,200.00 |
| Air Compressor 185cfm | $120.00 | $480.00 | $1,500.00 |
| Light Tower | $75.00 | $300.00 | $950.00 |
| Generator 25KW | $110.00 | $440.00 | $1,400.00 |

### 21.5 Per Diem Tab

Per diem rates are client-specific. Example (Chevron El Segundo):
- Standard Per Diem: $85.00/day
- High Cost Area: $115.00/day

### 21.6 Rate Book Fallback Picker (Manual Selector)

**This is a required feature.** The AI sidebar RATES tab is the primary way to load a rate book,
but the AI might be offline or the API key might be blank. In that case, the estimator must still
be able to load a rate book without the AI.

**Where it lives:** An **[Change]** link next to the Active Rate Book indicator in the estimate
header (Section 7.6).

**What it does:**
1. Clicking [Change] opens a modal or dropdown list.
2. The list shows ALL available rate books, grouped by client name, with ACTIVE/EXPIRED badges
   and the labor/equipment counts — exactly the same list as the AI RATES tab.
3. Clicking any rate book in the list immediately loads it into the current estimate.
4. All labor rows update with the new rates from the selected rate book.
5. This works completely independently of the AI assistant.

### 21.7 Rate Book Auto-Suggest

When the CLIENT field on the estimate header is changed:
1. App searches rate books for a matching client name or code.
2. If an active match is found, a suggestion card appears below the CLIENT field:
   `"Load BP Baytown, TX 2024?"  [Load] [Dismiss]`
3. Clicking **Load** applies that rate book immediately.
4. If multiple matches exist, show a small list of options to choose from.
5. If no match exists, no suggestion appears.

### 21.8 Expiry Warning

When a rate book's Expires date is in the past (before today):
- A warning badge/icon appears next to the rate book name in the estimate header.
- A warning badge appears in the Rate Library left sidebar for that rate set.
- When loading an expired rate book, a toast notification warns: "This rate book expired on [date]."
- The estimate can still use an expired rate book — the warning is advisory, not blocking.

---

## 22. Cost Book

> **Legacy reference:** `docs/legacy-app/README.md` — Section 5

### What the Cost Book Is

The cost book is Stronghold's internal cost structure. It defines what it costs us to employ
each type of worker (not just the hourly wage, but all the overhead on top: payroll taxes,
insurance, benefits, G&A). It also defines what it costs us to rent or own equipment.

**This information is confidential — never shown to customers.**

Different regions have different labor costs. A welder in California costs more than a welder
in West Virginia because of different wage rates, workers comp rates, and state taxes. If a
California estimator uses the West Virginia cost book, the job looks more profitable than it
really is — and the bid might be too low to make money.

### 22.1 Page Layout

- **Title:** Cost Book
- **Subtitle:** "Internal costs — What Stronghold PAYS (labor, equipment, expenses, overhead)"
- **INTERNAL USE ONLY** badge (red, top right)
- **Toolbar:** Default Cost Book dropdown | New Cost Book | Export | Save All
- **Sections:** Overhead & Burden Rates | Labor Costs | Equipment Costs | Expenses

### 22.2 Cost Book Stats Header

| Stat | Value |
|---|---|
| Labor Positions | 19 |
| Equipment Items | 12 |
| Expense Types | 12 |
| Total Burden | 40.2% |

### 22.3 Overhead & Burden Rates — The Most Complex Part

The burden section shows all the additional costs ON TOP of the base wage. These add up to
40.2% of the base wage in the default cost book.

#### CRITICAL FEATURE: % vs $ Toggle Per Line Item

**Every single burden line item has a toggle button that switches it between:**
- **% mode** — the burden is calculated as a percentage of the base hourly wage
  (e.g. 7.65% of $42/hr = $3.21/hr extra)
- **$ mode** — the burden is a flat dollar amount per hour regardless of wage
  (e.g. $7.65/hr flat, regardless of the worker's base rate)

**When modes are mixed** (some lines in % mode, some in $ mode), the total burden display shows
both parts: `"11.80% + $7.65/hr"`. This updates in real time across all JCA sections.

**Why this matters:** FICA is a flat percentage of wages, but some benefits (like a flat $X/hr
health benefit) are a fixed dollar amount. The cost book must support both to be accurate.

#### LABOR BURDEN (subtotal: 19.45%)

| Line Item | Default Mode | Default Value | Meaning |
|---|---|---|---|
| FICA / Social Security | % or $ (toggleable) | 7.65% | Federal payroll tax — matches IRS rate |
| FUTA (Federal Unemployment) | % | 0.60% | Federal unemployment insurance |
| SUTA (State Unemployment) | % | 2.70% | State unemployment insurance |
| Workers Comp | % | 8.50% | Insurance protecting workers from job injuries |

#### INSURANCE & BONDS (subtotal: 5.75%)

| Line Item | Default Mode | Default Value | Meaning |
|---|---|---|---|
| General Liability | % | 2.50% | Protects against third-party injury/property claims |
| Auto Insurance | % | 1.00% | Coverage for company vehicles |
| Umbrella / Excess | % | 0.75% | Excess liability coverage |
| Bonding | % | 1.50% | Surety bonds required by some contracts |

#### OTHER OVERHEAD (subtotal: 15.00%)

| Line Item | Default Mode | Default Value | Meaning |
|---|---|---|---|
| Health Benefits | % | 6.00% | Company contribution to employee health insurance |
| 401k Match | % | 3.00% | Company match on retirement savings |
| Training / Safety | % | 1.00% | Safety training, certifications |
| G&A / Admin | % | 5.00% | General and administrative overhead (office, accounting, etc.) |

**Grand Total Burden: 19.45% + 5.75% + 15.00% = 40.20%**

A **Reset Defaults** button appears in the top right of the burden section to restore all values
to the above defaults.

#### Burdened Rate Example

A Pipefitter Journeyman earns $42.00/hr (base ST rate in cost book).
Total burden = 40.20% = 0.402 × $42.00 = $16.88 burden per hour.
Burdened ST rate = $42.00 + $16.88 = **$58.88/hr** (the cost book shows $54.61 — this is
the verified legacy value; the exact calculation factors in the % vs $ toggle configuration).

### 22.4 Labor Costs — What We Pay

Columns: NAV CODE | CRAFT CODE | POSITION | TYPE | ST RATE | OT (1.5×) | DT (2×) | BURDENED ST

**DIRECT LABOR (13 positions):**

| NAV Code | Craft | Position | ST | OT | DT | Burdened ST |
|---|---|---|---|---|---|---|
| PF001 | PF | Pipefitter Journeyman | $42.00 | $63.00 | $84.00 | $54.61 |
| PF002 | PFH | Pipefitter Helper | $28.00 | $42.00 | $56.00 | $38.95 |
| BM001 | BM | Boilermaker Journeyman | $44.00 | $66.00 | $88.00 | $56.84 |
| BM002 | BMH | Boilermaker Helper | $29.00 | $43.50 | $58.00 | $40.07 |
| WD001 | WD | Welder Journeyman | $46.00 | $69.00 | $92.00 | $59.08 |
| WD002 | WDH | Welder Helper | $30.00 | $45.00 | $60.00 | $41.19 |
| MW001 | MW | Millwright Journeyman | $43.00 | $64.50 | $86.00 | $55.72 |
| EL001 | EL | Electrician Journeyman | $44.00 | $66.00 | $88.00 | $56.84 |
| IT001 | IE | Instrument Tech | $48.00 | $72.00 | $96.00 | $61.31 |
| CO001 | OPR | Crane Operator | $48.00 | $72.00 | $96.00 | $61.31 |
| RG001 | RIG | Rigger | $40.00 | $60.00 | $80.00 | $52.37 |
| SC001 | SCF | Scaffold Builder | $36.00 | $54.00 | $72.00 | $47.90 |
| DR001 | DRV | Driver/Teamster | $32.00 | $48.00 | $64.00 | $43.43 |

**INDIRECT LABOR (6 positions):**

| NAV Code | Craft | Position | ST | OT | DT | Burdened ST |
|---|---|---|---|---|---|---|
| PM001 | MGT | Project Manager | $65.00 | $97.50 | $130.00 | $80.32 |
| GF001 | SUP | General Foreman | $52.00 | $78.00 | $104.00 | $65.79 |
| FM001 | SUP | Foreman | $45.00 | $67.50 | $90.00 | $57.96 |
| SW001 | SAF | Safety Watch | $26.00 | $39.00 | $52.00 | $36.72 |
| FW001 | SAF | Fire Watch | $24.00 | $36.00 | $48.00 | $34.48 |
| HW001 | SAF | Hole Watch | $24.00 | $36.00 | $48.00 | $34.48 |

### 22.5 Equipment Costs — What We Pay

| Equipment | Daily | Weekly | Monthly |
|---|---|---|---|
| Crane - 50 Ton | $1,200 | $5,000 | $15,000 |
| Crane - 100 Ton | $1,900 | $8,000 | $26,000 |
| Manlift 40ft | $280 | $1,100 | $3,200 |
| Manlift 60ft | $400 | $1,600 | $4,600 |
| Scissor Lift | $175 | $700 | $2,000 |
| Forklift 5K | $200 | $800 | $2,400 |
| Forklift 10K | $275 | $1,100 | $3,200 |
| Welding Machine 400amp | $75 | $300 | $850 |
| Air Compressor 185cfm | $110 | $440 | $1,300 |
| Light Tower | $55 | $220 | $650 |
| Generator 25KW | $105 | $420 | $1,200 |
| Hydro Blast Unit | $550 | $2,200 | $6,400 |

### 22.6 Expense Costs — What We Actually Pay

**PER DIEM (what we pay workers, vs what we bill the customer):**

| Description | Our Cost/Day |
|---|---|
| Standard Per Diem (Local) | $65.00 |
| Standard Per Diem (Out of Town) | $125.00 |
| Per Diem - High Cost Area | $150.00 |
| Meals Only | $55.00 |

**TRAVEL:**

| Description | Our Cost |
|---|---|
| Mileage | $0.67/mile |
| Company Vehicle | $85.00/day |
| Rental Car | $75.00/day |
| Airfare | $450.00/trip |

**LODGING:**

| Description | Our Nightly Cost |
|---|---|
| Standard Hotel | $120.00/night |
| Extended Stay | $95.00/night |
| Premium Hotel | $175.00/night |
| Man Camp | $85.00/night |

### 22.7 Creating a New Cost Book

The **New Cost Book** button opens a dialog:
- **COST BOOK NAME** — text input. Placeholder: "e.g., Gulf Coast Region"
- **BASE ON** — dropdown. Copies all settings from an existing cost book as the starting point.
- Buttons: Cancel | Create

### 22.8 Multiple Cost Books

The app supports multiple cost books for different regions. The Default Cost Book is the
standard. Additional cost books (e.g. "Gulf Coast Region," "California," "West Virginia") can
have different base wages, different burden percentages, or different flat-dollar burden items.

The estimate form shows which cost book is active (Section 7.7) with a [Change] link.

---

## 23. Staffing Plans

> **Legacy reference:** `docs/legacy-app/README.md` — Section 8

### What Staffing Plans Are

A staffing plan is like an early draft of an estimate that only covers labor. It is built months
or even years before the job starts, when the customer is still planning the shutdown.

Think of it this way: the refinery tells Stronghold in January, "We're planning a big turnaround
in October. Can you rough out what crew you'd need?" The estimator walks the site, looks at
historical similar jobs, and builds a staffing plan. This plan says: "We expect to need 4
pipefitters, 2 welders, 2 foremen, and 1 project manager for 21 days starting October 5."

This staffing plan feeds:
- **Manpower forecast** — shows leadership that we'll need those workers in October
- **Future revenue estimate** — shows an approximate revenue number from that future job
- **Hiring decisions** — gives HR advance warning that we need to find workers

When the customer finally funds the job, the staffing plan converts to a full estimate.

### 23.1 Staffing Plan Library (List Page)

**Layout:** A card grid. One card per staffing plan.

Each card shows:
- Plan number (e.g. SP-S-26-0004-SHELL)
- Status badge: **CONVERTED** (green) | **APPROVED** (blue) | **DRAFT** (gray)
- Job name, Client, Location, Dates, Requested By
- Rough labor total ($)
- Crew summary tags (e.g. "1× Project Manager (Day)", "2× Pipefitter Journeyman")

**Action buttons per card:**
| Button | What It Does |
|---|---|
| **Open** | Opens the staffing plan form in edit mode |
| **Duplicate** | Creates a new editable copy of this plan with a new SP number (status = Draft). Does NOT create an estimate. |
| **Delete** | Permanently deletes the plan. Only available on non-Converted plans. |
| **Convert** | Creates a new estimate pre-populated with all the plan's data. The plan becomes CONVERTED and read-only. |

**Filter bar:** All Status ▼ | All Branches ▼ | Quick filter (client/location text search)

---

## 24. Staffing Plan Form

> **Legacy reference:** `docs/legacy-app/README.md` — Section 9

### How It Differs from the Estimate Form

The staffing plan form is the SAME component as the estimate form with the MODE dropdown set
to "STAFFING PLAN." When in staffing plan mode:

| Aspect | Estimate Mode | Staffing Plan Mode |
|---|---|---|
| Equipment section | ✅ Visible | ❌ Hidden (staffing plans are labor-only) |
| Expenses section | ✅ Visible | ❌ Hidden |
| Consumables section | ✅ Visible | ❌ Hidden |
| MOB/DEMOB section | ✅ Visible | ❌ Hidden |
| Cost section name | JOB COST ANALYSIS | REVENUE & COST FORECAST |
| KPI card labels | Billed to Client / Actual Cost / Gross Profit / Margin | Estimated Revenue / Actual Cost / Estimated Profit / Margin |
| Footer total label | GRAND TOTAL (blue) | ESTIMATED COST (teal) |
| Save button label | Save | Save Plan |
| Locked indicator | Yellow banner + Edit button | **Editing** green badge (plans are always editable until Converted) |

### 24.1 Staffing Plan Toolbar

```
Open Plans | Save Plan | [Editing] | Upload Contract | Extract Rates | Standard Rates |
Export CSV | Export Excel | Revisions | Change Orders | Pending ▼ | Search | Settings
```

### 24.2 Status Bar (Very Bottom of Page)

```
New Staffing Plan (unsaved) | Rates: Standard (Baseline)
                              Ctrl+S: Save | Ctrl+Z: Undo | ?: Shortcuts | Bid Manager v2.0
```

This bar always shows:
- Current save state of the plan (saved / unsaved)
- Name of the active rate book ("Rates: Standard (Baseline)" or the loaded client rate set)
- Keyboard shortcut reminders

### 24.3 Revenue & Cost Forecast Section (Internal)

This is the staffing plan's version of the JCA. It is INTERNAL USE ONLY and shows:

| KPI Card | Meaning |
|---|---|
| ESTIMATED REVENUE | What we would bill the customer if this staffing plan became an estimate, based on hours × billing rates |
| OUR ACTUAL COST | Cost book rates + burden applied to the same hours |
| ESTIMATED PROFIT | Estimated Revenue − Our Actual Cost |
| PROFIT MARGIN | (Estimated Profit ÷ Estimated Revenue) × 100 — shown as gauge chart |

---

## 25. Staffing Plan Conversion

> This is one of the most important workflows in the app. It must work perfectly.

### The Scenario

It is February 2026. The estimator built SP-S-26-0004-SHELL six months ago for Shell's Deer Park
Turnaround. Now Shell called and said "it's a go — we need a formal bid." The estimator clicks
**Convert** on that staffing plan card.

### 25.1 What Happens When Convert Is Clicked

Step by step:
1. A new estimate is created with a new estimate number (e.g. `26-0024-SHELL`).
2. All data from the staffing plan copies into the estimate:
   - All header fields: client, site, dates, shift, hours/shift, OT rules
   - All labor rows: positions, daily headcount grid, hours, current rates
   - The active rate book assignment
3. The estimate is opened in **fully editable** mode — it is NOT locked. The estimator can now
   add equipment, expenses, consumables, MOB/DEMOB, and refine the labor as needed.
4. The estimate header shows a reference: `Staff Plan#: SP-S-26-0004-SHELL`
5. The first revision is auto-created with the note: `"Converted from plan SP-S-26-0004-SHELL"`

### 25.2 What Happens to the Staffing Plan

After conversion:
1. The staffing plan status badge changes to **CONVERTED** (green).
2. The Convert button on the plan card becomes **grayed out and disabled**.
3. The plan is **read-only** — no more edits allowed.
4. The plan **no longer counts** toward manpower forecast (the new estimate takes over).
5. The plan **no longer counts** toward future revenue pipeline (the estimate counts instead).

### 25.3 Why This Double-Counting Prevention Matters

Before conversion, the staffing plan shows up in:
- Manpower Forecast (as planned future demand)
- Future Revenue on the dashboard (as planned future income)

After conversion, if both the plan AND the new estimate counted, we'd be double-counting the same
job. 65 pipefitters would show up as 130. $1M of revenue would look like $2M. The conversion
makes the plan go silent and lets the estimate speak for it.

---

## 26. Crew Templates

> **Legacy reference:** `docs/legacy-app/README.md` — Section 14

### What Crew Templates Are

Crew templates are saved preset crews. Instead of adding 13 individual positions one by one for
a standard shutdown, the estimator clicks **Load Crew**, picks "Shutdown Crew," and all 13
positions appear at once with the correct headcounts.

**Critical rule:** Templates do NOT store billing rates. They only store position names and
headcounts. When the template is applied, it pulls rates from the currently active rate book.
This means the same template applied to a Shell estimate uses Shell rates, and applied to a BP
estimate uses BP rates.

### 26.1 Load Crew Template Modal

Triggered by **[🧑 Load Crew]** in the labor section header.

```
🧑 Load Crew Template                                       [×]

[Boilermaker Crew]                          [6 labor] [0 equip]
1× Project Manager, 1× General Foreman, 1× Pipefitter Journeyman +3 more
Crew for pressure vessel and boiler work

[Electrical Crew]  ← selected (highlighted) [3 labor] [0 equip]
1× General Foreman, 1× Electrician Journeyman, 1× Electrician Journeyman
Crew for electrical and instrumentation work

[Mechanical Crew]                           [6 labor] [0 equip]
1× Project Manager, 1× General Foreman, 1× Pipefitter Journeyman +3 more
Crew for mechanical equipment installation

[Shutdown Crew]                             [13 labor] [0 equip]
1× Project Manager, 1× General Foreman, 1× Foreman +10 more
Large crew for major turnaround/shutdown work

                Templates can be managed in the Rate Library
                                           [Cancel]
```

### 26.2 What Happens When a Template Is Applied

1. The template's positions and headcounts are added as new rows in the labor section.
2. Each new row's ST/OT/DT billing rates are pulled from the currently active rate book.
3. Each new row's cost rates are pulled from the currently active cost book.
4. A toast notification appears: `"Loaded 'Shutdown Crew' — 13 labor, 0 equipment added."`

### 26.3 Template Management Location

Templates are managed at: **Rate Library** → select any rate set → **Templates tab**.

### 26.4 Template Apply on Unsaved Estimate — Required Guard

If the AI sidebar tries to apply a crew template (`apply_template` action) and the estimate
does not have an ID yet (it has never been saved), the AI must NOT silently fail.

Required behavior: the AI shows this message in the chat:
```
Save the estimate first, then I can apply the template.
[Save Now]
```
The [Save Now] button saves the estimate and then automatically applies the template.

---

## 27. Financial Dashboard

> **Legacy reference:** `docs/legacy-app/README.md` — Section 11

### What the Dashboard Shows

The Financial Dashboard is the "big picture" view for leadership. It answers: How much work have
we won? How much are we waiting on? What did we lose? What does our margin look like? What future
work is in the pipeline from staffing plans?

### 27.1 Page Location

Navigation → Reports (left sidebar)

### 27.2 Time Filter

MTD (Month to Date) | QTD (Quarter to Date) | **YTD** (Year to Date, default) | ALL

All KPI cards and charts filter by this selection.

### 27.3 KPI Cards (Top Row)

Eight cards across the top:

| Card | Color | Example | Meaning |
|---|---|---|---|
| AWARDED REVENUE | Green | $1.0M (10 jobs) | Total dollar value of all estimates with status Awarded |
| PENDING PIPELINE | Blue | $378K (8 bids) | Total value of Pending/Submitted estimates — work we submitted but haven't heard back on yet |
| LOST REVENUE | Red | $120K (2 jobs) | Total value of estimates we lost — money that went to a competitor |
| TOTAL PIPELINE | Dark | $1.5M (36 jobs) | All estimates that are not Draft or Lost |
| WEIGHTED PIPELINE | Orange | $1.0M | Awarded + (Pending × the weight slider %). Accounts for the fact that not all pending bids will be won |
| AVG MARGIN | Yellow | 68.0% | Average gross margin percentage on awarded estimates — are we making the right profit? |
| FUTURE REVENUE | Purple | $968K (4 plans) | Estimated revenue from staffing plans that are Approved but not yet converted to estimates |
| FUTURE PROFIT | Green | $277K (28.7%) | Estimated profit from those same staffing plans |

**Manpower Forecast widget** (top right corner of card row): A small widget with an "Open report →"
link that navigates to the full Manpower Forecast page.

### 27.4 Pending Weight Slider

A slider bar (0%–100%) below the KPI cards.

This slider adjusts the **Weighted Pipeline** calculation. If the slider is at 50%, the
Weighted Pipeline = Awarded Revenue + (Pending Pipeline × 50%). The idea is that not all
pending bids will be won — the slider lets leadership set their expected win rate for pending work.

The slider setting persists between sessions.

### 27.5 Revenue vs Cost Analysis Chart

A multi-line chart showing business performance over time.

- **Filter:** All Customers ▼ (or select a specific customer)
- **View tabs:** Monthly | Quarterly | Cumulative
- **4 lines:**
  - **Billed (blue)** — revenue actually invoiced
  - **Planned (purple dashed)** — revenue from staffing plans
  - **Cost (red)** — internal cost from cost book
  - **Profit (green)** — billed minus cost
- X-axis: months (Jan–Dec)
- Y-axis: dollar amounts ($0 to ~$900K in the example data)

### 27.6 Win/Loss Breakdown Panel (Right Side)

```
10          2          8
WON       LOST     PENDING

      83%
    WIN RATE
```

Shows count of won, lost, and pending bids plus the win rate percentage. This tells leadership
whether the sales/estimating team is winning bids at a healthy rate.

### 27.7 Future Revenue Pipeline (Staffing Plans Section)

This section is separate from the Job Estimates list and shows only staffing plans.

**Summary stats row:** Plans in Period: N | Est. Revenue: $XXX | Est. Cost: $XXX | Est. Profit: $XXX | Avg Margin: X%

**Table columns:** PLAN NUMBER | PLAN NAME | CLIENT | BRANCH | EST. REVENUE | EST. COST | EST. PROFIT | MARGIN | START DATE | END DATE | STATUS

Only Approved (not Converted, not Draft) staffing plans appear here.

**Data source rule:** The Avg Margin on this card requires the `EstimateSummary` table to have
`GrossMarginPct` values. Without this, the card shows 0%.

### 27.8 Job Estimates List

A scrollable table below the chart:

**Columns:** JOB NUMBER | JOB NAME | CLIENT | BRANCH | BILLED | COST | PROFIT | MARGIN | STATUS | TENTATIVE START | DATE SUBMITTED

Click any row to open the estimate form for that estimate.

### 27.9 Lost Bid Analysis Section

Explains WHY we lost bids so leadership can adjust strategy.

- **Donut chart:** Each slice is a lost reason category (Not specified, Went with competitor, etc.)
- **Table:** JOB NUMBER | JOB NAME | CLIENT | VALUE | REASON | DATE

Observed example rows:
- 26-0014-SHELL — Shell Deer Park Heat Exchanger — Shell — $87,318 — Not specified — Mar 6, 2026
- 26-0008-VLO — Willy Wonka 34t-2 — Valero Energy — $33,040 — Went with competitor — Feb 13, 2026

---

## 28. Revenue Forecast

### What It Combines

The revenue forecast shows leadership what money is expected to come in. It pulls from multiple
sources:

1. **Awarded estimates** — full dollar value (committed revenue)
2. **Pending estimates** — weighted by their confidence percentage or by the pending weight slider
3. **Approved staffing plans** (not yet converted) — labeled as "Planned / Future Revenue"
4. **FCO revenue** — additional revenue from approved FCOs, shown separately

### Exclusions

- **Lost estimates** — never count as future revenue. They appear only in the Lost Bid Analysis.
- **Converted staffing plans** — do NOT count. Their linked estimate counts instead.
- **Draft estimates** — excluded by default (configurable).

### Forecast Chart

The Revenue vs Cost Analysis chart on the Financial Dashboard (Section 27.5) is the primary
revenue forecast view. A separate Revenue Forecast page may show the same data in tabular form
with more filter options.

---

## 29. Manpower Forecast

> **Legacy reference:** `docs/legacy-app/README.md` — Section 12

### What the Manpower Forecast Does

Turnaround companies live or die on having the right people in the right place at the right time.
If a job in June needs 12 pipefitters and we only have 6 on payroll, we need to hire 6 more —
but hiring takes weeks. The manpower forecast gives leadership advance warning.

The forecast answers:
- How many workers do we have RIGHT NOW on awarded jobs?
- How many will we need each month for the next 6 months?
- Which crafts are short (we need more people than we currently have)?
- When do current jobs end so those workers become available?

### 29.1 Page Header

- **Title:** Manpower Forecast
- **Subtitle:** "Current = awarded work this month. Forecast = staffing plans + active estimates in selected range."
- **Export buttons (top right):** Export CSV | Export PNG | Reports

### 29.2 Filter Bar (Run on Demand)

**IMPORTANT:** The forecast does NOT calculate automatically. The user sets filters and then
clicks **Run Forecast** to trigger the calculation.

| Filter | Options | Explanation |
|---|---|---|
| FROM DATE | Date picker | Start of the forecast window |
| TO DATE | Date picker | End of the forecast window |
| INCLUDE | Plans + Estimates / Plans only / Estimates only | Which source records to include |
| STATUS FILTER | All Active / specific status | Filter by estimate/plan status |
| POSITIONS | Multi-select dropdown | Filter to specific craft positions only |
| **Run Forecast** | Blue button | Click to execute the calculation |

### 29.3 KPI Cards (Appear After Clicking Run Forecast)

| Card | Example | Meaning |
|---|---|---|
| CURRENT FIELDED (WON) | 12 (24 estimate(s) \| 4 plan(s)) | How many workers are on currently-awarded active jobs right now |
| PEAK NEED IN RANGE | 65 (Peak: Apr 26) | The highest single-month headcount demand across the forecast window |
| END OF RANGE NEED | 0 (Gap: -12, Oct 26) | How many workers needed at the END of the selected range, and how that compares to current |
| PEAK GAP VS CURRENT | +53 (3 month(s) above current) | The largest gap between what we need and what we currently have fielded |

### 29.4 Position Breakdown Table

Shows a row for each position and columns for each month in the forecast range.

**Columns:** POSITION | CURRENT | [Apr 26] | [May 26] | [Jun 26] | ... | END GAP | PEAK

| Column | Explanation |
|---|---|
| POSITION | The craft position name (Pipefitter Journeyman, etc.) |
| CURRENT | Workers currently on awarded jobs this month |
| [Month N] | Workers needed that month (green cell = there is demand; pink/red = demand exceeds current) |
| END GAP | Headcount needed at end of range minus CURRENT. **Red** when negative (we're short). |
| PEAK | The month with highest demand for this position, in format "(N) MonthYY" |

Special cells:
- `--` = no demand for this position in that month
- Green background = headcount needed
- Pink/red background = shortage (need more than we currently have)
- **TOTAL row** pinned to the bottom with dark background, summing all positions

**Example from legacy app:**

| Position | Current | Apr 26 | May 26 | Jun 26 | Jul 26 | End Gap | Peak |
|---|---|---|---|---|---|---|---|
| Foreman | 2 | 6 | 3 | 4 | 1 | -2 | (6) Apr 26 |
| Project Manager | 2 | 11 | 7 | 6 | 1 | -2 | (11) Apr 26 |
| Pipefitter Journeyman | 0 | 4 | 4 | 2 | 1 | — | (4) Apr 26 |
| **TOTAL** | **12** | **65** | **47** | **32** | **8** | **-12** | **(65) Apr 26** |

### 29.5 Headcount Over Time Chart

A multi-line chart below the table:

- One line per position
- **TOTAL** line: large blue, bold — shows total headcount demand by month
- **Current Baseline** line: dashed — shows the current fielded number as a flat reference
- X-axis: months in the selected range
- Y-axis: headcount number
- **Interactive tooltip:** hovering over any month shows a breakdown by position for that month
- **Legend** below the chart
- **Footer text:** "Analyzed N record(s) across N month(s). Peak demand: N (MonthYY) | Peak gap vs current: +N."

---

## 30. Calendar

### What It Shows

A time-based view of all estimates and staffing plans, showing when each job is scheduled.

### Features

- Job start and end dates displayed as calendar bars/events
- Staffing plan start and end dates
- Status badge on each event (Awarded, Pending, Submitted, etc.)
- Customer/site label on each event
- Click any event to open the source estimate or staffing plan
- Converted staffing plans shown with CONVERTED badge

### Future Enhancements (Post-Demo)

- Day / week / month view toggle
- Filters: customer, status, craft, shift, branch
- Crew demand overlay from the manpower forecast

---

## 31. AI Assistant

> **Legacy reference:** `docs/legacy-app/README.md` — Section 13

### What the AI Does

The AI assistant is a chat interface built into the estimate form that helps estimators build
estimates faster using natural language. Instead of clicking through dozens of dropdowns, the
estimator types: "Add 4 pipefitters and 2 foremen for a 14-day Shell job starting June 1" and
the AI fills in the header, adds the labor rows, and loads the Shell rate book.

### 31.1 UI Layout

- A **sparkle/star FAB button** (floating action button) on the estimate form opens the panel
- Panel slides in from the right side of the screen
- **Header:** "AI Assistant" | [Clear Chat] button
- **Tabs:** CHAT | RATES
- **Input area:** Text input at the bottom with a Send button

### 31.2 Backend Configuration

- **AI Engine:** Groq LLaMA 3.3-70b
- **API Key location:** `Api/appsettings.Local.json` (gitignored — NEVER committed to source control)
- **Key format:**
  ```json
  {
    "Ai": {
      "GroqApiKey": "gsk_..."
    }
  }
  ```
- **Post-demo plan:** Replace with Azure AI Foundry + GPT-4o-mini for grounded database queries

### 31.3 CHAT Tab — Quick Start Reference

The chat tab shows example commands to help new users:

```
⚡ QUICK START
• "new estimate"                               — Start guided estimate creation
• "new staffing plan"                          — Create a staffing plan
• "estimate for BP, 14 days, 6 welders, 2 PMs" — One-liner auto-fill

🔥 RATE MANAGEMENT
• "load BP" or "show BP rates"   — Load & view client rates
• "what rates do you have?"      — See all loaded rates
• "show all rate libraries"      — List available clients
• "what's the foreman rate?"     — Check a specific rate

🏗 BUILD YOUR ESTIMATE
• "add 4 pipefitters"           — Add crew
• "add 2 foremen night shift"   — Specify shift
• "use turnaround crew"         — Apply a template
• "set hours 12"                — Change hours/shift

💡 Pro Tip: "Create estimate for Shell, start Feb 15, 21 days, day shift, 10 hours,
              add 3 welders and 2 fitters, use Shell rates"
```

### 31.4 RATES Tab

The RATES tab lists every available rate book, grouped by client:

```
Available Rate Libraries — Click any client to load their rates:

BP - El Reno, OK          (15 labor rates, 12 equipment rates)
BP - Houston tx           (15 labor rates, 12 equipment rates)
BP Baytown, TX 2024       (17 labor rates, 11 equipment rates)
Chevron El Segundo, CA 2024  (17 labor rates, 11 equipment rates)
...
```

Clicking any entry instantly loads that rate book into the current estimate. This is the AI
version of the Rate Book Fallback Picker — both load the same list.

### 31.5 Context Auto-Loading

When the estimator opens an estimate, the AI automatically loads context from related records
and shows a message like:
```
Loaded 26-0024-SHELL: Shell Deer Park Turnaround | Client: Shell | Status: submitted | Total: $142,140
Loaded SP-S-26-0007-XOM: ExxonMobil Baytown Expansion | Client: ExxonMobil | Status: approved
...and 12 more rates. View complete list in the Rates tab →
```

### 31.6 AI Actions (What the AI Can Do)

| Action | What It Does |
|---|---|
| `fill_header` | Fills the estimate header fields (job name, client, dates, shift, hours, OT rules) from a natural language description |
| `add_labor_rows` | Adds labor rows with positions and headcount; pulls rates from active rate book |
| `set_dates` | Sets start/end dates and calculates the duration |
| `load_rate_book` | Fuzzy-matches a client name (e.g. "Shell" → "Shell Oil Company") and loads the best matching active rate book |
| `apply_template` | Applies a named crew template to the labor section (requires saved estimate — see Section 31.7) |

### 31.7 Apply Template Guard — Unsaved Estimate

If `apply_template` is triggered and the estimate has never been saved (no estimate ID exists):
- The AI must NOT fail silently.
- The AI must show in the chat:
  ```
  Save the estimate first, then I can apply the template.
  [Save Now]
  ```
- Clicking [Save Now] saves the estimate and then immediately applies the template.

### 31.8 Rate Anomaly Warnings

If the AI loads rates that are more than 20% above or below historical benchmarks for that
position, it shows a warning in the chat:
```
⚠️ Pipefitter Journeyman rate ($125/hr) is 47% above typical — is this a high-cost area?
```

### 31.9 AI Safety Rules

- AI must describe what it plans to fill in and wait for the user to confirm before writing.
- AI must not make irreversible changes without user confirmation.
- AI must use the company context from the server-side JWT token — never from client input.

### 31.10 Monday Demo Script

1. Open new estimate → click AI FAB → type:
   `"New turnaround estimate for Valero Corpus Christi TX, both shifts 12 hours, 21 days starting June 1"`
   → Header auto-fills with client, dates, shift, hours.
2. Type: `"Add 2 general foremen, 4 pipefitter journeymen, 4 helpers, 2 boilermakers day shift"`
   → Labor rows appear with rates. Rate anomaly warnings may appear.
3. Upload a sample RFQ PDF → AI extracts client, dates, crew positions → click Apply.
4. Type: `"Find rates for Valero"` → rate book suggestion card appears.
5. Save estimate → type: `"Use the shutdown crew template"` → template applies with live rates.

---

## 32. Proposal PDF

### What It Is

The proposal PDF is the customer-facing document sent to the refinery for their records and
approval. It looks professional and shows only what the customer needs to see.

### 32.1 What to Include

- Company logo and branding
- Estimate number and revision number
- Customer name and site
- Job name
- Schedule (start date, end date, number of days)
- Scope summary (from a text field or AI-drafted description)
- Labor billing summary (positions, hours, billing rates, subtotals) — using BILLING RATES, not cost book rates
- Equipment billing summary
- Expenses (billable) summary
- Consumables summary
- MOB/DEMOB summary
- Discount amount (if any)
- Tax rate and amount
- **GRAND TOTAL** — prominently displayed

### 32.2 What to EXCLUDE (NEVER Show the Customer)

- Internal labor cost rates (cost book rates)
- Labor burden amounts or percentages
- Gross profit in dollars
- Gross margin percentage
- The entire Job Cost Analysis (JCA) section
- Which cost book is active
- Any line labeled "INTERNAL USE ONLY"

### 32.3 Preview Button

The **Preview** button in the toolbar opens a PDF preview inside the app so the estimator can
see exactly what the customer will receive before exporting. The preview must reflect all edits
in real time.

---

## 33. Dark Mode / Light Mode

### 33.1 What It Does

The app supports two visual themes: dark (dark backgrounds, light text — good for daily use
at the office) and light (white backgrounds, dark text — better for printing and screen sharing).

### 33.2 Toggle Location

- **Settings page:** A toggle switch in the top-right corner of the Settings page.
- The toggle may also appear in the main application top bar for quick access during daily use.

### 33.3 Persistence

- The selected theme saves to `localStorage`.
- On the next session, the saved theme is applied BEFORE the first render so there is no
  "flash of wrong theme" when the page loads.

### 33.4 Default Theme

Dark mode is the default. Light mode is available and must look correct on all pages.

### 33.5 Print Views

Proposal PDFs and print-formatted pages must use a light color scheme regardless of the active
UI theme. A dark-background proposal PDF is unacceptable.

---

## 34. Navigation Structure

> **Legacy reference:** `docs/legacy-app/README.md` — Section 2

### Left Sidebar Navigation (Top to Bottom)

| Nav Item | What It Opens |
|---|---|
| **Estimates** | The estimate list — all estimates for this company |
| **Staffing Plans** | The staffing plan library — card grid of all plans |
| **Rate Library** | Client billing rate sets — manage, view, create |
| **Cost Book** | Internal cost configuration — labor costs, burden, equipment costs |
| **Reports** | Financial Dashboard — KPI cards, charts, win/loss, lost bid analysis |
| **Manpower Forecast** | Headcount demand table and chart — run on demand |
| **Revenue Forecast** | Revenue/cost forecast charts and tables |
| **Calendar** | Job timeline calendar |
| **Settings** | Company info, branches, customer codes, job number format, defaults |

### 34.1 Status Bar (Bottom of Staffing Plan Form)

```
New Staffing Plan (unsaved) | Rates: Standard (Baseline)
                              Ctrl+S: Save | Ctrl+Z: Undo | ?: Shortcuts | Bid Manager v2.0
```

---

## 35. Architecture Rules

### Why Architecture Rules Matter

The previous version of the app had views that did too much — API calls scattered inside
Vue components, calculations duplicated in 3 different places, business logic embedded in
templates. When something changed (like a new overtime rule), it had to be fixed in 5 places.
This app must avoid that pattern.

### 35.1 Frontend (Vue / Vite)

| Layer | What It Does | What It Does NOT Do |
|---|---|---|
| **Views** | Orchestrate workflow — call composables, coordinate stores | Make API calls directly; contain business logic |
| **Components** | Render UI — receive props, emit events | Call APIs, write to stores, contain calculation logic |
| **Composables** | Own reusable behavior and local workflow state | Own shared state across pages |
| **Services** | Make all API calls (axios/fetch) — one service per domain | Contain UI logic or state |
| **Stores (Pinia)** | Hold shared application state — data used by multiple views | Contain view logic |
| **Utilities** | Shared helper functions (formatting, date math, etc.) | Depend on Vue or stores |

**Calculation rule:** Overtime/double-time logic, burden math, and total calculations are
centralized in a utility or service. They are NOT duplicated inside Vue components.

### 35.2 Backend (.NET 10 / EF Core)

1. Entity Framework Code First. Run `Database.Migrate()` on startup.
2. Never hand-write migrations. Always use `dotnet ef migrations add [name]`.
3. Rate and cost calculations must be expressible as unit-testable pure functions.
4. Authentication is JWT. Company context is enforced server-side on every request.
5. Estimate number generation is server-side, atomic, collision-safe.
6. `appsettings.Local.json` is gitignored. Secrets (API keys, connection strings) live there.
7. Never commit secrets to source control.

---

## 36. Seed Data Requirements

> The seed data must make the app tell a real, convincing business story for Monday's demo.
> Every dashboard KPI, chart, and table should show meaningful data — not zeros and placeholders.

### 36.1 Estimates (18 records — match the legacy app exactly)

| Job Number | Job Name | Client | Status | Total |
|---|---|---|---|---|
| 26-0003-SHELL | Catdog 24-10h | Shell Oil Company | Submitted | $168,165 |
| 26-0007-XOM | Rusty 16-43 | ExxonMobil | Awarded | $71,610 |
| 26-0009-SHELL | Shell Norco Pipe Replacement | Shell | Awarded | $99,225 |
| 26-0010-BP | BP Baytown Unit Turnaround | BP | Awarded | $174,636 |
| 26-0011-VLO | Valero Houston Maintenance | Valero | Pending | $52,928 |
| 26-0012-XOM | ExxonMobil Baton Rouge Expansion | ExxonMobil | Awarded | $246,078 |
| 26-0013-CVX | Chevron El Segundo Small Repair | Chevron | Pending | $39,690 |
| 26-0014-SHELL | Shell Deer Park Heat Exchanger | Shell | Lost | $87,318 |
| 26-0015-BP | BP Texas City Emergency Tie-in | BP | Submitted | $23,814 |
| 26-0016-VLO | Valero Port Arthur Inspection Support | Valero | Pending | $99,225 |
| 26-0017-XOM | ExxonMobil Beaumont Tank Repair | ExxonMobil | Awarded | $85,995 |
| 26-0018-CVX | Chevron Pascagoula Piping Modification | Chevron | Pending | $125,685 |
| 26-0020-CVX | Chevron Pascagoula Unit Tie-in | Chevron | Submitted | $55,808 |
| 26-0021-BP | Marathon Petroleum Estimate | Marathon Petroleum | Submitted | $19,845 |
| 26-0022-VLO | Valero Port Arthur Maintenance | Valero | Pending | $41,745 |
| 26-0023-BP | Molasses 44-14A | Marathon Petroleum | Submitted | $33,075 |
| 26-0024-SHELL | Shell Deer Park Turnaround | Shell | Submitted | $142,140 |
| 26-0025-SHELL | Kangaroo 11-25h | Shell | Submitted | $47,628 |

### 36.2 Staffing Plans (10 records)

| Plan Number | Job Name | Client | Status | Rough Labor |
|---|---|---|---|---|
| SP-S-26-0001-BP | Charlie 26h | BP Refinery | Converted | — |
| SP-S-26-0002-SHELL | Catdog 24-10h | Shell Oil Company | Converted | — |
| SP-S-26-0003-SHELL | Gringo 25-12h | Shell | Draft | $18,744 |
| SP-S-26-0004-SHELL | Shell Deer Park Turnaround | Shell | Converted | $129,780 |
| SP-S-26-0005-BP | BP Texas City Piping | BP | Converted | $45,440 |
| SP-S-26-0006-VLO | Valero Port Arthur Maintenance | Valero | Converted | $52,580 |
| SP-S-26-0007-XOM | ExxonMobil Baytown Expansion | ExxonMobil | Approved | $819,294 |
| SP-S-26-0008-CVX | Chevron Pascagoula Unit Tie-in | Chevron | Converted | $33,488 |
| SP-S-26-0009-SHELL | Shell Martinez Heat Exchanger | Shell | Approved | $56,430 |
| SP-S-26-0010-BP | BP Whiting Emergency Repair | BP | Approved | $73,280 |

### 36.3 Labor Positions Required in Seed Data

Seed data must use ONLY turnaround crafts. Remove any electrical-only test positions:

**Direct:** Pipefitter Journeyman, Pipefitter Helper, Boilermaker Journeyman, Boilermaker Helper,
Welder Journeyman, Welder Helper, Millwright Journeyman, Electrician Journeyman, Instrument Tech,
Crane Operator, Rigger, Scaffold Builder, NDT Technician, Driver/Teamster

**Indirect:** Project Manager, General Foreman, Foreman, Safety Watch, Fire Watch, Hole Watch

### 36.4 Required Fields on Staffing Plan Labor Rows

Every seeded staffing plan labor row MUST have these fields populated (not null, not 0):
- `ScheduleJson` — the daily headcount grid data
- `StHours` — straight-time hours total
- `OtHours` — overtime hours total
- `Subtotal` — total dollar amount for the row

Without these, the Manpower Forecast will show a blank table and the Revenue/Cost Forecast
on the staffing plan will show $0.

### 36.5 Required Fields on Estimate Summary Records

Every seeded estimate MUST have a corresponding `EstimateSummary` record with:
- `GrossMarginPct` — gross margin as a percentage (e.g. 43.3 not 0.433)
- `GrossProfit` — gross profit in dollars
- `TotalBilled` — grand total billed to customer

Without these, the Financial Dashboard Avg Margin card will show **0.0%**.

---

## 37. Playwright Test Strategy

### The Core Rule

**Never call `POST /api/v1/dev/reset` from any test.** That endpoint wipes the entire database
including all demo seed data. Tests must only create and delete their own test records.

### 37.1 Test Record Naming Convention

All records created by tests are prefixed with `[PW-TEST]`:
- Estimate name: `[PW-TEST] Playwright Pipe Job`
- Client: `[PW-TEST] Test Client Inc.`

This prefix allows global cleanup without touching real demo data.

### 37.2 Lifecycle

```
globalSetup      → login → if DB has no estimates: seed demo data → never call reset-standard
test beforeAll   → POST [PW-TEST] records via API → capture their IDs
test             → interact via UI → assert expected state → verify via GET API call
test afterAll    → DELETE every [PW-TEST] record created in beforeAll (using captured IDs)
globalTeardown   → safety net: DELETE any remaining [PW-TEST] records (in case afterAll missed one)
```

### 37.3 Test Types

| Type | Description | Safe to run? |
|---|---|---|
| Mocked tests | No real API calls — all data is mocked | Any time |
| Live tests | Real API + real DB writes | Safe once afterAll cleanup is wired |

---

## 38. Open Decisions

These items have not been fully resolved. They need stakeholder input before implementation.

1. **Job letter spreadsheet** — The full list of job letter codes (e.g. H = ETS) comes from a
   spreadsheet. This spreadsheet needs to be imported into the Settings configuration.

2. **Final status vocabulary** — Is "Submitted" the same as "Pending"? Or are they distinct?
   Confirm the complete list: Draft / Pending / Submitted / Awarded / Lost / Archived.

3. **Scenario compare/promote** — What-if alternate versions of an estimate for side-by-side
   comparison. Confirm whether this is needed for the demo or is post-demo only.

4. **MSA number automation** — Currently a free-text placeholder. Does it need lookup or
   validation against a contract list? No automation needed before demo.

5. **Regional cost book selection** — California, West Virginia, Gulf Coast each need their own
   cost books. Configuration mechanism (which estimates get which book by default) is TBD.

6. **Burden retroactive recalculation scope** — When burden rates change in the cost book, should
   saved snapshots (revision history) recalculate too? Or only the live working estimate?

7. **Equipment contract auto-pull** — The legacy app pulls equipment from an uploaded contract.
   Exact implementation and contract parsing format TBD.

8. **Win/loss reason picklist** — Final list of loss reasons should be configurable in Settings.
   Confirm the initial set of values.

9. **Azure AI Foundry integration** — Post-demo: replace Groq with Azure AI Foundry + GPT-4o-mini
   + grounded database queries. Requires Azure subscription setup and SQL integration planning.
   Demo runs on Groq only.
