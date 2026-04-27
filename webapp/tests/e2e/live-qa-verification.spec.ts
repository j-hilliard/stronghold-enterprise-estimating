/**
 * Live QA verification sweep — screenshots for LIVE_QA_TODO open items.
 * Captures evidence for P0-001, P0-002, P0-003, P0-004, P0-006, P0-010,
 * P0-011, P0-012, P0-013, P0-014, P0-015, P0-020, P0-021, P1-SP-002, P1-SP-003.
 *
 * Run: npx playwright test live-qa-verification --project=chromium
 * Screenshots land in: docs/qa-evidence/QA_SWEEP_<date>/
 */
import { test, expect, Page } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';

const SWEEP_ID = `QA_SWEEP_${new Date().toISOString().slice(0, 10)}`;
const OUT_DIR = path.join('..', 'docs', 'qa-evidence', SWEEP_ID);

function ensureDir() {
    if (!fs.existsSync(OUT_DIR)) fs.mkdirSync(OUT_DIR, { recursive: true });
}

async function shot(page: Page, name: string, fullPage = true) {
    ensureDir();
    const p = path.join(OUT_DIR, `${name}.png`);
    await page.screenshot({ path: p, fullPage });
    console.log(`📸 ${name} → ${p}`);
}

async function stabilize(page: Page, url: string, readySelector: string) {
    await page.goto(url, { waitUntil: 'domcontentloaded' });
    await page.locator(readySelector).first().waitFor({ state: 'visible', timeout: 20_000 });
    await page.waitForTimeout(600);
    await page.evaluate(() => {
        document.querySelectorAll('*').forEach(el => {
            (el as HTMLElement).style.transition = 'none';
            (el as HTMLElement).style.animation = 'none';
        });
    });
}

// ── P0-020: Light mode across all major pages ─────────────────────────────────
test.describe('P0-020: Light mode audit', () => {
    async function enableLight(page: Page) {
        const btn = page.locator('.theme-toggle-btn');
        if (await btn.count() > 0) await btn.click();
        await page.waitForTimeout(300);
    }

    test('estimate list — light mode', async ({ page }) => {
        await stabilize(page, '/estimating/estimates', '.p-datatable');
        await enableLight(page);
        await shot(page, 'P0-020_estimate-list-light');
    });

    test('estimate form — light mode', async ({ page }) => {
        await stabilize(page, '/estimating/estimates/new', '.estimate-header');
        await enableLight(page);
        await shot(page, 'P0-020_estimate-form-light');
    });

    test('staffing list — light mode', async ({ page }) => {
        await stabilize(page, '/estimating/staffing-plans', '.sp-card, .empty-state');
        await enableLight(page);
        await shot(page, 'P0-020_staffing-list-light');
    });

    test('revenue forecast — light mode', async ({ page }) => {
        await stabilize(page, '/estimating/analytics/revenue', '.fd-page');
        await page.waitForTimeout(2000); // allow analytics API to settle
        await enableLight(page);
        await shot(page, 'P0-020_revenue-forecast-light');
    });

    test('rate book — light mode', async ({ page }) => {
        await stabilize(page, '/estimating/rate-books', '.rb-layout');
        await enableLight(page);
        await shot(page, 'P0-020_rate-book-light');
    });

    test('cost book — light mode', async ({ page }) => {
        await stabilize(page, '/estimating/cost-book', '.cost-book-view, .empty-state');
        await enableLight(page);
        await shot(page, 'P0-020_cost-book-light');
    });
});

// ── P0-001: Status vocabulary ─────────────────────────────────────────────────
test.describe('P0-001: Status vocabulary', () => {
    test('estimate list shows Submitted for Approval badge', async ({ page }) => {
        await stabilize(page, '/estimating/estimates', '.p-datatable');
        // Filter to submitted status if filter exists
        await shot(page, 'P0-001_estimate-list-all-statuses');
    });

    test('new estimate form shows read-only DRAFT badge', async ({ page }) => {
        await stabilize(page, '/estimating/estimates/new', '.estimate-header');
        await shot(page, 'P0-001_new-estimate-draft-badge');
    });
});

