/**
 * Smoke tests — basic navigation and layout checks.
 * REQ-EST-001, REQ-EST-006, REQ-EST-011
 */
import { test, expect } from '@playwright/test';

// REQ-EST-006: Estimate log renders with search/filter support.
test('[REQ-EST-006] app loads and redirects to estimate list', async ({ page }) => {
    // domcontentloaded prevents goto from hanging while SPA API calls are in-flight.
    await page.goto('/', { waitUntil: 'domcontentloaded' });
    // waitForURL polls continuously — handles the async Vue Router client-side redirect.
    await page.waitForURL(/\/estimating\/estimates/, { timeout: 20_000 });
    await expect(page.locator('.p-datatable')).toBeVisible({ timeout: 15_000 });
});

// REQ-EST-006: Estimate list renders with core controls.
test('[REQ-EST-006] estimate list renders', async ({ page }) => {
    await page.goto('/estimating/estimates');
    await expect(page.locator('.p-datatable')).toBeVisible({ timeout: 15_000 });
    await expect(page.locator('button', { hasText: 'New Estimate' })).toBeVisible();
});

// REQ-EST-011: Sections are collapsible without data loss.
test('[REQ-EST-001, REQ-EST-011] estimate form collapsible sections', async ({ page }) => {
    // domcontentloaded prevents goto from hanging while the cost book API call is in-flight.
    await page.goto('/estimating/estimates/new', { waitUntil: 'domcontentloaded' });
    await expect(page.locator('.estimate-header')).toBeVisible({ timeout: 15_000 });

    // All sections visible by default
    const sections = page.locator('.section-toggle');
    await expect(sections).toHaveCount(5);

    // Click Labor to collapse
    await sections.nth(0).click();
    const laborBody = page.locator('.collapsible-section').nth(0).locator('.section-body');
    await expect(laborBody).toBeHidden();

    // Click again to expand
    await sections.nth(0).click();
    await expect(laborBody).toBeVisible();

    await page.screenshot({ path: 'test-results/collapsible.png', fullPage: true });
});
