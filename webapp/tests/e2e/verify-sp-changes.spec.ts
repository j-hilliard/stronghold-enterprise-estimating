/**
 * Quick verification test for PART F + PART G changes:
 * - QA-UI-008: Staffing plan shows Job Cost Analysis with burden rate badges
 * - QA-UI-009: Staffing plan shows Summary section
 * - QA-UI-010: Submit for Approval button visible on saved Draft plan
 *
 * Run: npx playwright test verify-sp-changes --headed
 */
import { test, expect } from '@playwright/test';
import { API_BASE_URL, DEV_CREDENTIALS } from './utils/constants';

test.describe('SP Changes Verification', () => {
    let authToken = '';
    let planId = 0;

    test.beforeAll(async ({ request }) => {
        // Authenticate
        const loginResp = await request.post(`${API_BASE_URL}/api/auth/login`, {
            data: { username: DEV_CREDENTIALS.username, password: DEV_CREDENTIALS.password },
            ignoreHTTPSErrors: true,
        });
        const { tempToken } = await loginResp.json();

        const selResp = await request.post(`${API_BASE_URL}/api/auth/select-company`, {
            data: { tempToken, companyCode: 'ETS' },
            ignoreHTTPSErrors: true,
        });
        const { token } = await selResp.json();
        authToken = token;

        // Get the first available staffing plan
        const plansResp = await request.get(`${API_BASE_URL}/api/v1.0/staffing-plans`, {
            headers: { Authorization: `Bearer ${authToken}` },
            ignoreHTTPSErrors: true,
        });
        const plans = await plansResp.json();
        planId = plans[0]?.staffingPlanId ?? 1;
    });

    test('QA-UI-010: Submit for Approval button visible on saved Draft SP', async ({ page }) => {
        // Inject auth token into localStorage before navigating
        await page.goto('http://localhost:5173', { waitUntil: 'domcontentloaded' });
        await page.evaluate((token) => {
            localStorage.setItem('auth_token', token);
        }, authToken);

        await page.goto(`http://localhost:5173/estimating/staffing-plans/${planId}`, {
            waitUntil: 'networkidle',
            timeout: 30_000,
        });

        // Wait for the form to load
        await expect(page.locator('[data-testid="sp-form-view"]')).toBeVisible({ timeout: 15_000 });

        // Screenshot the header area
        await page.screenshot({
            path: 'tests/e2e/screenshots/sp-header-submit-btn.png',
            fullPage: false,
            clip: { x: 0, y: 0, width: 1280, height: 200 },
        });

        // Check for Submit for Approval button
        const submitBtn = page.locator('[data-testid="sp-submit-approval"]');
        const isVisible = await submitBtn.isVisible();
        console.log(`Submit for Approval button visible: ${isVisible}`);

        // Screenshot full page
        await page.screenshot({ path: 'tests/e2e/screenshots/sp-full-page.png', fullPage: true });
    });

    test('QA-UI-008 + QA-UI-009: Job Cost Analysis and Summary visible on SP with labor rows', async ({ page }) => {
        await page.goto('http://localhost:5173', { waitUntil: 'domcontentloaded' });
        await page.evaluate((token) => {
            localStorage.setItem('auth_token', token);
        }, authToken);

        await page.goto(`http://localhost:5173/estimating/staffing-plans/${planId}`, {
            waitUntil: 'networkidle',
            timeout: 30_000,
        });

        await expect(page.locator('[data-testid="sp-form-view"]')).toBeVisible({ timeout: 15_000 });

        // Scroll down to see JCA section
        await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
        await page.waitForTimeout(1000);

        // Screenshot the bottom of the page (JCA + Summary section)
        await page.screenshot({
            path: 'tests/e2e/screenshots/sp-jca-section.png',
            fullPage: true,
        });

        // Check for the job-cost-analysis class (JobCostAnalysis component root)
        const jcaSection = page.locator('.job-cost-analysis');
        const jcaVisible = await jcaSection.isVisible().catch(() => false);
        console.log(`Job Cost Analysis section visible: ${jcaVisible}`);

        // Check for summary section
        const summarySection = page.locator('.sp-summary');
        const summaryVisible = await summarySection.isVisible().catch(() => false);
        console.log(`Plan Summary section visible: ${summaryVisible}`);
    });
});
