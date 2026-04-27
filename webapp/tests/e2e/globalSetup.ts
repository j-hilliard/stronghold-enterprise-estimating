/**
 * Playwright global setup — seeds dev data before live test suite runs.
 * REQ-GOV-012: All local testing uses isolated Docker SQL only.
 * REQ-QA-001: End-to-end tests must cover all in-scope modules.
 */
import { chromium, request } from '@playwright/test';
import { API_BASE_URL, DEV_CREDENTIALS } from './utils/constants';

export default async function globalSetup() {
    // Skip when SKIP_GLOBAL_SETUP=true (mocked-only runs) or gracefully handle missing API.
    if (process.env.SKIP_GLOBAL_SETUP === 'true') {
        console.log('globalSetup: skipped (SKIP_GLOBAL_SETUP=true)');
        return;
    }

    const ctx = await request.newContext({ ignoreHTTPSErrors: true });

    try {
        // Authenticate — two-step: login → select-company
        const loginResp = await ctx.post(`${API_BASE_URL}/api/auth/login`, {
            data: { username: DEV_CREDENTIALS.username, password: DEV_CREDENTIALS.password },
        });
        if (!loginResp.ok()) {
            const body = await loginResp.text();
            throw new Error(`login failed (${loginResp.status()}): ${body}`);
        }
        const { tempToken } = await loginResp.json();

        const selectResp = await ctx.post(`${API_BASE_URL}/api/auth/select-company`, {
            data: { tempToken, companyCode: DEV_CREDENTIALS.companyCode },
        });
        if (!selectResp.ok()) {
            const body = await selectResp.text();
            throw new Error(`select-company failed (${selectResp.status()}): ${body}`);
        }
        const { token } = await selectResp.json();
        const headers = { Authorization: `Bearer ${token}` };

        // Seed standard dev data (estimates, rate books, staffing plans)
        // 409 = already seeded — treated as OK.
        const seedResp = await ctx.post(`${API_BASE_URL}/api/v1.0/dev/seed`, { headers });
        if (!seedResp.ok() && seedResp.status() !== 409) {
            const body = await seedResp.text();
            throw new Error(`dev/seed failed (${seedResp.status()}): ${body}`);
        }

        // NOTE: do NOT call cost-books/reset-standard here. Live setup must not mutate an
        // existing demo cost book; dev/seed already creates the standard cost books when absent.

        console.log('globalSetup: dev seed complete');
    } catch (err) {
        // API not reachable — warn but don't block mocked UI tests.
        // Live tests (live-*.spec.ts) will still fail if they need real data.
        console.warn(`globalSetup: API unavailable — seed skipped. (${(err as Error).message})`);
    } finally {
        await ctx.dispose();
    }
}
