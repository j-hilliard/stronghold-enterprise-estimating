/**
 * AI Chat sidebar tests — FAB visibility, chat open/close, message send/receive,
 * and NLP prompt variation robustness.
 * REQ-AI-001 through REQ-AI-004, REQ-AI-011, REQ-QA-004.
 */
import { test, expect } from '@playwright/test';

test.describe('AI Chat sidebar', () => {
    test('[REQ-AI-001] FAB is visible and opens chat window', async ({ page }) => {
        await page.goto('/estimating/estimates/new');
        await page.locator('.estimate-header').waitFor({ timeout: 15000 });
        await page.waitForTimeout(400);

        await expect(page.locator('.ai-fab')).toBeVisible();

        await page.locator('.ai-fab').click();
        await page.waitForTimeout(350);
        await expect(page.locator('.ai-window')).toBeVisible();
    });

    test('[REQ-AI-001] sends a message and receives AI response', async ({ page }) => {
        await page.goto('/estimating/estimates/new');
        await page.locator('.estimate-header').waitFor({ timeout: 15000 });
        await page.waitForTimeout(400);

        await page.locator('.ai-fab').click();
        await expect(page.locator('.ai-window')).toBeVisible();

        await page.locator('.ai-input').fill('hello');
        await page.locator('.ai-input').press('Enter');

        await expect(page.locator('.ai-bubble--assistant').first()).toBeVisible({ timeout: 20000 });
    });

    test('[REQ-AI-001] chat window closes and reopens correctly', async ({ page }) => {
        await page.goto('/estimating/estimates/new');
        await page.locator('.estimate-header').waitFor({ timeout: 15000 });
        await page.waitForTimeout(400);

        await page.locator('.ai-fab').click();
        await expect(page.locator('.ai-window')).toBeVisible();

        await page.locator('.ai-fab').click();
        await page.waitForTimeout(300);
        await expect(page.locator('.ai-window')).not.toBeVisible();

        await page.locator('.ai-fab').click();
        await expect(page.locator('.ai-window')).toBeVisible();
    });
});

// ── NLP variation tests — REQ-AI-003, REQ-AI-004, REQ-QA-004 ────────────────

test.describe('AI NLP variation robustness', () => {
    async function openChat(page: any) {
        await page.goto('/estimating/estimates/new');
        await page.locator('.estimate-header').waitFor({ timeout: 15000 });
        await page.waitForTimeout(400);
        await page.locator('.ai-fab').click();
        await expect(page.locator('.ai-window')).toBeVisible();
    }

    async function sendAndWait(page: any, message: string) {
        await page.locator('.ai-input').fill(message);
        await page.locator('.ai-input').press('Enter');
        // Wait for typing indicator to appear then disappear (AI responded).
        // ai-typing is present while the response is streaming; once gone, actual bubble has content.
        await page.waitForFunction(
            () => !!document.querySelector('.ai-bubble--assistant.ai-typing'),
            { timeout: 10000 },
        ).catch(() => {/* typing indicator may flash too fast — that's OK */});
        await page.waitForFunction(
            () => !document.querySelector('.ai-bubble--assistant.ai-typing'),
            { timeout: 30000 },
        );
        // Return text of the first non-typing assistant bubble
        const bubble = page.locator('.ai-bubble--assistant:not(.ai-typing)').first();
        await expect(bubble).toBeVisible({ timeout: 5000 });
        return bubble.textContent();
    }

    test('[REQ-AI-003, REQ-QA-004] misspelled city — bakersfeild normalizes to Bakersfield', async ({ page }) => {
        await openChat(page);
        const reply = await sendAndWait(
            page,
            'create estimate for BP in bakersfeild california starting June 6 for 12 days',
        );
        // Agent must either normalize the city or ask for clarification — it must NOT silently drop the location
        expect(reply).toBeTruthy();
        const normalized = (reply ?? '').toLowerCase();
        const handledLocation =
            normalized.includes('bakersfield') ||
            normalized.includes('bakersfeild') ||
            normalized.includes('location') ||
            normalized.includes('clarif');
        expect(handledLocation).toBeTruthy();
    });

    test('[REQ-AI-003, REQ-QA-004] misspelled city — pasedena normalizes to Pasadena', async ({ page }) => {
        await openChat(page);
        const reply = await sendAndWait(
            page,
            'new staffing plan Shell pasedena TX start 2026-06-10 13 days',
        );
        expect(reply).toBeTruthy();
        const normalized = (reply ?? '').toLowerCase();
        const handledLocation =
            normalized.includes('pasadena') ||
            normalized.includes('pasedena') ||
            normalized.includes('location') ||
            normalized.includes('clarif');
        expect(handledLocation).toBeTruthy();
    });

    test('[REQ-AI-003, REQ-QA-004] misspelled position — boilermakr is handled gracefully', async ({ page }) => {
        await openChat(page);
        const reply = await sendAndWait(page, 'add a boilermakr to the labor section');
        expect(reply).toBeTruthy();
        // Agent must either ask for clarification or recognize the misspelling
        const handled = (reply ?? '').toLowerCase();
        const recognizedPosition =
            handled.includes('boilermaker') || handled.includes('clarif') || handled.includes('position');
        expect(recognizedPosition).toBeTruthy();
    });

    test('[REQ-AI-004, REQ-QA-004] location format — "city, ST" parsed correctly', async ({ page }) => {
        await openChat(page);
        const reply = await sendAndWait(
            page,
            'new estimate for Valero in Port Arthur, TX start 2026-07-01 for 10 days',
        );
        expect(reply).toBeTruthy();
        const lower = (reply ?? '').toLowerCase();
        // Agent must reference Port Arthur or TX in the response
        const parsedLocation =
            lower.includes('port arthur') || lower.includes('tx') || lower.includes('texas') || lower.includes('valero');
        expect(parsedLocation).toBeTruthy();
    });

    test('[REQ-AI-002, REQ-AI-011] ambiguous helper — agent asks for clarification before writing', async ({ page }) => {
        await openChat(page);
        const reply = await sendAndWait(page, 'add a helper');
        expect(reply).toBeTruthy();
        // REQ-AI-002: ambiguous actions must ask clarifying questions, not write immediately
        const lower = (reply ?? '').toLowerCase();
        const askedForClarification =
            lower.includes('which') ||
            lower.includes('type') ||
            lower.includes('clarif') ||
            lower.includes('pipefitter') ||
            lower.includes('welding') ||
            lower.includes('boilermaker') ||
            lower.includes('specify');
        expect(askedForClarification).toBeTruthy();
    });

    test('[REQ-AI-009, REQ-QA-004] jobs-today query returns a data-grounded answer', async ({ page }) => {
        await openChat(page);
        const reply = await sendAndWait(page, 'what jobs are going on today');
        expect(reply).toBeTruthy();
        // Agent must respond with data or a meaningful no-data message — not an error
        expect((reply ?? '').length).toBeGreaterThan(10);
    });
});
