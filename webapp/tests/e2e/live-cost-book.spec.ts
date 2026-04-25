/**
 * Cost Book real API tests — hits the running API+Docker SQL.
 * REQ-COST-001 through REQ-COST-007 (Cost Book and Internal Job Cost Analysis requirements).
 */
import { test, expect } from '@playwright/test';
import { seedCostBook } from './utils/liveApi';

// ── Tests ─────────────────────────────────────────────────────────────────────

test.describe('Cost Book page', () => {
    test.beforeAll(async ({ request }) => {
        await seedCostBook(request);
    });

    test('[REQ-COST-001] loads and shows all four sections', async ({ page }) => {
        await page.goto('/estimating/cost-book');

        // Stats bar is the anchor — waits for API load to complete
        await expect(page.locator('.stats-bar')).toBeVisible({ timeout: 15000 });
        await expect(page.locator('.stats-bar')).toContainText('Labor Positions');

        // All 4 collapsible sections must be present
        const sections = page.locator('.cb-section');
        await expect(sections).toHaveCount(4, { timeout: 15000 });

        // Burden section header text
        await expect(sections.nth(0)).toContainText('OVERHEAD & BURDEN RATES');
        await expect(sections.nth(1)).toContainText('LABOR COSTS');
        await expect(sections.nth(2)).toContainText('EQUIPMENT COSTS');
        await expect(sections.nth(3)).toContainText('EXPENSES');

        await page.screenshot({ path: 'test-results/cost-book-loaded.png', fullPage: true });
    });

    test('[REQ-COST-002] burden section shows items and 3 columns', async ({ page }) => {
        await page.goto('/estimating/cost-book');
        await expect(page.locator('.stats-bar')).toBeVisible({ timeout: 15000 });

        // Burden grid columns
        const cols = page.locator('.burden-col');
        await expect(cols).toHaveCount(3);
        await expect(cols.nth(0)).toContainText('Labor Burden');
        await expect(cols.nth(1)).toContainText('Insurance & Bonds');
        await expect(cols.nth(2)).toContainText('Other Overhead');

        // At least FICA and Workers Comp should be visible
        await expect(page.locator('.burden-name').filter({ hasText: 'FICA' })).toBeVisible();
        await expect(page.locator('.burden-name').filter({ hasText: 'Workers Comp' })).toBeVisible();

        // Total burden chip should show non-zero pct
        const totalChip = page.locator('.stat-chip').filter({ hasText: 'Total Burden' });
        await expect(totalChip).toBeVisible();
        await expect(totalChip).not.toContainText('0.0%');

        await page.screenshot({ path: 'test-results/cost-book-burden.png', fullPage: true });
    });

    test('[REQ-COST-001] collapsible sections collapse and expand', async ({ page }) => {
        await page.goto('/estimating/cost-book');
        await expect(page.locator('.stats-bar')).toBeVisible({ timeout: 15000 });

        const firstHeader = page.locator('.cb-section-header').first();

        // Click to collapse
        await firstHeader.click();
        await expect(page.locator('.burden-grid')).toBeHidden();

        // Click to expand
        await firstHeader.click();
        await expect(page.locator('.burden-grid')).toBeVisible();

        await page.screenshot({ path: 'test-results/cost-book-collapse.png', fullPage: true });
    });

    test('[REQ-COST-003] Save All button is disabled when no changes', async ({ page }) => {
        await page.goto('/estimating/cost-book');
        await expect(page.locator('.stats-bar')).toBeVisible({ timeout: 15000 });

        const saveBtn = page.locator('button', { hasText: 'Save All' });
        await expect(saveBtn).toBeVisible();
        await expect(saveBtn).toBeDisabled();
    });

    test('[REQ-COST-004] Reset Defaults button opens confirm dialog', async ({ page }) => {
        await page.goto('/estimating/cost-book');
        await expect(page.locator('.stats-bar')).toBeVisible({ timeout: 15000 });

        const resetBtn = page.locator('button', { hasText: 'Reset Defaults' });
        await expect(resetBtn).toBeVisible();
        await resetBtn.click();

        // Our custom reset confirm dialog
        const confirmDlg = page.locator('[data-testid="reset-confirm-dialog"]');
        await expect(confirmDlg).toBeVisible({ timeout: 5000 });

        await page.screenshot({ path: 'test-results/cost-book-reset-dialog.png' });

        // Cancel the dialog
        await confirmDlg.locator('button', { hasText: 'Cancel' }).click();
        await expect(confirmDlg).toBeHidden({ timeout: 3000 });
    });

    test('[REQ-COST-005] Add Position dialog opens and closes', async ({ page }) => {
        await page.goto('/estimating/cost-book');
        await expect(page.locator('.stats-bar')).toBeVisible({ timeout: 15000 });

        // Labor section — click Add Position
        await page.locator('button', { hasText: 'Add Position' }).click();
        await expect(page.locator('.p-dialog').filter({ hasText: 'Add Position' })).toBeVisible({ timeout: 5000 });

        await page.screenshot({ path: 'test-results/cost-book-add-position.png' });

        // Cancel
        await page.locator('.p-dialog button').filter({ hasText: 'Cancel' }).click();
        await expect(page.locator('.p-dialog').filter({ hasText: 'Add Position' })).toBeHidden();
    });
});
