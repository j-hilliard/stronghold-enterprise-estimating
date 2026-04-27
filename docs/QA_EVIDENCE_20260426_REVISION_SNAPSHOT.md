# QA Evidence - Revision Snapshot Mismatch

Date: 2026-04-26

This file is a handoff for Claude or any QA agent. Do not treat this as permission to edit source. It documents a defect Joseph found and Codex traced in code.

## Evidence

Joseph provided screenshots in chat showing:

- Visible estimate Labor section: `8 rows`.
- Visible Summary / Grand Total: `$291,378`.
- Revision History current Rev 2 card: `6 labor`.
- Revision History current Rev 2 card total: `$240,093`.
- Rev 1 and Rev 2 both show the same old total even though the visible estimate revenue changed.

No local screenshot file exists for Joseph's original browser screenshots, so this issue must be re-captured in the next UI audit using the required `docs/qa-evidence/<sweep-id>/` packet format.

## Finding - Revision Workflow Is Too Complex And Can Snapshot Stale Database State

Status: FAIL / OPEN

Product decision:

Joseph wants this simplified. The target workflow is not two different revision/snapshot paths. It should be one simple `Create Revision` action:

1. Create the next sequential editable revision copy of the estimate.
2. Open that revision for editing.
3. Save normally into that revision number.
4. Allow deliberate revert/restore to a prior revision.

Put differently: a revision should behave like an editable version, not like a hidden one-time snapshot note. The user should not have to understand two buttons or two different save paths.

What is wrong:

Creating a revision can produce a Revision History card whose labor count and totals do not match the estimate currently visible on screen.

Why it is wrong:

Revision History is supposed to be an audit trail. If it says the current revision has 6 labor rows and `$240,093`, while the actual estimate has 8 labor rows and `$291,378`, users cannot trust revisions, compare, restore, or revenue deltas.

Likely cause:

There are two revision creation paths:

1. `Save + Revision` in `EstimateFormView.vue`
   - calls `store.saveEstimate(revisionNote)`
   - `estimateStore.saveEstimate()` saves header, labor, equipment, expenses, and summary
   - then it posts the revision snapshot

2. `+ Create Revision` inside `RevisionDrawer.vue`
   - directly posts `/api/v1/estimates/{estimateId}/revisions`
   - does not save current dirty form state first
   - backend snapshots whatever is already in SQL

So if the visible form has unsaved changes, the drawer creates a revision from stale DB rows/totals. Saving afterward updates the estimate, but it does not update the already-created revision.

Likely files:

- `webapp/src/modules/estimating/features/estimate-form/components/RevisionDrawer.vue`
- `webapp/src/modules/estimating/features/estimate-form/views/EstimateFormView.vue`
- `webapp/src/modules/estimating/stores/estimateStore.ts`
- `Api/Controllers/RevisionsController.cs`

How to fix:

Replace the current mixed workflow with one consistent revision model:

- Remove/hide duplicate revision entry points such as toolbar `Save + Revision` plus drawer `+ Create Revision`.
- Keep one visible `Create Revision` action.
- When clicked, create the next sequential revision copy from the current estimate/revision and open it for editing.
- When the user saves, save into that revision number.
- Revision History should show each saved revision with its own row counts and totals.
- Revert/restore should deliberately switch/copy back to that prior revision.
- Do not mark complete if `Create Revision` only creates a stale DB snapshot card and does not open an editable sequential revision.

If a smaller interim fix is needed before a larger model change, the drawer must at minimum save first or block dirty snapshots. But the preferred product fix is the simplified editable revision copy workflow above.

Regression test:

Run `QA-EST-015` and `QA-EST-016` from `docs/QA_REGRESSION_CHECKLIST.md`.

Minimum closure evidence:

- Screenshot before Rev 2 creation showing `8 rows` and changed Grand Total.
- Screenshot after Rev 2 creation showing Rev 2 card with `8 labor` and matching Grand Total.
- Screenshot after browser refresh showing Rev 2 still has the same values.
- Compare screenshot showing the delta from Rev 1 to Rev 2.

Do not close this from code inspection only. It requires screenshots.
