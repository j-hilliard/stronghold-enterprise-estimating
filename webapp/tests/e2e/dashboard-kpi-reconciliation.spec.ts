import { test, expect } from '@playwright/test';
import { expectNoRuntimeErrors, gotoAndStabilize, startRuntimeErrorTracking } from './utils/ui';
import { API_BASE_URL, getJsonOrFail } from './utils/liveApi';
import { installMockApi } from './utils/mockApi';

const runLiveApi = process.env.PW_RUN_LIVE_API === 'true';

type IncidentDashboardSummary = {
    totalIncidents: number;
    occurredLast30Days: number;
    openInvestigations: number;
    openInvestigationsOver30Days: number;
    recordableIncidents: number;
    lostTimeIncidents: number;
    nearMisses: number;
};

type SafetyInvestigationRecord = {
    investigationId: string;
    status: string;
};

function isOpenInvestigationStatus(status: string | undefined | null) {
    const normalized = (status || '').trim().toLowerCase();
    return normalized === 'assigned' || normalized === 'in progress' || normalized === 'inprogress';
}

test.describe('Dashboard KPI: Open Investigations card navigation (mock)', () => {
    test.describe.configure({ timeout: 60_000 });

    test('clicking Open Investigations card navigates with status=Open query param', async ({ page }) => {
        const runtimeErrors = startRuntimeErrorTracking(page);

        await installMockApi(page);

        await page.route('**/v1/Incident/register-summary', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    totalIncidents: 12,
                    occurredLast30Days: 3,
                    openInvestigations: 4,
                    openInvestigationsOver30Days: 1,
                    recordableIncidents: 2,
                    lostTimeIncidents: 1,
                    nearMisses: 1,
                }),
            });
        });

        await page.route('**/v1/Incident/register-trend', async (route) => {
            await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
        });

        await page.route('**/v1/Investigation**', async (route) => {
            await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
        });

        await gotoAndStabilize(page, '/safety-application');
        await expect(page.getByText('Open Investigations').first()).toBeVisible();

        await page.locator('a:has-text("Open Investigations")').click();
        await page.waitForURL(/\/safety-application\/investigations/);

        const url = new URL(page.url());
        expect(url.searchParams.get('status')).toBe('Open');

        expectNoRuntimeErrors(runtimeErrors);
    });
});

test.describe('Dashboard KPI vs Investigations page reconciliation (Live DB-backed)', () => {
    test.skip(!runLiveApi, 'Set PW_RUN_LIVE_API=true for authenticated live KPI reconciliation checks.');
    test.describe.configure({ timeout: 120_000 });

    test('dashboard openInvestigations count equals Assigned+In Progress count from investigation list API', async ({ request }) => {
        const summary = await getJsonOrFail<IncidentDashboardSummary>(
            request,
            `${API_BASE_URL}/v1/Incident/register-summary`,
        );

        const investigations = await getJsonOrFail<SafetyInvestigationRecord[]>(
            request,
            `${API_BASE_URL}/v1/Investigation`,
        );

        const openLikeCount = investigations.filter(inv => isOpenInvestigationStatus(inv.status)).length;
        expect(summary.openInvestigations).toBe(openLikeCount);
    });

    test('Open Investigations card click applies status=Open and all visible rows are Assigned or In Progress', async ({ page }) => {
        const runtimeErrors = startRuntimeErrorTracking(page);

        await gotoAndStabilize(page, '/safety-application');
        await expect(page.locator('a:has-text("Open Investigations")')).toBeVisible();

        await page.locator('a:has-text("Open Investigations")').click();
        await page.waitForURL(/\/safety-application\/investigations/);

        const url = new URL(page.url());
        expect(url.searchParams.get('status')).toBe('Open');

        const rows = page.locator('.p-datatable-tbody > tr');
        const rowCount = await rows.count();

        for (let i = 0; i < rowCount; i += 1) {
            const tds = rows.nth(i).locator('td');
            const tdCount = await tds.count();
            if (tdCount < 8) continue;

            const statusText = (await tds.nth(5).innerText()).trim();
            expect(isOpenInvestigationStatus(statusText)).toBeTruthy();
        }

        expectNoRuntimeErrors(runtimeErrors);
    });

    test('Open filter KPI counts reconcile with summary API', async ({ page, request }) => {
        const runtimeErrors = startRuntimeErrorTracking(page);

        const summary = await getJsonOrFail<IncidentDashboardSummary>(
            request,
            `${API_BASE_URL}/v1/Incident/register-summary`,
        );

        await gotoAndStabilize(page, '/safety-application/investigations?status=Open');

        const visibleCountText = await page
            .locator('div:has(> p:has-text("Visible Investigations"))')
            .first()
            .locator('p')
            .nth(1)
            .textContent();
        const visibleCount = parseInt(visibleCountText?.trim() ?? '-1', 10);

        const openKpiText = await page
            .locator('p', { hasText: 'Investigations with Open status across all data.' })
            .locator('..')
            .locator('p.text-3xl')
            .textContent();
        const pageOpenCount = parseInt(openKpiText?.trim() ?? '-1', 10);

        expect(pageOpenCount).toBe(summary.openInvestigations);
        expect(visibleCount).toBe(summary.openInvestigations);

        expectNoRuntimeErrors(runtimeErrors);
    });

    test('Open Investigations KPI card value is stable regardless of status filter', async ({ page }) => {
        const runtimeErrors = startRuntimeErrorTracking(page);

        await gotoAndStabilize(page, '/safety-application/investigations');

        const getOpenKpiCount = async () => {
            const text = await page
                .locator('div:has(> p:has-text("Open Investigations"))')
                .first()
                .locator('p')
                .nth(1)
                .textContent();
            return parseInt(text?.trim() ?? '-1', 10);
        };

        const baseCount = await getOpenKpiCount();
        expect(baseCount).toBeGreaterThanOrEqual(0);

        await gotoAndStabilize(page, '/safety-application/investigations?status=Completed');
        expect(await getOpenKpiCount()).toBe(baseCount);

        await gotoAndStabilize(page, '/safety-application/investigations?status=Closed');
        expect(await getOpenKpiCount()).toBe(baseCount);

        expectNoRuntimeErrors(runtimeErrors);
    });
});
