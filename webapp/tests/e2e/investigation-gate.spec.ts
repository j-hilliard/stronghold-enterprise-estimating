import { test, expect } from './fixtures';
import { expectNoRuntimeErrors, gotoAndStabilize } from './utils/ui';

const FORMAL_INVESTIGATION_ID = 'li-4';
const FULL_CAUSE_MAP_ID = 'li-5';
const INCIDENT_REPORT_ID = 'li-6';

test.describe('Investigation gate logic', () => {
    test.describe.configure({ timeout: 60_000 });

    test('Begin Investigation is hidden for Incident Report-only incidents', async ({ page, runtimeErrors, mockApi }) => {
        mockApi.incidents.push({
            id: 'inc-gate-report',
            incidentNumber: 'CCI-2026-GATE2',
            incidentDate: '2026-01-15T00:00:00.000Z',
            companyId: 'co-1',
            companyName: 'Central Construction Inc',
            regionId: 're-1',
            regionName: 'Southwest',
            incidentClass: 'Actual',
            status: 'Open',
            referenceIds: [INCIDENT_REPORT_ID],
            employeesInvolved: [],
            actions: [],
        });

        await gotoAndStabilize(page, '/safety-application/incidents/inc-gate-report');

        await expect(page.getByRole('button', { name: 'Begin Investigation' })).not.toBeVisible();
        await expect(page.getByRole('button', { name: 'Save Changes' })).toBeVisible();

        expectNoRuntimeErrors(runtimeErrors);
    });

    test('Begin Investigation is visible when Formal Investigation is required', async ({ page, runtimeErrors, mockApi }) => {
        mockApi.incidents.push({
            id: 'inc-gate-actual',
            incidentNumber: 'CCI-2026-GATE1',
            incidentDate: '2026-01-15T00:00:00.000Z',
            companyId: 'co-1',
            companyName: 'Central Construction Inc',
            regionId: 're-1',
            regionName: 'Southwest',
            incidentClass: 'Actual',
            status: 'Open',
            referenceIds: [FORMAL_INVESTIGATION_ID],
            employeesInvolved: [],
            actions: [],
        });

        await gotoAndStabilize(page, '/safety-application/incidents/inc-gate-actual');

        await expect(page.getByRole('button', { name: 'Begin Investigation' })).toBeVisible();

        expectNoRuntimeErrors(runtimeErrors);
    });

    test('Submit and Start Investigation is hidden when Incident Report is selected', async ({ page, runtimeErrors }) => {
        await gotoAndStabilize(page, '/safety-application/incidents/new');

        // PrimeVue hides the native checkbox input — click the visible label instead
        await page.locator('label').filter({ hasText: /^Incident Report$/ }).click();
        await expect(page.getByRole('button', { name: 'Submit & Start Investigation' })).not.toBeVisible();
        await expect(page.getByRole('button', { name: 'Submit', exact: true })).toBeVisible();

        expectNoRuntimeErrors(runtimeErrors);
    });

    test('Submit and Start Investigation appears for Formal Investigation or Full Cause Map', async ({ page, runtimeErrors }) => {
        await gotoAndStabilize(page, '/safety-application/incidents/new');

        // PrimeVue hides the native checkbox input — click the visible label to toggle
        await page.locator('label').filter({ hasText: /^Incident Report$/ }).click();
        await expect(page.getByRole('button', { name: 'Submit & Start Investigation' })).not.toBeVisible();

        await page.locator('label').filter({ hasText: /^Formal Investigation$/ }).click();
        await expect(page.getByRole('button', { name: 'Submit & Start Investigation' })).toBeVisible();

        await page.locator('label').filter({ hasText: /^Formal Investigation$/ }).click(); // toggle off
        await expect(page.getByRole('button', { name: 'Submit & Start Investigation' })).not.toBeVisible();

        await page.locator('label').filter({ hasText: /^Full Cause Map$/ }).click();
        await expect(page.getByRole('button', { name: 'Submit & Start Investigation' })).toBeVisible();

        expectNoRuntimeErrors(runtimeErrors);
    });

    test('Incident Report checkbox renders with a real reference ID, not the legacy fake ID', async ({ page, runtimeErrors }) => {
        await gotoAndStabilize(page, '/safety-application/incidents/new');

        // The Incident Report label must come from CheckboxGroup (DB-sourced), not the
        // removed hardcoded fake element. The label is visible; the native input is hidden by PrimeVue.
        const label = page.locator('label').filter({ hasText: /^Incident Report$/ });
        await expect(label).toBeVisible();

        // Get the associated input's value via the for attribute
        const forId = await label.getAttribute('for');
        expect(forId).toBeTruthy();
        const value = await page.locator(`#${forId}`).evaluate((el: HTMLInputElement) => el.value);
        expect(value).not.toBe('inv-incident-report');
        expect(value.length).toBeGreaterThan(0);

        expectNoRuntimeErrors(runtimeErrors);
    });
});

