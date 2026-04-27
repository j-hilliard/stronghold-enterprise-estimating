import { test, expect, type APIRequestContext } from '@playwright/test';
import { resetDevData, seedCostBook } from './utils/liveApi';

function jsonResponse(data: unknown, status = 200) {
    return {
        ok: () => status >= 200 && status < 300,
        status: () => status,
        text: async () => JSON.stringify(data),
        json: async () => data,
    };
}

function fakeRequest(existingCostBooks: unknown[]) {
    const calls: string[] = [];
    const request = {
        post: async (url: string) => {
            calls.push(`POST ${url}`);
            if (url.endsWith('/api/auth/login')) return jsonResponse({ tempToken: 'temp-token' });
            if (url.endsWith('/api/auth/select-company')) return jsonResponse({ token: 'jwt-token' });
            return jsonResponse({});
        },
        get: async (url: string) => {
            calls.push(`GET ${url}`);
            if (url.endsWith('/api/v1.0/cost-books')) return jsonResponse(existingCostBooks);
            return jsonResponse({});
        },
    } as unknown as APIRequestContext;

    return { request, calls };
}

test.describe('cost book safety helpers', () => {
    test('seedCostBook does not reset an existing cost book', async () => {
        const { request, calls } = fakeRequest([{ costBookId: 1, name: 'Standard Cost Book' }]);

        await seedCostBook(request);

        expect(calls.some(call => call.includes('/cost-books/reset-standard'))).toBe(false);
    });

    test('seedCostBook only creates defaults when no cost book exists', async () => {
        const { request, calls } = fakeRequest([]);

        await seedCostBook(request);

        expect(calls.some(call => call.includes('/cost-books/reset-standard'))).toBe(true);
    });

    test('resetDevData preserves cost books by default', async () => {
        const { request, calls } = fakeRequest([]);

        await resetDevData(request);

        expect(calls.some(call => call.includes('/api/v1.0/dev/reset?includeCostBooks=true'))).toBe(false);
        expect(calls.some(call => call.endsWith('/api/v1.0/dev/reset'))).toBe(true);
    });
});