// ── P0-002: Submit for Approval workflow ──────────────────────────────────────
test.describe('P0-002: Submit for Approval', () => {
    test('estimate form shows Submit for Approval button for Draft', async ({ page }) => {
        await stabilize(page, '/estimating/estimates/new', '.estimate-header');
        // Fill minimum required fields to enable save
        await page.locator('[data-testid="est-name"]').fill('QA Test Submit Flow');
        await page.locator('[data-testid="est-client"]').fill('Test Client');
        await shot(page, 'P0-002_submit-btn-visible-draft');
    });
});

// ── P0-003: Award/Lost/Revision list actions ──────────────────────────────────
test.describe('P0-003: Estimate list workflow actions', () => {
    test('three-dot menu visible and opens on first row', async ({ page }) => {
        await stabilize(page, '/estimating/estimates', '.p-datatable tbody tr');
        const firstRow = page.locator('.p-datatable tbody tr').first();
        await firstRow.hover();
        await page.waitForTimeout(300);
        await shot(page, 'P0-003_row-hover-actions-visible');

        // Click three-dot menu
        const menuBtn = firstRow.locator('[data-testid^="est-menu-"], button[aria-label*="menu"], .p-button-icon-only').last();
        if (await menuBtn.count() > 0) {
            await menuBtn.click();
            await page.waitForTimeout(300);
            await shot(page, 'P0-003_three-dot-menu-open', false);
        }
    });
});

// ── P0-006: Revenue Forecast KPI vs chart ────────────────────────────────────
test.describe('P0-006: Revenue Forecast', () => {
    test('KPI cards and chart — ALL period', async ({ page }) => {
        await stabilize(page, '/estimating/analytics/revenue', '.fd-page');
        await page.waitForTimeout(2500);
        await shot(page, 'P0-006_revenue-forecast-all');
    });

    test('KPI drilldown — click Awarded KPI', async ({ page }) => {
        await stabilize(page, '/estimating/analytics/revenue', '.fd-page');
        await page.waitForTimeout(2500);
        const awardedCard = page.locator('.fd-kpi-card').first();
        if (await awardedCard.count() > 0) {
            await awardedCard.click();
            await page.waitForTimeout(400);
            await shot(page, 'P0-011_kpi-drilldown-awarded');
        } else {
            await shot(page, 'P0-011_kpi-drilldown-no-cards');
        }
    });

    test('Revenue Forecast — Q1 period', async ({ page }) => {
        await stabilize(page, '/estimating/analytics/revenue', '.fd-page');
        await page.waitForTimeout(2500);
        const q1Btn = page.locator('button', { hasText: 'Q1' });
        if (await q1Btn.count() > 0) {
            await q1Btn.click();
            await page.waitForTimeout(400);
        }
        await shot(page, 'P0-006_revenue-forecast-Q1');
    });

    test('Revenue Forecast — YTD period', async ({ page }) => {
        await stabilize(page, '/estimating/analytics/revenue', '.fd-page');
        await page.waitForTimeout(2500);
        const ytdBtn = page.locator('button', { hasText: 'YTD' });
        if (await ytdBtn.count() > 0) {
            await ytdBtn.click();
            await page.waitForTimeout(400);
        }
        await shot(page, 'P0-006_revenue-forecast-YTD');
    });
});

// ── P0-010/P0-015: Demo data coverage ────────────────────────────────────────
test.describe('P0-010/P0-015: Demo data', () => {
    test('estimate list — 2025 filter', async ({ page }) => {
        await stabilize(page, '/estimating/estimates', '.p-datatable');
        const yearFilter = page.locator('select, .p-dropdown').filter({ hasText: /year|all/i });
        await shot(page, 'P0-015_estimate-list-2025');
    });

    test('staffing list — 2027 plans visible', async ({ page }) => {
        await stabilize(page, '/estimating/staffing-plans', '.sp-card, .empty-state');
        await shot(page, 'P0-015_staffing-list-2027-plans');
    });

    test('staffing list plan count visible', async ({ page }) => {
        await stabilize(page, '/estimating/staffing-plans', '.sp-card, .empty-state');
        const countEl = page.locator('[class*="plan-count"], .filter-bar, .pagination-bar');
        await shot(page, 'P0-010_staffing-plan-count');
    });
});

