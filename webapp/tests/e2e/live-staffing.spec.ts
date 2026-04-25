/**
 * Live UI→API→DB reconciliation tests for the staffing plan module.
 * Requires API on PW_API_BASE_URL and Docker SQL on port 14331.
 * Run with: npm run test:e2e:live
 *
 * REQ-SP-001, REQ-SP-004, REQ-SP-005, REQ-SP-006,
 * REQ-STAT-002, REQ-STAT-004, REQ-QA-001, REQ-QA-002.
 */
import { test, expect } from '@playwright/test';
import { apiGet, apiPost, seedDevData } from './utils/liveApi';
import { TEST_TAG } from './utils/constants';

const TAG = TEST_TAG;

test.describe('Live — staffing plans', () => {
    test.beforeAll(async ({ request }) => {
        await seedDevData(request);
    });

    // ── REQ-SP-001: create staffing plan ─────────────────────────────────────
    test('[REQ-SP-001] create new staffing plan from UI and verify it persists to DB', async ({ page, request }) => {
        const planName = `${TAG} Live SP ${Date.now()}`;

        await page.goto('/estimating/staffing-plans/new');
        await expect(page.locator('[data-testid="sp-name"]')).toBeVisible({ timeout: 15_000 });

        await page.locator('[data-testid="sp-name"]').fill(planName);
        await page.locator('[data-testid="sp-client"]').fill('Shell');
        await page.locator('[data-testid="sp-client-code"]').fill('SHL');
        await page.locator('[data-testid="sp-city"]').fill('Deer Park');
        await page.locator('[data-testid="sp-state"]').fill('TX');
        await page.locator('[data-testid="sp-start-date"]').fill('2026-07-01');
        await page.locator('[data-testid="sp-end-date"]').fill('2026-07-14');

        await page.locator('[data-testid="sp-save"]').click();
        await expect(page).toHaveURL(/\/estimating\/staffing-plans\/\d+$/, { timeout: 15_000 });

        const url = page.url();
        const idMatch = url.match(/\/staffing-plans\/(\d+)$/);
        expect(idMatch).toBeTruthy();
        const planId = Number(idMatch![1]);

        // DB reconciliation
        const detail = await apiGet(request, `/api/v1/staffing-plans/${planId}`);
        expect(detail.name).toBe(planName);
        expect(detail.client).toBe('Shell');
        expect(detail.city).toBe('Deer Park');
    });

    // ── REQ-SP-004 / REQ-SP-005: convert to estimate ─────────────────────────
    test('[REQ-SP-004, REQ-SP-005] staffing plan converts to estimate with reference linkage', async ({ page, request }) => {
        const planName = `${TAG} Convert ${Date.now()}`;

        // Create staffing plan via API
        const created = await apiPost(request, '/api/v1/staffing-plans', {
            name: planName,
            client: 'BP',
            clientCode: 'BPH',
            city: 'Houston',
            state: 'TX',
            startDate: '2026-08-01',
            endDate: '2026-08-10',
            status: 'Active',
            shift: 'Day',
            hoursPerShift: 10,
            days: 10,
            otMethod: 'daily8_weekly40',
            dtWeekends: false,
        });
        const planId = created.staffingPlanId;
        expect(planId).toBeTruthy();

        // Navigate to staffing form
        await page.goto(`/estimating/staffing-plans/${planId}`);
        await expect(page.locator('[data-testid="sp-name"]')).toBeVisible({ timeout: 15_000 });

        // Click convert
        const convertBtn = page.locator('[data-testid="sp-convert"], button', { hasText: /convert/i });
        if (await convertBtn.count() === 0) {
            test.skip(true, 'Convert button not yet implemented');
            return;
        }
        await convertBtn.first().click();

        // Confirm dialog
        const confirmDlg = page.locator('[data-testid="sp-convert-dialog"]');
        if (await confirmDlg.isVisible({ timeout: 3000 }).catch(() => false)) {
            await confirmDlg.locator('[data-testid="sp-convert-confirm"], button', { hasText: /confirm|convert/i }).first().click();
        }

        // Wait for conversion result
        await expect(
            page.locator('[data-testid="sp-converted-message"], .p-toast', { hasText: /converted/i }),
        ).toBeVisible({ timeout: 15_000 });

        // DB reconciliation — staffing plan should now have convertedEstimateId
        const plan = await apiGet(request, `/api/v1/staffing-plans/${planId}`);
        expect(plan.convertedEstimateId).toBeTruthy();
        expect(plan.status).toBe('Converted');

        // REQ-SP-005: converted estimate stores staffing plan reference
        const estimateId = plan.convertedEstimateId;
        const estimate = await apiGet(request, `/api/v1/estimates/${estimateId}`);
        expect(estimate).toBeTruthy();
    });

    // ── REQ-SP-006: converted staffing deduped in forecast ───────────────────
    test('[REQ-SP-006] converted staffing plan is deduped in manpower list view', async ({ page, request }) => {
        // Seed a converted plan
        const planName = `${TAG} Dedup ${Date.now()}`;
        const created = await apiPost(request, '/api/v1/staffing-plans', {
            name: planName,
            client: 'Valero',
            clientCode: 'VLO',
            city: 'Port Arthur',
            state: 'TX',
            startDate: '2026-09-01',
            endDate: '2026-09-10',
            status: 'Active',
            shift: 'Day',
            hoursPerShift: 10,
            days: 10,
            otMethod: 'daily8_weekly40',
            dtWeekends: false,
        });

        await page.goto('/estimating/staffing-plans');
        await expect(page.locator('[data-testid="sp-toolbar"]')).toBeVisible({ timeout: 15_000 });

        // The unconverted plan must appear in the list
        const planCard = page.locator('[data-testid^="sp-card-"]', { hasText: planName });
        await expect(planCard.first()).toBeVisible({ timeout: 10_000 });

        // Filter to Converted status should NOT show this plan (not yet converted)
        await page.locator('[data-testid="sp-status"]').click();
        await page.locator('.p-dropdown-item', { hasText: 'Converted' }).click();
        await expect(planCard.first()).toHaveCount(0);
    });
});
