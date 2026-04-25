import { defineConfig, devices } from '@playwright/test';

const isCI = !!process.env.CI;

// API base URL — override with PW_API_BASE_URL env var in CI or when API runs elsewhere.
// Never hardcode in spec files; import from tests/e2e/utils/constants.ts instead.
export const API_BASE_URL = process.env.PW_API_BASE_URL ?? 'https://localhost:7211';

const sharedChromiumOptions = {
    ...devices['Desktop Chrome'],
    launchOptions: {
        // Trust dev SSL certs so axios/fetch calls within the page work
        args: ['--ignore-certificate-errors'],
    },
};

export default defineConfig({
    testDir: './tests/e2e',
    globalSetup: './tests/e2e/globalSetup',
    timeout: 60_000,
    fullyParallel: false,
    forbidOnly: isCI,
    retries: isCI ? 1 : 0,
    // Live tests must run single-worker to prevent DB race conditions.
    // Mocked tests are safe to parallelize but kept sequential for stability.
    workers: isCI ? 1 : 1,
    reporter: [['list'], ['html', { open: 'never' }]],
    expect: {
        timeout: 15_000,
        toHaveScreenshot: {
            maxDiffPixelRatio: 0.02,
        },
    },
    use: {
        baseURL: 'https://localhost:7210',
        ignoreHTTPSErrors: true,
        actionTimeout: 15_000,
        trace: 'retain-on-failure',
        screenshot: 'only-on-failure',
        video: 'retain-on-failure',
        viewport: { width: 1536, height: 960 },
    },
    webServer: {
        command: 'npm run dev -- --host 127.0.0.1 --port 7210',
        url: 'https://localhost:7210',
        reuseExistingServer: !isCI,
        timeout: 120_000,
        ignoreHTTPSErrors: true,
        stdout: 'pipe',
        stderr: 'pipe',
        env: {
            VITE_BYPASS_AUTH: 'true',
            VITE_APP_API_BASE_URL: API_BASE_URL,
        },
    },
    projects: [
        {
            // Mocked suite — UI state-machine tests using page.route() interceptors.
            // No real API required. Fast, deterministic, runs first.
            name: 'chromium-mocked',
            testIgnore: ['**/live-*.spec.ts'],
            use: sharedChromiumOptions,
        },
        {
            // Live suite — real UI→API→DB reconciliation tests.
            // Requires API on PW_API_BASE_URL and Docker SQL on port 14331.
            // Run with: npm run test:e2e:live
            name: 'chromium-live',
            testMatch: ['**/live-*.spec.ts'],
            use: sharedChromiumOptions,
        },
    ],
});
