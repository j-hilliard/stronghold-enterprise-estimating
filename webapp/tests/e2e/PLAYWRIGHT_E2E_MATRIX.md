# Playwright E2E Requirements Traceability Matrix

Generated: 2026-03-27
Standard: `REQ-GOV-003` — Each test file traces to requirement IDs.
Standard: `REQ-CC-003` — Traceability map links Requirement → Implementation → Tests → Evidence.

---

## Test Suite Overview

| Suite | File | Type | Project |
|---|---|---|---|
| Smoke | `smoke.spec.ts` | Mocked | chromium-mocked |
| Cost Book | `cost-book.spec.ts` | Mocked/Live | chromium-mocked |
| AI Chat / NLP | `ai-fab.spec.ts` | Mocked | chromium-mocked |
| Staffing Workflow | `staffing-workflow.spec.ts` | Mocked | chromium-mocked |
| Analytics & Rate Books | `estimating-analytics-ratebook.spec.ts` | Mocked | chromium-mocked |
| Visual Regression | `theme-visual.spec.ts` | Mocked | chromium-mocked |
| Live — Estimates | `live-estimates.spec.ts` | Live | chromium-live |
| Live — Staffing | `live-staffing.spec.ts` | Live | chromium-live |
| Live — Rate Books | `live-ratebook.spec.ts` | Live | chromium-live |

---

## Requirements Coverage

| Requirement ID | Description | Covered By | Test ID(s) |
|---|---|---|---|
| REQ-EST-001 | Create estimate from blank | smoke.spec.ts, live-estimates.spec.ts | `[REQ-EST-001]` |
| REQ-EST-005 | Save/reopen preserves all rows and totals | live-estimates.spec.ts | `[REQ-EST-005]` |
| REQ-EST-006 | Estimate log with search/filter | smoke.spec.ts, live-estimates.spec.ts | `[REQ-EST-006]` |
| REQ-EST-010 | Revision create/compare/restore | live-estimates.spec.ts | `[REQ-EST-010]` |
| REQ-EST-011 | Sections are collapsible without data loss | smoke.spec.ts | `[REQ-EST-001, REQ-EST-011]` |
| REQ-STAT-001 | Estimate statuses: draft/proposed/awarded/lost/archived | live-estimates.spec.ts | `[REQ-STAT-001]` |
| REQ-STAT-002 | Staffing statuses: draft/active/converted/archived | staffing-workflow.spec.ts | `[REQ-SP-001, REQ-STAT-002]` |
| REQ-STAT-003 | Lost requires reason code | live-estimates.spec.ts | `[REQ-STAT-001, REQ-STAT-003]` |
| REQ-STAT-004 | Convert updates staffing status to converted | staffing-workflow.spec.ts, live-staffing.spec.ts | `[REQ-STAT-004]` |
| REQ-SP-001 | Create staffing plans | staffing-workflow.spec.ts, live-staffing.spec.ts | `[REQ-SP-001]` |
| REQ-SP-004 | Convert staffing plan to estimate | staffing-workflow.spec.ts, live-staffing.spec.ts | `[REQ-SP-004]` |
| REQ-SP-005 | Converted estimate stores staffing plan reference | staffing-workflow.spec.ts, live-staffing.spec.ts | `[REQ-SP-005]` |
| REQ-SP-006 | Converted staffing deduped in forecast | live-staffing.spec.ts | `[REQ-SP-006]` |
| REQ-SP-007 | Toggle between estimate/staffing modes | staffing-workflow.spec.ts | `[REQ-SP-004, REQ-SP-005, REQ-SP-006]` |
| REQ-RATE-001 | Rate books are customer/location specific | estimating-analytics-ratebook.spec.ts, live-ratebook.spec.ts | `[REQ-RATE-001]` |
| REQ-RATE-002 | Exact customer/location match loads rates | estimating-analytics-ratebook.spec.ts, live-ratebook.spec.ts | `[REQ-RATE-002]` |
| REQ-RATE-003 | No exact match — nearest candidates suggested | estimating-analytics-ratebook.spec.ts, live-ratebook.spec.ts | `[REQ-RATE-003]` |
| REQ-RATE-004 | User can clone nearest rate book | estimating-analytics-ratebook.spec.ts, live-ratebook.spec.ts | `[REQ-RATE-004]` |
| REQ-COST-001 | Cost book includes all four sections | cost-book.spec.ts | `[REQ-COST-001]` |
| REQ-COST-002 | Burden supports mixed inputs | cost-book.spec.ts | `[REQ-COST-002]` |
| REQ-COST-003 | Save All disabled when no changes | cost-book.spec.ts | `[REQ-COST-003]` |
| REQ-COST-004 | Reset Defaults opens confirm dialog | cost-book.spec.ts | `[REQ-COST-004]` |
| REQ-COST-005 | Add Position dialog opens and closes | cost-book.spec.ts | `[REQ-COST-005]` |
| REQ-AI-001 | NL create/update for staffing and estimates | ai-fab.spec.ts | `[REQ-AI-001]` |
| REQ-AI-002 | Agent asks clarifying questions before ambiguous writes | ai-fab.spec.ts | `[REQ-AI-002, REQ-AI-011]` |
| REQ-AI-003 | Agent handles misspellings and varied phrasing | ai-fab.spec.ts | `[REQ-AI-003, REQ-QA-004]` |
| REQ-AI-004 | Agent normalizes location patterns | ai-fab.spec.ts | `[REQ-AI-004, REQ-QA-004]` |
| REQ-AI-009 | Agent answers data-grounded questions | ai-fab.spec.ts | `[REQ-AI-009, REQ-QA-004]` |
| REQ-AI-011 | Irreversible actions require explicit confirmation | ai-fab.spec.ts | `[REQ-AI-002, REQ-AI-011]` |
| REQ-AN-001 | Revenue forecast composition | estimating-analytics-ratebook.spec.ts | `[REQ-AN-001]` |
| REQ-AN-002 | Dashboard time filters | estimating-analytics-ratebook.spec.ts | `[REQ-AN-002]` |
| REQ-MF-002 | Future demand by month/craft/position | estimating-analytics-ratebook.spec.ts | `[REQ-MF-002]` |
| REQ-MF-004 | Peak month and peak craft demand | estimating-analytics-ratebook.spec.ts | `[REQ-MF-004]` |
| REQ-OBJ-002 | Enterprise template UX consistency | theme-visual.spec.ts | `[REQ-OBJ-002, REQ-QA-003]` |
| REQ-QA-003 | Visual regression coverage | theme-visual.spec.ts | `[REQ-OBJ-002, REQ-QA-003]` |
| REQ-QA-004 | AI test suite includes high-variance NLP prompts | ai-fab.spec.ts | `[REQ-QA-004]` |

