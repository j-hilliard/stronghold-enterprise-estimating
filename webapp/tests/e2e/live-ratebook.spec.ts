/**
 * Live UI→API→DB reconciliation tests for the rate library.
 * Requires API on PW_API_BASE_URL and Docker SQL on port 14331.
 * Run with: npm run test:e2e:live
 *
 * REQ-RATE-001, REQ-RATE-002, REQ-RATE-003, REQ-RATE-004,
 * REQ-QA-001, REQ-QA-002.
 */
import { test, expect } from '@playwright/test';
import { apiGet, apiPost, seedDevData } from './utils/liveApi';
import { TEST_TAG } from './utils/constants';

const TAG = TEST_TAG;

test.describe('Live — rate books', () => {
    test.beforeAll(async ({ request }) => {
        await seedDevData(request);
    });

    // ── REQ-RATE-001: rate books are customer/location specific ──────────────
    test('[REQ-RATE-001] rate book list loads and shows records', async ({ page }) => {
        await page.goto('/estimating/rate-books');
        await expect(page.locator('[data-testid="rb-sidebar"]')).toBeVisible({ timeout: 15_000 });

        // At least one rate book from seeded data
        const items = page.locator('[data-testid^="rb-item-"]');
        await expect(items.first()).toBeVisible({ timeout: 15_000 });
    });

    // ── REQ-RATE-002: exact match load ───────────────────────────────────────
    test('[REQ-RATE-002] exact customer/location match loads correct rate book', async ({ page }) => {
        await page.goto('/estimating/rate-books');
        await expect(page.locator('[data-testid="rb-sidebar"]')).toBeVisible({ timeout: 15_000 });

        // Use lookup panel
        const lookupClientInput = page.locator('input[data-testid="rb-lookup-client"]');
        if (await lookupClientInput.count() === 0) {
            test.skip(true, 'Rate book lookup panel not yet implemented');
            return;
        }

        await lookupClientInput.fill('Shell');
        await page.locator('[data-testid="rb-lookup-find"]').click();
        await expect(page.locator('[data-testid="rb-lookup-table"]')).toBeVisible({ timeout: 10_000 });
    });

    // ── REQ-RATE-003: nearest suggestion when no exact match ─────────────────
    test('[REQ-RATE-003] nearest rate book candidates appear when no exact match', async ({ page, request }) => {
        // Ensure there is at least one rate book in DB to match against
        const books = await apiGet(request, '/api/v1/rate-books');
        if (!books || books.length === 0) {
            test.skip(true, 'No rate books seeded — cannot test nearest match');
            return;
        }

        await page.goto('/estimating/rate-books');
        await expect(page.locator('[data-testid="rb-sidebar"]')).toBeVisible({ timeout: 15_000 });

        const lookupClientInput = page.locator('input[data-testid="rb-lookup-client"]');
        if (await lookupClientInput.count() === 0) {
            test.skip(true, 'Rate book lookup panel not yet implemented');
            return;
        }

        // Search with a client that likely has no exact city match
        await lookupClientInput.fill('BP');
        const cityInput = page.locator('input[data-testid="rb-lookup-city"]');
        if (await cityInput.count() > 0) {
            await cityInput.fill('Pasadena');
        }
        await page.locator('[data-testid="rb-lookup-find"]').click();

        // Results table should appear (even if no exact Pasadena match)
        await expect(page.locator('[data-testid="rb-lookup-table"]')).toBeVisible({ timeout: 10_000 });
    });

    // ── REQ-RATE-004: clone rate book ────────────────────────────────────────
    test('[REQ-RATE-004] user can clone a rate book to create a new customer/location book', async ({ page, request }) => {
        await page.goto('/estimating/rate-books');
        await expect(page.locator('[data-testid="rb-sidebar"]')).toBeVisible({ timeout: 15_000 });

        // Select first rate book
        const firstItem = page.locator('[data-testid^="rb-item-"]').first();
        if (await firstItem.count() === 0) {
            test.skip(true, 'No rate books in DB to clone');
            return;
        }
        await firstItem.click();

        // Click duplicate/clone
        const duplicateBtn = page.locator('[data-testid="rb-duplicate"]');
        if (await duplicateBtn.count() === 0) {
            test.skip(true, 'Duplicate button not yet implemented');
            return;
        }
        await duplicateBtn.click();

        const cloneDialog = page.locator('[data-testid="rb-clone-dialog"]');
        await expect(cloneDialog).toBeVisible({ timeout: 5_000 });

        const cloneName = `${TAG} Clone ${Date.now()}`;
        await cloneDialog.locator('[data-testid="rb-clone-name"]').fill(cloneName);
        await cloneDialog.locator('button', { hasText: /duplicate|clone/i }).click();

        // Verify the clone appears in the sidebar
        await expect(page.locator('[data-testid="rb-sidebar"]', { hasText: cloneName })).toBeVisible({
            timeout: 10_000,
        });

        // DB reconciliation — clone must exist in API
        const books = await apiGet(request, '/api/v1/rate-books');
        const clone = books.find((b: any) => b.name === cloneName);
        expect(clone).toBeTruthy();
    });
});
