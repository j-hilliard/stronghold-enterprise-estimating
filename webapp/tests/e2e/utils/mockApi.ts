import { Page, Route } from '@playwright/test';

export type MockApiState = Record<string, never>;

const JSON_HEADERS = {
    'access-control-allow-origin': '*',
    'content-type': 'application/json',
};

function replyJson(route: Route, status: number, payload: unknown) {
    return route.fulfill({
        status,
        headers: JSON_HEADERS,
        body: JSON.stringify(payload),
    });
}

export async function installMockApi(page: Page): Promise<MockApiState> {
    // Auth bypass — return a fake JWT so mocked tests never need a live API.
    await page.route('**/api/auth/**', async (route) => {
        const url = new URL(route.request().url());
        const path = url.pathname.toLowerCase();

        if (path.endsWith('/login')) {
            return replyJson(route, 200, {
                tempToken: 'mock-temp-token',
                username: 'dev.user',
                companies: [{ code: 'CSL', name: 'Cat-Spec, Ltd.', jobLetter: null }],
            });
        }
        if (path.endsWith('/select-company')) {
            return replyJson(route, 200, {
                token: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjEiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiZGV2LnVzZXIiLCJjb21wYW55X2NvZGUiOiJDU0wiLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJBZG1pbmlzdHJhdG9yIiwiZXhwIjo5OTk5OTk5OTk5fQ.mock',
                username: 'dev.user',
                companyCode: 'CSL',
                roles: ['Administrator'],
            });
        }
        return route.continue();
    });

    return {};
}