---

## Open Gaps

| Requirement ID | Description | Gap | Planned Test File |
|---|---|---|---|
| REQ-STAT-001 | Status vocabulary: `proposed` and `archived` | Impl uses `Pending`/`Canceled` — see AMD-20260327-001 | live-estimates.spec.ts (sentinel) |
| REQ-EST-009 | Won/lost affects downstream reporting | Lost reason dialog — UI exists, E2E not yet exercised | live-estimates.spec.ts |
| REQ-COST-006 | Cost assumption changes are revision-traceable | Not yet tested end-to-end | cost-book.spec.ts |
| REQ-SEC-001 | Company-scoped read/write | No isolation test yet | live-security.spec.ts (future) |
| REQ-SCEN-002 | Scenario badge visible in estimate list | CloneAsScenario frontend work exists, no live E2E | live-estimates.spec.ts |
| REQ-PROP-001 | Proposal PDF generation | Export button exists, no download E2E | live-estimates.spec.ts |

---

## Run Commands

```bash
# Mocked suite (no API required)
npm run test:e2e

# Live suite (requires API + Docker SQL)
npm run test:e2e:live

# All suites
npm run test:e2e:all

# Update visual baselines
npm run test:e2e:update-snapshots
```

---

## Status Vocabulary Amendment (AMD-20260327-001)

REQ-STAT-001 specifies: `draft, proposed, awarded, lost, archived`
Implementation uses: `Draft, Pending, Awarded, Lost, Canceled`

**Gap**: `proposed` → `Pending`, `archived` → `Canceled`
**Approach**: Tests assert against actual implementation values until vocabulary is corrected.
Sentinel test in `live-estimates.spec.ts` will fail when vocabulary is aligned — that is intentional.
See amendment log in `MASTER_REQUIREMENTS_POC.md` §28.
