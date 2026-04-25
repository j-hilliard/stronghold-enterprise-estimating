# Legacy App Reference — Cat-Spec (CSL Bid Manager v2.0)

> This document is extracted from live screenshots of the Cat-Spec legacy estimating application.
> It serves as the authoritative UI/UX, data, and behavior reference for the Stronghold Enterprise
> Estimating rebuild. Every rate, field name, section label, workflow, and calculation documented
> here was observed directly in the running legacy app.
>
> Screenshots are stored in this folder (save manually from chat history).
> Label convention: `[screen-name]-[section].png`

---

## Table of Contents

1. [Application Overview](#1-application-overview)
2. [Navigation Structure](#2-navigation-structure)
3. [Settings Page](#3-settings-page)
4. [Rate Library](#4-rate-library)
5. [Cost Book](#5-cost-book)
6. [Estimates List & Database View](#6-estimates-list--database-view)
7. [Estimate Form — Full Reference](#7-estimate-form--full-reference)
8. [Staffing Plan Library](#8-staffing-plan-library)
9. [Staffing Plan Form](#9-staffing-plan-form)
10. [Revision History Drawer](#10-revision-history-drawer)
11. [Financial Dashboard (Reports)](#11-financial-dashboard-reports)
12. [Manpower Forecast](#12-manpower-forecast)
13. [AI Assistant Sidebar](#13-ai-assistant-sidebar)
14. [Crew Templates](#14-crew-templates)
15. [Seed Dataset — Real Legacy Estimates](#15-seed-dataset--real-legacy-estimates)
16. [Seed Dataset — Staffing Plans](#16-seed-dataset--staffing-plans)

---

## 1. Application Overview

**App name:** Cat-Spec / Bid Manager v2.0  
**Company:** Cat-Spec, Ltd. (a Stronghold Company)  
**Use case:** Industrial turnaround estimating for petrochemical/refinery clients  
**Multi-user:** Yes — shows "N viewing" badge when multiple users are on the same record  
**Keyboard shortcuts:** Ctrl+S (save), Ctrl+Z (undo), ? (shortcuts reference)  
**Version shown:** Bid Manager v2.0 (shown in bottom status bar)

---

## 2. Navigation Structure

Left sidebar navigation (top to bottom):

| Nav Item | Purpose |
|---|---|
| Estimates | All estimates — list and open/create |
| Contracts | Contract management (not detailed in screenshots) |
| Rate Library | Client billing rate sets |
| Cost Book | Internal labor/equipment cost |
| Reports | Financial dashboard (main analytics) |
| Calendar | Job calendar (not detailed in screenshots) |
| Staffing | Staffing plan library |
| Forecast | Manpower forecast (separate from Reports) |
| Database | Raw DB browser (internal tool) |
| Settings | Company/branch/customer code/format config |

---

## 3. Settings Page

**Screenshot label:** `settings-main.png`  
**Subtitle:** "Manage branches, company info, customer codes, and system preferences"  
**Top bar actions:** Save All Settings | Export Settings | Import Settings  
**Dark mode toggle:** Top-right corner — toggle switch (not a button)

### 3.1 Branches / Locations

| Branch # | Name | City, State |
|---|---|---|
| 100 | Houston Operations | Houston, TX |
| 200 | Beaumont Division | Beaumont, TX |
| 300 | Lake Charles | Lake Charles, LA |

Actions per branch: Edit | Delete  
Add button: **+ Add Branch**

### 3.2 Company Information

| Field | Value |
|---|---|
| Company Name | Stronghold Companies |
| Address | 10344 New Decade Dr, Pasadena TX 77507 |
| Phone | (555) 123-4567 |
| Email | info@stronghold.com |

### 3.3 Customer Codes

| Code | Full Name |
|---|---|
| BP | British Petroleum |
| SHELL | Shell Oil Company |
| OXY | Occidental Petroleum |
| XOM | ExxonMobil |

Actions per code: Delete (×)  
Add button: **+ Add Code**

### 3.4 Job Number Format

**Configurable dropdown.** Format options include named patterns.  
**Pattern shown:** Date-Branch-Client-Seq  
**Example in dropdown:** `250202-100-BP-001`  
**Live preview:** `260424-100-BP-001` (date = April 24, 2026)

> NOTE: Estimate list shows `26-NNNN-CLIENTCODE` format (e.g. `26-0025-SHELL`). The format was
> likely reconfigured at some point. The estimates list format is the authoritative observed format.

### 3.5 Estimate Defaults

| Setting | Value |
|---|---|
| Hours Per Shift | 10 |
| OT After (hours) | 8 |
| Default Duration (days) | 7 |
| DT on Weekends | Yes — Double Time |
| Tax Rate (%) | 0 |

> Implication: A 10-hour shift = 8 ST hours + 2 OT hours per day.

---

## 4. Rate Library

**Screenshot labels:** `rate-library-standard-direct.png`, `rate-library-standard-indirect.png`,
`rate-library-equipment.png`, `rate-library-perdiem.png`, `rate-library-sidebar.png`,
`rate-library-chevron-richmond.png`

**Page subtitle:** "Client rate sheets & pricing"  
**Toolbar:** Rate Set | Duplicate | Delete | Search (all rate sets: labor, equipment, per diem, travel, lodging) | Save | Versions | **Sync to API**

### 4.1 Rate Set Structure

Each rate set has:
- **Name** (e.g. "BP Baytown, TX 2024")
- **Status badge:** ACTIVE (green) or EXPIRED (red)
- **Source:** "Default Rates" or "Contract rates"
- **Effective date** | **Expires date**
- **Stats:** N LABOR | N EQUIPMENT | N OTHER | N TEMPLATES
- **Tabs:** Labor | Equipment | Per Diem | Travel | Lodging | Templates

### 4.2 Rate Set Sidebar (Left Panel)

Grouped by client, with count badge:

**BP (5 rate sets):**
- BP - El Reno, OK — EXPIRED (15 labor, 12 equip, Exp 2026-02-27)
- BP - Houston TX — EXPIRED (15 labor, 12 equip, Exp 2026-02-27)
- BP Baytown, TX 2024 — ACTIVE (17 labor, 11 equip, Exp 2026-12-31)
- BP Texas City, TX 2024 — ACTIVE (labor, equip, Exp 2026-12-31)
- BP Whiting, IN 2024 — ACTIVE (17 labor, 11 equip, Exp 2026-12-31)

**BP Refinery (1):**
- BP Refinery - Bakersfield, CA (15 labor, 12 equip)

**Chevron (3):**
- Chevron El Segundo, CA 2024 — ACTIVE (17 labor, 11 equip, Exp 2026-12-31)
- Chevron Pascagoula, MS 2024 — ACTIVE (17 labor, 11 equip, Exp 2026-12-31)
- Chevron Richmond, CA 2024 — ACTIVE (17 labor, 11 equip, Exp 2026-12-31)

**ExxonMobil (4):**
- ExxonMobil - Houston (14 labor, 14 equip)
- ExxonMobil Baton Rouge, LA 2024 — ACTIVE (17 labor, 11 equip, Exp 2026-12-31)
- ExxonMobil Baytown, TX 2024 — ACTIVE (17 labor, 11 equip, Exp 2026-12-31)
- ExxonMobil Beaumont, TX 2024 — ACTIVE (17 labor, 11 equip, Exp 2026-12-31)

### 4.3 Labor Tab — Standard (Baseline) Rate Set

**DIRECT LABOR — 13 POSITIONS:**

| Position | Type | ST Rate | OT Rate | DT Rate |
|---|---|---|---|---|
| Pipefitter Journeyman | DIRECT | $78.00 | $117.00 | $156.00 |
| Pipefitter Helper | DIRECT | $52.00 | $78.00 | $104.00 |
| Boilermaker Journeyman | DIRECT | $82.00 | $123.00 | $164.00 |
| Boilermaker Helper | DIRECT | $54.00 | $81.00 | $108.00 |
| Welder Journeyman | DIRECT | $85.00 | $127.50 | $170.00 |
| Welder Helper | DIRECT | $55.00 | $82.50 | $110.00 |
| Millwright Journeyman | DIRECT | $80.00 | $120.00 | $160.00 |
| Electrician Journeyman | DIRECT | $82.00 | $123.00 | $164.00 |
| Instrument Tech | DIRECT | $88.00 | $132.00 | $176.00 |
| Crane Operator | DIRECT | $95.00 | $142.50 | $190.00 |
| Rigger | DIRECT | $72.00 | $108.00 | $144.00 |
| Scaffold Builder | DIRECT | $65.00 | $97.50 | $130.00 |
| NDT Technician | DIRECT | $95.00 | $142.50 | $190.00 |

**INDIRECT LABOR — 7 POSITIONS:**

| Position | Type | ST Rate | OT Rate | DT Rate |
|---|---|---|---|---|
| Project Manager | INDIRECT | $125.00 | $187.50 | $250.00 |
| General Foreman | INDIRECT | $98.00 | $147.00 | $196.00 |
| Foreman | INDIRECT | $85.00 | $127.50 | $170.00 |
| Safety Watch | INDIRECT | $48.00 | $72.00 | $96.00 |
| Fire Watch | INDIRECT | $45.00 | $67.50 | $90.00 |
| Hole Watch | INDIRECT | $45.00 | $67.50 | $90.00 |
| Driver/Teamster | INDIRECT | $58.00 | $87.00 | $116.00 |

Actions per row: Edit | (reorder) | Delete  
Add button: **+ Add Labor Rate**

> NOTE: OT rate = ST × 1.5, DT rate = ST × 2.0 (confirmed by observed values)

### 4.4 Labor Tab — Client-Specific Example (Chevron Richmond, CA 2024)

Rates differ from Standard Baseline — negotiated per contract:

| Position | Type | ST | OT | DT |
|---|---|---|---|---|
| Project Manager | INDIRECT | $124.00 | $186.00 | $248.00 |
| General Foreman | INDIRECT | $68.00 | $102.00 | $136.00 |
| Foreman | INDIRECT | $55.00 | $82.50 | $110.00 |
| Safety Watch | INDIRECT | $74.00 | $111.00 | $148.00 |
| Fire Watch | INDIRECT | $72.00 | $108.00 | $144.00 |
| Hole Watch | INDIRECT | $35.00 | $52.50 | $70.00 |
| Driver/Teamster | INDIRECT | $67.00 | $100.50 | $134.00 |
| Electrician Journeyman | DIRECT | $87.00 | $130.50 | $174.00 |
| Millwright Journeyman | DIRECT | $58.00 | $87.00 | $116.00 |
| Instrument Tech | DIRECT | $81.00 | $121.50 | $162.00 |
| NDT Technician | DIRECT | $97.00 | $145.50 | $194.00 |

### 4.5 Equipment Tab — Billing Rates (4 rate types)

Columns: EQUIPMENT | HOURLY | DAILY | WEEKLY | MONTHLY

| Equipment | Hourly | Daily | Weekly | Monthly |
|---|---|---|---|---|
| Forklift 5K | $0.00 | $185.00 | $750.00 | $2,400.00 |
| Forklift 10K | $0.00 | $250.00 | $1,000.00 | $3,200.00 |
| Crane - 50 Ton | $0.00 | $1,200.00 | $5,000.00 | $16,000.00 |
| Crane - 100 Ton | $0.00 | $1,800.00 | $7,500.00 | $24,000.00 |
| Manlift 40ft | $0.00 | $220.00 | $900.00 | $2,800.00 |
| Manlift 60ft | $0.00 | $280.00 | $1,150.00 | $3,600.00 |
| Scissor Lift | $0.00 | $150.00 | $600.00 | $1,900.00 |
| Welding Machine 400amp | $0.00 | $95.00 | $380.00 | $1,200.00 |
| Air Compressor 185cfm | $0.00 | $120.00 | $480.00 | $1,500.00 |
| Light Tower | $0.00 | $75.00 | $300.00 | $950.00 |
| Generator 25KW | $0.00 | $110.00 | $440.00 | $1,400.00 |

Add button: **+ Add Equipment**

### 4.6 Per Diem Tab — Example (Chevron El Segundo)

Columns: DESCRIPTION | RATE/DAY

| Description | Rate/Day |
|---|---|
| Standard Per Diem | $85.00 |
| High Cost Area | $115.00 |

Add button: **+ Add Per Diem**

> Note: Per diem rates vary by client rate set. Standard (Baseline) has 5 per diem entries.

### 4.7 Travel Tab

> Observed: 4 travel entries per Chevron rate set. Full list not captured.

### 4.8 Lodging Tab

> Observed: 3 lodging entries per Chevron rate set. Full list not captured.

### 4.9 Templates Tab

Each rate set shows a Templates count (e.g. 9 templates). Templates are managed here and apply across estimates/staffing plans.

---

## 5. Cost Book

**Screenshot labels:** `cost-book-overhead.png`, `cost-book-labor-direct.png`,
`cost-book-labor-indirect.png`, `cost-book-equipment.png`, `cost-book-expenses.png`,
`cost-book-new-dialog.png`, `cost-book-burden-mixed.png`

**Page subtitle:** "Internal costs — What Stronghold PAYS (labor, equipment, expenses, overhead)"  
**INTERNAL USE ONLY** badge (red, top right)  
**Toolbar:** Default Cost Book dropdown | New Cost Book | Export | Save All

### 5.1 Cost Book Header Stats

| Stat | Value |
|---|---|
| Labor Positions | 19 |
| Equipment Items | 12 |
| Expense Types | 12 |
| Total Burden | 40.2% |
| Last Updated | 4/24/2026 |

### 5.2 Overhead & Burden Rates Section

**CRITICAL: Each burden line item has a % / $ toggle.** Can be entered as a percentage of wage OR
a flat dollar-per-hour amount. When mixed, the total shows as e.g. `"11.80% + $7.65/hr"`.  
The JCA section on estimates and staffing plans reflects this same mixed display in real time.

#### LABOR BURDEN (Total: 19.45%)

| Item | Mode | Value |
|---|---|---|
| FICA / Social Security | % or $ | 7.65% |
| FUTA (Federal Unemployment) | % | 0.60% |
| SUTA (State Unemployment) | % | 2.70% |
| Workers Comp | % | 8.50% |

#### INSURANCE & BONDS (Total: 5.75%)

| Item | Mode | Value |
|---|---|---|
| General Liability | % | 2.50% |
| Auto Insurance | % | 1.00% |
| Umbrella / Excess | % | 0.75% |
| Bonding | % | 1.50% |

#### OTHER OVERHEAD (Total: 15.00%)

| Item | Mode | Value |
|---|---|---|
| Health Benefits | % | 6.00% |
| 401k Match | % | 3.00% |
| Training / Safety | % | 1.00% |
| G&A / Admin | % | 5.00% |

**Grand Total Burden: 19.45 + 5.75 + 15.00 = 40.20%**

> Reset Defaults button visible (top right of section)

### 5.3 Labor Costs — WHAT WE PAY

Columns: NAV CODE | CRAFT CODE | POSITION | TYPE | ST RATE | OT RATE (1.5x) | DT RATE (2x) | BURDENED ST  
Actions per row: Edit | (reorder) | Del  
Add button: **+ Add Position**

**DIRECT LABOR — 13 POSITIONS:**

| NAV Code | Craft | Position | ST | OT (1.5x) | DT (2x) | Burdened ST |
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

**INDIRECT LABOR — 6 POSITIONS:**

| NAV Code | Craft | Position | ST | OT | DT | Burdened ST |
|---|---|---|---|---|---|---|
| PM001 | MGT | Project Manager | $65.00 | $97.50 | $130.00 | $80.32 |
| GF001 | SUP | General Foreman | $52.00 | $78.00 | $104.00 | $65.79 |
| FM001 | SUP | Foreman | $45.00 | $67.50 | $90.00 | $57.96 |
| SW001 | SAF | Safety Watch | $26.00 | $39.00 | $52.00 | $36.72 |
| FW001 | SAF | Fire Watch | $24.00 | $36.00 | $48.00 | $34.48 |
| HW001 | SAF | Hole Watch | $24.00 | $36.00 | $48.00 | $34.48 |

### 5.4 Equipment Costs — WHAT WE PAY

Columns: EQUIPMENT | HOURLY COST | DAILY COST | WEEKLY COST | MONTHLY COST  
Add button: **+ Add Equipment**

| Equipment | Hourly | Daily | Weekly | Monthly |
|---|---|---|---|---|
| Crane - 50 Ton | $0.00 | $1,200.00 | $5,000.00 | $15,000.00 |
| Crane - 100 Ton | $0.00 | $1,900.00 | $8,000.00 | $26,000.00 |
| Manlift 40ft | $0.00 | $280.00 | $1,100.00 | $3,200.00 |
| Manlift 60ft | $0.00 | $400.00 | $1,600.00 | $4,600.00 |
| Scissor Lift | $0.00 | $175.00 | $700.00 | $2,000.00 |
| Forklift 5K | $0.00 | $200.00 | $800.00 | $2,400.00 |
| Forklift 10K | $0.00 | $275.00 | $1,100.00 | $3,200.00 |
| Welding Machine 400amp | $0.00 | $75.00 | $300.00 | $850.00 |
| Air Compressor 185cfm | $0.00 | $110.00 | $440.00 | $1,300.00 |
| Light Tower | $0.00 | $55.00 | $220.00 | $650.00 |
| Generator 25KW | $0.00 | $105.00 | $420.00 | $1,200.00 |
| Hydro Blast Unit | $0.00 | $550.00 | $2,200.00 | $6,400.00 |

### 5.5 Expenses — PER DIEM, TRAVEL, LODGING

Three subsections, each with **+ Add** button.

**PER DIEM:**

| Description | Daily Rate |
|---|---|
| Standard Per Diem (Local) | $65.00/day |
| Standard Per Diem (Out of Town) | $125.00/day |
| Per Diem - High Cost Area | $150.00/day |
| Meals Only | $55.00/day |

**TRAVEL:**

| Description | Rate | Per |
|---|---|---|
| Mileage Reimbursement | $0.67 | mile |
| Company Vehicle | $85.00 | day |
| Rental Car | $75.00 | day |
| Airfare (Average) | $450.00 | trip |

**LODGING:**

| Description | Nightly Rate |
|---|---|
| Standard Hotel | $120.00/night |
| Extended Stay | $95.00/night |
| Premium Hotel | $175.00/night |
| Man Camp | $85.00/night |

### 5.6 Create New Cost Book Dialog

Fields:
- COST BOOK NAME — text input (placeholder: "e.g., Gulf Coast Region")
- BASE ON — dropdown (e.g. "Default Cost Book (Copy)")

Buttons: Cancel | Create

### 5.7 Live Recalculation Rule

When burden rates are edited and saved, the Job Cost Analysis on ALL estimates using that cost book
must automatically recalculate. Costs, gross profit, and margin update without page refresh.

### 5.8 Cost Book Indicator on Estimate Form

The estimate form must show which cost book is currently active, with a link to switch it, so
estimators can verify they are using the correct regional cost book before submitting.

---

## 6. Estimates List & Database View

**Screenshot labels:** `estimates-database-list.png`, `estimates-database-record-detail.png`

### 6.1 List Columns

ID | Job Number | Job Name | Client | Status | Total | Created

### 6.2 Status Values Observed

| Status | Color |
|---|---|
| SUBMITTED | gray pill |
| AWARDED | green pill |
| LOST | red/pink pill |
| PENDING | yellow/orange pill |
| DRAFT | light gray pill |

### 6.3 Database Record Fields (confirmed field names)

`id`, `job_number`, `job_name`, `client`, `location`, `po_number`, `branch`, `status`,
`start_date`, `end_date`, `shift_type` (both/day/night), `hours_per_shift`,
`ot_method` (daily8), `dt_weekends`, `ot_after`

### 6.4 Estimate Number Format

`YY-NNNN-CLIENTCODE` — e.g. `26-0025-SHELL`  
- YY = 2-digit year
- NNNN = 4-digit zero-padded sequence
- CLIENTCODE = customer code (SHELL, BP, VLO, XOM, CVX, etc.)

---

## 7. Estimate Form — Full Reference

**Screenshot labels:** `estimate-form-header.png`, `estimate-form-labor.png`,
`estimate-form-equipment.png`, `estimate-form-expenses.png`, `estimate-form-consumables.png`,
`estimate-form-mob-demob.png`, `estimate-form-jca.png`, `estimate-form-footer.png`,
`estimate-form-revision-history.png`

### 7.1 Top Area

```
[CSL CAT-SPEC LTD. logo]                              MODE: ESTIMATE ▼
                                               26-0001-SHL
                                               April 24, 2026
                                               [Rev 2 badge]
                              Staff Plan#: SP-S-26-0004-SHELL
[=================================] 75% complete
[🔒 This estimate is locked. Click Edit to make changes.]        [✏ Edit]
```

- **MODE dropdown** — ESTIMATE or STAFFING PLAN (same form, toggled)
- **Estimate number** — top right
- **Date** — shown below number
- **Revision badge** — "Rev N" (dark pill)
- **Staff Plan reference** — shows source staffing plan number if converted
- **Progress bar** — colored horizontal bar, % complete shown
- **Lock banner** — yellow/gold; "This estimate is locked. Click Edit to make changes."
- **Edit button** — top right when locked

### 7.2 Toolbar

```
Open | Save | Locked | Submit Bid | Upload Contract | Extract Rates | Standard Rates |
Export CSV | Export Excel | Preview | Revisions | Change Orders | Pending ▼ | Search... | Settings
[bell🔔 N] [user1 user2] [2 viewing] [AD]
```

- **Submit Bid** — marks estimate as submitted to client
- **Upload Contract** — upload signed contract document
- **Extract Rates** — pull rates from uploaded contract
- **Standard Rates** — load standard baseline rates
- **Revisions** — opens revision history drawer
- **Change Orders** — FCO management
- **Pending ▼** — dropdown for pending/approval workflow
- **N viewing** — multi-user awareness badge

### 7.3 Header Fields (Row 1)

| Field | Notes |
|---|---|
| JOB NAME | Free text |
| CLIENT | Free text / autocomplete |
| JOB TYPE | Dropdown (Select...) |
| MSA # | Text (MSA-XXXX placeholder) |
| QUOTE NUMBER | Text (Quote# placeholder) |

### 7.4 Header Fields (Row 2)

| Field | Notes |
|---|---|
| BRANCH | Dropdown (Select Branch...) |
| CITY | Free text |
| STATE | Dropdown (2-letter) |
| COUNTY | Free text (County... placeholder) |
| START DATE | Date picker |
| END DATE | Date picker |
| DAYS | Auto-calculated from dates |

### 7.5 Header Fields (Row 3)

| Field | Notes |
|---|---|
| SHIFTS | Dropdown: Both / Day / Night |
| HRS/SHIFT | Number input |
| OVERTIME RULES | Dropdown: Daily (after 8 hrs), Daily 8 + Weekly 40, Weekly 40, etc. |
| DT W/E | Dropdown: Yes / No |

### 7.6 Header Fields (Row 4)

| Field | Notes |
|---|---|
| VP | Free text (VP name...) |
| DIRECTOR | Free text (Director name...) |
| REGION | Free text (Region...) — example: "Gulf" |
| SITE | Free text (Site/facility...) |
| QUOTE # | Text (Q-XXXX placeholder) |
| CONFIDENCE (0-100) | Number 0–100 |

### 7.7 LABOR Section

```
▼ LABOR                           [All] [Wk1] [Wk2] [Wk3]  [🧑 Load Crew] [+ Add Employee]
```

**Column headers:**
`EMPLOYEE | HRS | ST (green) | OT (orange) | DT (red) | SHFT | [daily date grid] | ST | OT | DT | ST $ | OT $ | DT $ | TOTAL | [×]`

**Daily date grid:**
- One column per calendar day
- Header: day abbreviation + date number (e.g. THU 19, FRI 20, SAT 21...)
- Weekend columns highlighted in yellow
- Cell value = headcount for that position that day (e.g. 1, 2, 3...)
- **This enables ramp up/ramp down** — change headcount per day per position

**Row type badge:** `DIR` (green, direct) or `IND` (yellow, indirect)

**Labor Subtotal row:**
```
Labor Subtotal (D: $62,100 | I: $80,040)    728  364  168  $64,272  $48,204  $29,664    $142,140.00
```
- D: = Direct labor subtotal
- I: = Indirect labor subtotal
- Then ST/OT/DT hour totals and $ totals

**Week filter:** All | Wk1 | Wk2 | Wk3 (shows only that week's daily columns)

### 7.8 EQUIPMENT Section

```
▼ EQUIPMENT                       [All] [Wk1] [Wk2] [Wk3]              [+ Add Equipment]
```

**Columns:** EQUIPMENT | QTY | RATE | TYPE | [daily date grid] | DAYS | TOTAL

- Equipment pulled from contract auto-populates
- Select billing type: hourly/weekly/monthly/daily → rates auto-fill from active rate book
- Equipment Subtotal row

### 7.9 EXPENSES (BILLABLE) Section

```
▼ EXPENSES (BILLABLE)                                              Charged to Client →
```

**Columns:** DESCRIPTION | BILL RATE | DAYS/QTY | PEOPLE | BILLABLE  
**Empty state:** "No expenses added — use buttons below for out-of-town jobs"  
**Subtotal line**  
**Buttons:** [+ Auto Per Diem] (blue) | [+ Expense] (green)

**Auto Per Diem dropdown (quick-add):**

| Option | Rate |
|---|---|
| Standard Per Diem | $65.00/day |
| Per Diem - High Cost Area | $85.00/day |
| Meals Only | $45.00/day |
| Direct | $100.00/day |
| Indirect | $120.00/day |

**+ Expense full dropdown:**

PER DIEM group:
- Standard Per Diem — $65
- Per Diem - High Cost Area — $85
- Meals Only — $45
- Direct — $100
- Indirect — $120

TRAVEL group:
- Mileage Reimbursement — $0.67/mile
- Airfare - Domestic — $500/trip
- Rental Car — $75/day
- Fuel Allowance — $50/day
- (additional items below — list scrolls)

### 7.10 CONSUMABLES Section

```
▼ CONSUMABLES                                                   Materials & Supplies →
```

**Columns:** DESCRIPTION | UNIT | UNIT COST | QTY | SUBTOTAL  
**Empty state:** "No consumables added — use buttons below for materials & supplies"  
**Subtotal line**  
**Buttons:** [+ Add Consumable] (orange) | [+ Common Items] (orange)

### 7.11 MOB / DEMOB Section

```
▼ MOB / DEMOB                                             Mobilization & Demobilization →
```

**Columns:** DESCRIPTION | COST | QTY | SUBTOTAL  
**Placeholder text:** "e.g., Truck Rental - 1 Ton"  
**Buttons:** [+ Add Item] (purple) | [+ Common Items] (purple/dark)

**Example seeded items (from estimate 26-0001-SHL):**

| Description | Cost | Qty | Subtotal |
|---|---|---|---|
| Truck Rental - 1 Ton | $250 | 1 | $250.00 |
| Trailer Rental | $150 | 1 | $150.00 |
| Fuel (round trip) | $500 | 1 | $500.00 |
| Tool Shipment | $800 | 1 | $800.00 |
| Rig-Up Labor (crew) | $2,500 | 1 | $2,500.00 |
| Rig-Down Labor (crew) | $2,500 | 1 | $2,500.00 |
| **SUBTOTAL** | | | **$6,700.00** |

### 7.12 JOB COST ANALYSIS Section

```
▼ JOB COST ANALYSIS                                              INTERNAL USE ONLY →
```

> **NEVER shown on proposal PDF.** Internal only.

**LABOR COST BREAKDOWN (FROM COST BOOK) subsection:**

Header line: `Labor Burden: 11.80% + $7.65/hr` (updates in real time as burden rates change)

Same daily grid as Labor section, but uses COST BOOK rates (what we pay), not billing rates.

**Columns:** POSITION | QTY | ST (purple) | OT (purple) | DT (purple) | SHFT | [daily grid] | ST hrs | OT hrs | DT hrs | ST$ | OT$ | DT$ | BURDEN | TOTAL

**Example rates (from estimate 26-0001-SHL, cost book rates):**

| Position | Qty | ST | OT | DT | Shift | ST hrs | OT hrs | DT hrs | Burden | Total |
|---|---|---|---|---|---|---|---|---|---|---|
| Project Manager (Day) | 1 | $65 | $98 | $130 | D | 104 | 52 | 24 | $1,764 | $16,714 |
| General Foreman (Day) | 1 | $52 | $78 | $104 | D | 104 | 52 | 24 | $1,411 | $13,371 |
| Project Manager (Night) | 1 | $65 | $98 | $130 | N | 104 | 52 | 24 | $1,764 | $16,714 |
| Pipefitter Journeyman (Day) | 1 | $42 | $63 | $84 | D | 104 | 52 | 24 | $1,140 | $10,800 |
| Pipefitter Helper (Day) | 1 | $28 | $42 | $56 | D | 104 | 52 | 24 | $760 | $7,200 |
| Welder Helper (Night) | 1 | $30 | $45 | $60 | N | 104 | 52 | 24 | $814 | $7,714 |
| Welder Journeyman (Night) | 1 | $46 | $69 | $92 | N | 104 | 52 | 24 | $1,248 | $11,828 |

Cost Subtotal: 728 | 364 | 168 | $34,112 | $25,584 | $15,744 | $8,902 | **$84,341.92**

**EXPENSE COSTS (WHAT WE PAY) subsection**

**Summary KPI Cards (4 boxes):**

| Card | Color | Value (example) |
|---|---|---|
| BILLED TO CLIENT | Blue | $148,840.00 |
| OUR ACTUAL COST | Purple | $84,341.92 |
| GROSS PROFIT | Green | $64,498.08 |
| PROFIT MARGIN | Yellow/Orange gauge | 43.3% |

> PROFIT MARGIN shown as animated gauge chart (semi-circle arc, value in orange/green)

### 7.13 GRAND TOTAL Footer Bar (Sticky)

Pinned to bottom of estimate form. Always visible.

```
LABOR        EQUIPMENT    EXPENSES     CONSUMABLES  MOB/DEMOB    DISCOUNT  DISCOUNT AMT   TAX RATE  TAX       GRAND TOTAL
$142,140.00  $0.00        $0.00        $0.00        $6,700.00    % ▼       -$0.00         0%        $0.00     $148,840.00
```

- DISCOUNT: toggle between % and $ mode
- GRAND TOTAL highlighted in teal/blue

---

## 8. Staffing Plan Library

**Screenshot label:** `staffing-plan-library.png`

### 8.1 Card Grid Layout

Each card shows:
- Plan number (e.g. SP-S-26-0004-SHELL)
- Status badge: **CONVERTED** (green) | **APPROVED** (blue) | **DRAFT** (gray)
- Job name
- Client, Location, Dates, Requested By, Rough Labor $
- Crew summary tags (e.g. "1x Project Manager (Day)", "1x Pipefitter Journeyman")
- Action buttons: **[Open]** | **[Duplicate]** | **[Delete]** | **[Convert]**

### 8.2 Staffing Plan Number Format

`SP-S-26-NNNN-CLIENTCODE`  
Example: `SP-S-26-0004-SHELL`

### 8.3 Button Behaviors

| Button | Behavior |
|---|---|
| Open | Opens the staffing plan form in edit mode |
| Duplicate | Creates a new editable copy (does NOT create an estimate) |
| Delete | Deletes the plan (only available on non-converted plans) |
| Convert | Creates a new estimate pre-populated with all plan data (editable, not locked). After conversion: button grayed out, plan shows CONVERTED badge, plan becomes read-only |

### 8.4 Filter Bar

All Status ▼ | All Branches ▼ | Quick filter (client/location/p...)

---

## 9. Staffing Plan Form

**Screenshot labels:** `staffing-plan-form-empty.png`, `staffing-plan-form-filled.png`,
`staffing-plan-crew-template-modal.png`

### 9.1 Key Differences from Estimate Form

| Aspect | Estimate Form | Staffing Plan Form |
|---|---|---|
| MODE | ESTIMATE | STAFFING PLAN |
| Number prefix | 26-NNNN-CLIENT | SP-26-NNNN-XXX |
| Equipment section | ✅ Present | ❌ Not present |
| Expenses section | ✅ Present | ❌ Not present |
| Consumables section | ✅ Present | ❌ Not present |
| MOB/DEMOB section | ✅ Present | ❌ Not present |
| Cost section name | JOB COST ANALYSIS | REVENUE & COST FORECAST |
| KPI card labels | Billed to Client / Our Actual Cost / Gross Profit / Profit Margin | Estimated Revenue / Our Actual Cost / Estimated Profit / Profit Margin |
| Footer active label | GRAND TOTAL (blue) | ESTIMATED COST (teal) |
| Toolbar save label | Save | Save Plan |
| Toolbar editing badge | Locked | **Editing** (green) |

### 9.2 Toolbar (Staffing Plan)

```
Open Plans | Save Plan | [Editing] | Upload Contract | Extract Rates | Standard Rates |
Export CSV | Export Excel | Revisions | Change Orders | Pending ▼ | Search | Settings
```

### 9.3 Status Bar (Bottom)

```
New Staffing Plan (unsaved) | Rates: Standard (Baseline)
                                              Ctrl+S: Save | Ctrl+Z: Undo | ?: Shortcuts | Bid Manager v2.0
```

Shows active rate book name — "Rates: Standard (Baseline)"

### 9.4 OT Rules Options Observed

- Daily (after 8 hrs)
- Daily 8 + Weekly 40
- Weekly 40
- (others)

### 9.5 Header Fields

Identical to estimate form header (see Section 7.3–7.6).

---

## 10. Revision History Drawer

**Screenshot label:** `revision-history-drawer.png`

### 10.1 Header

```
📋 Revision History                                                    [×]
[+ Create Revision]   [🔀 Compare]
```

### 10.2 Revision Card Layout

```
Rev 2                                          [CURRENT]
rev 2
📅 Mar 25, 2026, 07:27 PM   👥 7 labor   ⚙ 0 equip
Total: $142,140  L: $142,140  E: $0
              [👁 View]  [↺ Restore]  [🗑 Delete]
```

- Auto-generated notes: `"Converted from plan SP-S-26-0004-SHELL"` (when converted)
- Rev 1 (from conversion) starts at $0 — rates added in subsequent revisions
- CURRENT badge on the active revision
- View / Restore / Delete per revision

### 10.3 Compare Button

Opens side-by-side comparison (select 2 revisions, see row-level differences).

---

## 11. Financial Dashboard (Reports)

**Screenshot labels:** `dashboard-kpi-cards.png`, `dashboard-revenue-chart.png`,
`dashboard-winloss.png`, `dashboard-future-pipeline.png`, `dashboard-estimates-list.png`,
`dashboard-lost-bid.png`

### 11.1 Time Filter

MTD | QTD | **YTD** (default) | ALL

### 11.2 KPI Cards (Top Row)

| Card | Color | Example Value |
|---|---|---|
| AWARDED REVENUE | Green | $1.0M (10 jobs won) |
| PENDING PIPELINE | Blue | $378K (8 active bids) |
| LOST REVENUE | Red | $120K (2 jobs lost) |
| TOTAL PIPELINE | Dark | $1.5M (36 total jobs) |
| WEIGHTED PIPELINE | Orange | $1.0M (w/ 0% pending) |
| AVG MARGIN | Yellow | 68.0% (on awarded jobs) |
| FUTURE REVENUE | Purple | $968K (4 staffing plans) |
| FUTURE PROFIT | Green | $277K (28.7% avg margin) |

**Pending Weight Slider:** 0–100% slider adjusts Weighted Pipeline calculation  
**Manpower Forecast widget** (top right): "Open report →" link

### 11.3 Revenue vs Cost Analysis Chart

- Customer filter: All Customers ▼ (or specific client)
- Tabs: **Monthly** | Quarterly | Cumulative
- 4 lines: Billed (blue) | Planned (purple dashed) | Cost (red) | Profit (green)
- X-axis: months Jan–Dec
- Y-axis: dollar amounts ($0–$900K in example)

### 11.4 Win/Loss Breakdown (Right Panel)

```
10          2          8
WON       LOST     PENDING

      83%
    WIN RATE
```

### 11.5 Future Revenue Pipeline (Staffing Plans) Table

**Summary row:** Plans in Period: N | Est. Revenue: $XXX | Est. Cost: $XXX | Est. Profit: $XXX | Avg Margin: X%

**Table columns:** PLAN NUMBER | PLAN NAME | CLIENT | BRANCH | EST. REVENUE | EST. COST | EST. PROFIT | MARGIN | START DATE | END DATE | STATUS

**Example row:**
`SP-S-26-0010-BP | BP Whiting Emergency Repair | BP | — | $73,280 | $52,731.16 | $20,548.84 | 28.0% | Feb 17, 2026 | Feb 26, 2026 | APPROVED`

### 11.6 Job Estimates List (Scrollable)

**Columns:** JOB NUMBER | JOB NAME | CLIENT | BRANCH | BILLED | COST | PROFIT | MARGIN | STATUS | TENTATIVE START | DATE SUBMITTED

### 11.7 Lost Bid Analysis

- Donut chart: segments by lost reason (Not specified, Went with competitor, etc.)
- Table columns: JOB NUMBER | JOB NAME | CLIENT | VALUE | REASON | DATE
- Example rows:
  - `26-0014-SHELL | Shell Deer Park Heat Exchanger | Shell | $87,318 | Not specified | Mar 6, 2026`
  - `26-0008-VLO | Willy Wonka 34t-2 | Valero Energy | $33,040 | Went with competitor | Feb 13, 2026`

---

## 12. Manpower Forecast

**Screenshot labels:** `manpower-forecast-empty.png`, `manpower-forecast-results.png`,
`manpower-forecast-chart.png`, `manpower-forecast-tooltip.png`

### 12.1 Page Header

**Title:** Manpower Forecast  
**Subtitle:** "Current = awarded work this month. Forecast = staffing plans + active estimates in selected range."  
**Export buttons (top right):** Export CSV | Export PNG | Reports

### 12.2 Filter Bar

| Filter | Options |
|---|---|
| FROM DATE | Date picker |
| TO DATE | Date picker |
| INCLUDE | Plans + Estimates / Plans only / Estimates only |
| STATUS FILTER | All Active / specific status |
| POSITIONS | Multi-select (shows count badge like "125BC") |
| Run Forecast | Blue button — triggers calculation (not auto) |

### 12.3 KPI Cards (appear after Run Forecast)

| Card | Example |
|---|---|
| CURRENT FIELDED (WON) | 12 (24 estimate(s) \| 4 plan(s)) |
| PEAK NEED IN RANGE | 65 (Peak month: Apr 26) |
| END OF RANGE NEED | 0 (Gap vs current: -12 Oct 26) |
| PEAK GAP VS CURRENT | +53 (3 month(s) above current) |

### 12.4 Position Breakdown Table

Header: "X positions" badge | "Show All (N)" button

**Columns:** POSITION | CURRENT | [Month 1] | [Month 2] ... [Month N] | END GAP | PEAK

**Cell colors:**
- Green background = headcount needed that month
- Pink/red background = gap (needed exceeds currently fielded)
- `--` = no headcount needed

**END GAP column:** Red highlight when negative (shortage)  
**PEAK column:** "(N) MonthYY" format  
**TOTAL row:** Pinned to bottom, dark background

**Example positions (from screenshot):**

| Position | Current | Apr 26 | May 26 | Jun 26 | Jul 26 | End Gap | Peak |
|---|---|---|---|---|---|---|---|
| Boilermaker Helper | 0 | 2 | 2 | — | — | — | (2) Apr 26 |
| Boilermaker Journeyman | 0 | 2 | 2 | — | — | — | (2) Apr 26 |
| Crane Operator | 0 | 1 | 1 | — | — | — | (1) Apr 26 |
| Fire Watch | 0 | 2 | 1 | 1 | 1 | — | (2) Apr 26 |
| Foreman | 2 | 6 | 3 | 4 | 1 | -2 | (6) Apr 26 |
| General Foreman | 0 | 5 | 5 | 2 | 1 | — | (5) Apr 26 |
| NDT Technician | 0 | 1 | — | — | — | — | (1) Apr 26 |
| Pipefitter Helper | 0 | 5 | 5 | 1 | 1 | — | (5) Apr 26 |
| Pipefitter Journeyman | 0 | 4 | 4 | 2 | 1 | — | (4) Apr 26 |
| Project Manager | 2 | 11 | 7 | 6 | 1 | -2 | (11) Apr 26 |
| Safety Watch | 0 | 1 | 1 | — | — | — | (1) Apr 26 |
| Welder Helper | 0 | 2 | 3 | 2 | 1 | — | (3) May 26 |
| Welder Journeyman | 0 | 2 | 5 | 5 | 1 | — | (5) May 26 |
| **TOTAL** | **12** | **65** | **47** | **32** | **8** | **-12** | **(65) Apr 26** |

### 12.5 Headcount Over Time Chart

- Multi-line chart, one line per position + TOTAL (large blue) + Current Baseline (dashed)
- X-axis: months in selected range
- Y-axis: headcount
- Interactive tooltip on hover: shows per-position breakdown + total for that month
- Tooltip example (Jun 26): TOTAL: 32, Project Manager: 6, Helper: 4, Journeyman: 4, Foreman: 4...
- Legend below chart
- Footer text: "Analyzed N record(s) across N month(s). Peak demand: N (MonthYY) | Peak gap vs current: +N."

---

## 13. AI Assistant Sidebar

**Screenshot labels:** `ai-sidebar-chat.png`, `ai-sidebar-rates.png`, `ai-sidebar-greeting.png`

### 13.1 Layout

- **Right-side panel** — slides in/out
- **Header:** "AI Assistant" | [Clear Chat] button
- **Tabs:** CHAT | RATES
- **Input:** "Describe your estimate or use commands" text area + Send button

### 13.2 CHAT Tab — Quick Start Section

```
⚡ QUICK START
• "new estimate" - Start guided estimate creation
• "new staffing plan" - Create a staffing plan
• "estimate for BP, 14 days, 6 welders, 2 PMs" - One-liner
```

### 13.3 CHAT Tab — Rate Management Section

```
🔥 RATE MANAGEMENT
• "load BP" or "show BP rates" - Load & view client rates
• "what rates do you have?" - See all loaded rates
• "show all rate libraries" - List available clients
• "what's the foreman rate?" - Check specific rate
```

### 13.4 CHAT Tab — Build Your Estimate Section

```
🏗 BUILD YOUR ESTIMATE
• "add 4 pipefitters" - Add crew
• "add 2 foremen night shift" - Add to night crew
• "use turnaround crew" - Apply template
• "set hours 12" - Update all hours
```

### 13.5 CHAT Tab — Pro Tip

```
💡 Pro Tip: Just describe what you need naturally!
"Create estimate for Shell, start Feb 15, 21 days, day shift,
10 hours, add 3 welders and 2 fitters, use Shell rates"

Type "help" anytime to see all commands | Type "?" for quick reference
```

### 13.6 RATES Tab

**Header:** "Available Rate Libraries"  
**Subtitle:** "Click any client to load their rates:"

Clickable list of all rate books (same as Rate Library sidebar):
- BP - El Reno, OK (15 labor rates, 12 equipment rates)
- BP - Houston tx (15 labor rates, 12 equipment rates)
- BP Baytown, TX 2024 (17 labor rates, 11 equipment rates)
- BP Refinery - Bakersfield, CA
- BP Texas City, TX 2024
- BP Whiting, IN 2024
- Chevron El Segundo, CA 2024 (17 labor rates, 11 equipment rates)
- (continues...)

Clicking any entry loads that rate book into the current estimate/staffing plan instantly.

### 13.7 Toast Notification (template loaded)

```
Loaded "Electrical Crew" — 3 labor, 0 equipment added.   [×]
```

### 13.8 Rate Book Fallback Picker Feature Request

The AI RATES tab is the primary way to load a rate book.  
A **manual rate book selector** must also exist directly on the estimate/staffing plan form  
(dropdown or button) as a fallback for when the AI is unavailable or offline.

### 13.9 Context Loading (Observed in Chat History)

When an estimate is opened, AI auto-loads context:
```
Loaded 26-0001-SHELL: Shelly 25h | Client: Shell | Status: awarded | Total: $95,760
Loaded 26-0024-SHELL: Shell Deer Park Turnaround | Client: Shell | Status: submitted | Total: $142,140
Loaded SP-S-26-0007-XOM: ExxonMobil Baytown Expansion | Client: ExxonMobil | Status: approved
```

Followed by: "...and 12 more rates. View complete list in the Rates tab →"

---

## 14. Crew Templates

**Screenshot label:** `crew-template-modal.png`

### 14.1 Load Crew Template Modal

Triggered by "Load Crew" button on the estimate/staffing plan labor section.

```
🧑 Load Crew Template                                    [×]

[Boilermaker Crew]                        [6 labor] [0 equip]
1x Project Manager, 1x General Foreman, 1x Pipefitter Journeyman +3 more
Crew for pressure vessel and boiler work

[Electrical Crew]  ← selected (highlighted)             [3 labor] [0 equip]
1x General Foreman, 1x Electrician Journeyman, 1x Electrician Journeyman
Crew for electrical and instrumentation work

[Mechanical Crew]                         [6 labor] [0 equip]
1x Project Manager, 1x General Foreman, 1x Pipefitter Journeyman +3 more
Crew for mechanical equipment installation

[Shutdown Crew]                           [13 labor] [0 equip]
1x Project Manager, 1x General Foreman, 1x Foreman +10 more
Large crew for major turnaround/shutdown work

Templates can be managed in the Rate Library
                                         [Cancel]
```

### 14.2 Behavior

- Clicking a template populates labor rows with positions + headcount
- Rates are pulled from the currently active rate book (not hardcoded)
- Templates managed in: Rate Library → rate set → Templates tab
- Note at bottom: "Templates can be managed in the Rate Library"

---

## 15. Seed Dataset — Real Legacy Estimates

Use these exact records for seed data in DevController. Matches observed legacy database.

| Job Number | Job Name | Client | Status | Total | Notes |
|---|---|---|---|---|---|
| 26-0003-SHELL | Catdog 24-10h | Shell Oil Company | Submitted | $168,165 | |
| 26-0007-XOM | Rusty 16-43 | Exxon | Awarded | $71,610 | |
| 26-0009-SHELL | Shell Norco Pipe Replacement | Shell | Awarded | $99,225 | |
| 26-0010-BP | BP Baytown Unit Turnaround | BP | Awarded | $174,636 | |
| 26-0011-VLO | Valero Houston Maintenance | Valero | Pending | $52,928 | |
| 26-0012-XOM | ExxonMobil Baton Rouge Expansion | ExxonMobil | Awarded | $246,078 | |
| 26-0013-CVX | Chevron El Segundo Small Repair | Chevron | Pending | $39,690 | |
| 26-0014-SHELL | Shell Deer Park Heat Exchanger | Shell | Lost | $87,318 | Lost — Not specified |
| 26-0015-BP | BP Texas City Emergency Tie-in | BP | Submitted | $23,814 | |
| 26-0016-VLO | Valero Port Arthur Inspection Support | Valero | Pending | $99,225 | |
| 26-0017-XOM | ExxonMobil Beaumont Tank Repair | ExxonMobil | Awarded | $85,995 | |
| 26-0018-CVX | Chevron Pascagoula Piping Modification | Chevron | Pending | $125,685 | |
| 26-0020-CVX | Chevron Pascagoula Unit Tie-in | Chevron | Submitted | $55,808 | |
| 26-0021-BP | Marathon Petroleum Estimate | Marathon Petroleum | Submitted | $19,845 | |
| 26-0022-VLO | Valero Port Arthur Maintenance | Valero | Pending | $41,745 | |
| 26-0023-BP | Molasses 44-14A | Marathon Petroleum | Submitted | $33,075 | |
| 26-0024-SHELL | Shell Deer Park Turnaround | Shell | Submitted | $142,140 | |
| 26-0025-SHELL | Kangaroo 11-25h | Shell | Submitted | $47,628 | |

---

## 16. Seed Dataset — Staffing Plans

Observed from the Staffing Plan Library card grid.

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

---

*Last updated: 2026-04-24. Source: Cat-Spec Bid Manager v2.0 live screenshots.*
