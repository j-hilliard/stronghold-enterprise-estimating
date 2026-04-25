/**
 * Visual regression baselines — enforced with toHaveScreenshot().
 * REQ-OBJ-002: Preserve enterprise template UX consistency.
 * REQ-QA-003: Visual regression coverage is required for key screens/states.
 */
import { test, expect } from '@playwright/test';
import { gotoAndStabilize, disableAnimations } from './utils/ui';

test.describe('Visual regression — dark mode (default)', () => {
    test('[REQ-OBJ-002, REQ-QA-003] estimate form baseline', async ({ page }) => {
        await gotoAndStabilize(page, '/estimating/estimates/new');
        await expect(page.locator('.estimate-header')).toBeVisible({ timeout: 15_000 });
        await disableAnimations(page);
        await page.waitForTimeout(300);
        await expect(page).toHaveScreenshot('theme-dark-estimate-form.png', { fullPage: true });
    });

    test('[REQ-OBJ-002, REQ-QA-003] estimate list baseline', async ({ page }) => {
        await gotoAndStabilize(page, '/estimating/estimates');
        await expect(page.locator('.p-datatable')).toBeVisible({ timeout: 15_000 });
        await disableAnimations(page);
        await page.waitForTimeout(300);
        await expect(page).toHaveScreenshot('theme-dark-estimate-list.png', { fullPage: true });
    });

    test('[REQ-OBJ-002, REQ-QA-003] cost book baseline', async ({ page }) => {
        await gotoAndStabilize(page, '/estimating/cost-book');
        // Wait for the view to finish loading (stats-bar when API is live, error/empty state otherwise)
        await expect(page.locator('.cost-book-view')).toBeVisible({ timeout: 15_000 });
        await page.waitForFunction(() => !document.querySelector('.p-progress-spinner'), { timeout: 10_000 });
        await disableAnimations(page);
        await page.waitForTimeout(300);
        await expect(page).toHaveScreenshot('theme-dark-cost-book.png', { fullPage: true });
    });
});

test.describe('Visual regression — light mode toggle', () => {
    test('[REQ-OBJ-002, REQ-QA-003] estimate form light mode', async ({ page }) => {
        await gotoAndStabilize(page, '/estimating/estimates/new');
        await expect(page.locator('.estimate-header')).toBeVisible({ timeout: 15_000 });
        await page.locator('.theme-toggle-btn').click();
        await disableAnimations(page);
        await page.waitForTimeout(300);

        // Verify light mode is active — body background must not be black
        const bodyBg = await page.evaluate(() => window.getComputedStyle(document.body).backgroundColor);
        expect(bodyBg).not.toBe('rgb(0, 0, 0)');
        expect(bodyBg).not.toBe('#000');

        await expect(page).toHaveScreenshot('theme-light-estimate-form.png', { fullPage: true });
    });

    test('[REQ-OBJ-002, REQ-QA-003] estimate list light mode', async ({ page }) => {
        await gotoAndStabilize(page, '/estimating/estimates');
        await expect(page.locator('.p-datatable')).toBeVisible({ timeout: 15_000 });
        await page.locator('.theme-toggle-btn').click();
        await disableAnimations(page);
        await page.waitForTimeout(300);
        await expect(page).toHaveScreenshot('theme-light-estimate-list.png', { fullPage: true });
    });
});