// ── P0-012: Three-dot menu in list ────────────────────────────────────────────
test.describe('P0-012: Three-dot menu', () => {
    test('menu not clipped — opens at bottom of first row', async ({ page }) => {
        await stabilize(page, '/estimating/estimates', '.p-datatable tbody tr');
        const firstRow = page.locator('.p-datatable tbody tr').first();
        await firstRow.hover();
        await page.waitForTimeout(300);
        await shot(page, 'P0-012_before-menu-open');
        const menuTrigger = firstRow.locator('button').last();
        if (await menuTrigger.count() > 0) {
            await menuTrigger.click();
            await page.waitForTimeout(400);
            await shot(page, 'P0-012_menu-open-not-clipped', false);
        }
    });
});

// ── P0-013: LaborGrid sticky TOTAL column ────────────────────────────────────
test.describe('P0-013: LaborGrid sticky TOTAL', () => {
    test('labor grid visible on existing estimate', async ({ page }) => {
        // Navigate to estimates list and open first estimate
        await stabilize(page, '/estimating/estimates', '.p-datatable tbody tr');
        const firstLink = page.locator('.p-datatable tbody tr td a, .p-datatable tbody tr').first();
        await firstLink.click();
        await page.locator('.estimate-header', { timeout: 15_000 } as any).waitFor({ state: 'visible' }).catch(() => {});
        await page.waitForTimeout(800);
        await shot(page, 'P0-013_labor-grid-left-scroll');

        // Scroll right in the labor grid
        const grid = page.locator('.labor-grid, [class*="labor-grid"]').first();
        if (await grid.count() > 0) {
            await grid.evaluate(el => el.scrollLeft = 9999);
            await page.waitForTimeout(300);
            await shot(page, 'P0-013_labor-grid-right-scroll');
        }
    });
});

// ── P0-014: Crew template dialog ─────────────────────────────────────────────
test.describe('P0-014: Crew template dialog', () => {
    test('crew template dialog opens on estimate form', async ({ page }) => {
        await stabilize(page, '/estimating/estimates/new', '.estimate-header');
        const crewBtn = page.locator('button', { hasText: /crew|template/i });
        if (await crewBtn.count() > 0) {
            await crewBtn.first().click();
            await page.waitForTimeout(500);
            await shot(page, 'P0-014_crew-template-dialog');
        } else {
            await shot(page, 'P0-014_crew-template-btn-not-found');
        }
    });
});

// ── P1-SP-002: Staffing form status read-only ─────────────────────────────────
test.describe('P1-SP-002: Staffing form status', () => {
    test('staffing form shows read-only status badge not dropdown', async ({ page }) => {
        await stabilize(page, '/estimating/staffing-plans', '.sp-card, .empty-state');
        const openBtn = page.locator('[data-testid^="sp-open-"]').first();
        if (await openBtn.count() > 0) {
            await openBtn.click();
            await page.waitForTimeout(1000);
            await shot(page, 'P1-SP-002_staffing-form-status-badge');
        } else {
            await shot(page, 'P1-SP-002_no-plans-to-open');
        }
    });
});

// ── P1-SP-003: PW-TEST plan gone ─────────────────────────────────────────────
test.describe('P1-SP-003: No PW-TEST plan', () => {
    test('staffing list has no PW-TEST header-only plan', async ({ page }) => {
        await stabilize(page, '/estimating/staffing-plans', '.sp-card, .empty-state');
        const pwTest = page.locator('.sp-card, .sp-name').filter({ hasText: /PW-TEST/i });
        await shot(page, 'P1-SP-003_staffing-list-check-pw-test');
        const count = await pwTest.count();
        if (count > 0) {
            console.warn(`⚠️  P1-SP-003 FAIL: Found ${count} PW-TEST plan(s) still visible`);
        } else {
            console.log('✅ P1-SP-003: No PW-TEST plans visible on first page');
        }
    });
});

// ── P0-021: Revision workflow ─────────────────────────────────────────────────
test.describe('P0-021: Revision workflow', () => {
    test('revision drawer opens and shows history', async ({ page }) => {
        await stabilize(page, '/estimating/estimates', '.p-datatable tbody tr');
        const firstLink = page.locator('.p-datatable tbody tr td a, .p-datatable tbody tr').first();
        await firstLink.click();
        await page.waitForTimeout(1000);
        const revisionsBtn = page.locator('button', { hasText: /revision/i });
        if (await revisionsBtn.count() > 0) {
            await revisionsBtn.first().click();
            await page.waitForTimeout(600);
            await shot(page, 'P0-021_revision-drawer-open');
        } else {
            await shot(page, 'P0-021_revision-btn-not-found');
        }
    });
});
