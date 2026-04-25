/**
 * Shared constants for all Playwright E2E tests.
 * Import from here — never hardcode URLs or credentials in spec files.
 * REQ-GOV-003: All test files trace to requirements.
 */

export const API_BASE_URL = process.env.PW_API_BASE_URL ?? 'https://localhost:7211';

export const DEV_CREDENTIALS = {
    username: 'dev.user',
    password: 'StrongholdDev2024',
    companyCode: 'CSL',
} as const;

/** Prefix all test-created records so globalSetup can clean them up after the run. */
export const TEST_TAG = '[PW-TEST]';
