# QA Evidence - Light Mode Audit Failure

Date: 2026-04-26

This file is a handoff for Claude or any QA agent. Do not treat this as permission to edit source. It documents a defect Joseph found and Codex confirmed with screenshots.

## Screenshot Folder

- `docs/qa-evidence/QA_LIGHT_MODE_20260426/crew-templates-light.png`
- `docs/qa-evidence/QA_LIGHT_MODE_20260426/new-estimate-light.png`

## Finding - Light Mode Was Not Audited And Has Obvious Contrast Failures

Status: FAIL / OPEN

What is wrong:

Light mode is visibly broken in multiple places:

- Page titles such as `Crew Templates` and `New Estimate` render white or near-white on a pale blue background.
- Disabled buttons such as `Review with AI` are barely visible.
- Low-priority action links/buttons such as `Back`, `Load Crew`, and `Load Rate Book` are washed out.
- Form/header controls look uneven because some components use dark-theme utility classes or opacity values that do not translate to light mode.

Why it is wrong:

The theme toggle is visible in the top bar. If a user, manager, or demo observer clicks it, the app should remain professional and readable. A full UI audit must include both themes. Treating dark mode as the only tested mode misses an entire user-visible state.

Likely files:

- `webapp/src/assets/css/style.css`
- `webapp/src/assets/css/theme.css`
- `webapp/src/components/layout/TheTopBar.vue`
- `webapp/src/components/layout/BasePageHeader.vue`
- Feature views/components using hardcoded `text-white`, `text-slate-*`, pale button variants, or dark-only surface assumptions.

How to fix:

1. Define light-mode design tokens for page title, subtitle, content text, muted text, borders, cards, inputs, section headers, buttons, disabled buttons, and icon buttons.
2. Override or remove hardcoded dark-theme utilities in light mode:
   - `text-white`
   - `text-slate-300`
   - `text-slate-400`
   - very low opacity button/link states
3. Make disabled buttons visibly disabled but still readable.
4. Verify all primary pages in both themes after the changes.

Regression test:

Run `QA-UI-008` and `QA-UI-009` from `docs/QA_REGRESSION_CHECKLIST.md`.

Minimum required screenshot set for closure:

- Estimates list: dark + light
- New Estimate: dark + light
- Staffing Plans list: dark + light
- Staffing Plan form: dark + light
- Rate Books: dark + light
- Cost Book: dark + light
- Crew Templates: dark + light
- Revenue Forecast: dark + light
- Manpower Forecast: dark + light
- AI sidebar open: dark + light

Do not close this item from code inspection only. It requires screenshots.
