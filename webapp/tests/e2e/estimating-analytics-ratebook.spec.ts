/**
 * Estimating — rate books and analytics (mocked suite).
 * REQ-RATE-001 through REQ-RATE-004, REQ-AN-001, REQ-AN-002, REQ-MF-002, REQ-MF-004.
 */
import { expect, Page, Route, test } from '@playwright/test';

type RateBookLaborRate = {
    position: string;
    laborType: string;
    craftCode?: string | null;
    navCode?: string | null;
    stRate: number;
    otRate: number;
    dtRate: number;
};

type RateBook = {
    rateBookId: number;
    name: string;
    client?: string | null;
    clientCode?: string | null;
    city?: string | null;
    state?: string | null;
    isStandardBaseline: boolean;
    effectiveDate?: string | null;
    updatedAt: string;
    laborRates: RateBookLaborRate[];
};

type EstimateListItem = {
    estimateId: number;
    estimateNumber: string;
    name: string;
    client: string;
    status: string;
    branch?: string | null;
    city?: string | null;
    state?: string | null;
    startDate?: string | null;
    endDate?: string | null;
    confidencePct?: number | null;
    grandTotal?: number | null;
    updatedAt?: string | null;
};

type EstimateDetail = {
    estimateId: number;
    status: string;
    fcoEntries: Array<{ dollarAdjustment: number }>;
    laborRows: Array<{ position?: string; craftCode?: string; scheduleJson?: string }>;
};

type StaffingListItem = {
    staffingPlanId: number;
    staffingPlanNumber: string;
    name: string;
    client: string;
    status: string;
    startDate?: string | null;
    endDate?: string | null;
    roughLaborTotal?: number | null;
    convertedEstimateId?: number | null;
    updatedAt?: string | null;
};

type StaffingDetail = {
    staffingPlanId: number;
    status: string;
    convertedEstimateId?: number | null;
    laborRows: Array<{ position?: string; craftCode?: string; scheduleJson?: string }>;
};

type MockState = {
    rateBooks: RateBook[];
    estimates: EstimateListItem[];
    estimateDetails: Record<number, EstimateDetail>;
    staffingPlans: StaffingListItem[];
    staffingDetails: Record<number, StaffingDetail>;
    lastRateBookCreatePayload: any | null;
    lastRateBookUpdatePayload: any | null;
};

const JSON_HEADERS = {
    'access-control-allow-origin': '*',
    'content-type': 'application/json',
};

const MOCK_JWT =
    'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjQxMDI0NDQ4MDB9.signature';

function json(route: Route, status: number, payload: unknown) {
    return route.fulfill({
        status,
        headers: JSON_HEADERS,
        body: JSON.stringify(payload),
    });
}

function noContent(route: Route, status = 204) {
    return route.fulfill({
        status,
        headers: { 'access-control-allow-origin': '*' },
    });
}

function nowIso() {
    return new Date('2026-03-25T12:00:00.000Z').toISOString();
}

async function primeAuth(page: Page) {
    await page.addInitScript(
        ({ token }) => {
            localStorage.setItem('ste_auth_token', token);
            localStorage.setItem(
                'ste_auth_user',
                JSON.stringify({
                    username: 'dev.user',
                    companyCode: 'CSL',
                    roles: ['Administrator'],
                }),
            );
        },
        { token: MOCK_JWT },
    );
}

function scoreRateBook(
    rateBook: RateBook,
    client: string,
    city: string | null,
    state: string | null,
): number {
    const rbClient = (rateBook.client ?? '').toLowerCase();
    const queryClient = client.toLowerCase();
    const clientMatch = rbClient.includes(queryClient) || queryClient.includes(rbClient);
    if (!clientMatch) return 0;

    let score = 1;
    if (state && (rateBook.state ?? '').toLowerCase() === state.toLowerCase()) score += 1;
    if (city && (rateBook.city ?? '').toLowerCase() === city.toLowerCase()) score += 1;
    return score;
}

function paginate<T>(items: T[], url: URL) {
    const page = Number(url.searchParams.get('page') ?? '1');
    const pageSize = Number(url.searchParams.get('pageSize') ?? '50');
    const start = (page - 1) * pageSize;
    return {
        total: items.length,
        page,
        pageSize,
        items: items.slice(start, start + pageSize),
    };
}

async function installEstimatingMocks(page: Page): Promise<MockState> {
    const state: MockState = {
        rateBooks: [
            {
                rateBookId: 1,
                name: 'CSL Standard',
                client: 'CSL',
                clientCode: 'CSL',
                city: 'Houston',
                state: 'TX',
                isStandardBaseline: true,
                effectiveDate: '2026-01-01T00:00:00.000Z',
                updatedAt: nowIso(),
                laborRates: [
                    { position: 'PM', laborType: 'Direct', craftCode: 'PM', navCode: '100', stRate: 125, otRate: 187.5, dtRate: 250 },
                    { position: 'Welder', laborType: 'Direct', craftCode: 'WELD', navCode: '200', stRate: 95, otRate: 142.5, dtRate: 190 },
                ],
            },
            {
                rateBookId: 2,
                name: 'BP Houston TX',
                client: 'BP',
                clientCode: 'BPH',
                city: 'Houston',
                state: 'TX',
                isStandardBaseline: false,
                effectiveDate: '2026-02-10T00:00:00.000Z',
                updatedAt: nowIso(),
                laborRates: [
                    { position: 'PM', laborType: 'Direct', craftCode: 'PM', navCode: '100', stRate: 140, otRate: 210, dtRate: 280 },
                    { position: 'Pipefitter', laborType: 'Direct', craftCode: 'PIPE', navCode: '210', stRate: 110, otRate: 165, dtRate: 220 },
                ],
            },
        ],
        estimates: [
            {
                estimateId: 101,
                estimateNumber: 'H-26-0001-BPH',
                name: 'BP Turnaround Alpha',
                client: 'BP',
                status: 'Awarded',
                startDate: '2026-06-01',
                endDate: '2026-06-12',
                confidencePct: 100,
                grandTotal: 100000,
                updatedAt: nowIso(),
            },
            {
                estimateId: 102,
                estimateNumber: 'H-26-0002-BPH',
                name: 'BP Expansion Beta',
                client: 'BP',
                status: 'Pending',
                startDate: '2026-07-15',
                endDate: '2026-07-24',
                confidencePct: 60,
                grandTotal: 50000,
                updatedAt: nowIso(),
            },
            {
                estimateId: 103,
                estimateNumber: 'H-26-0003-BPH',
                name: 'BP Retrofit Gamma',
                client: 'BP',
                status: 'Lost',
                startDate: '2026-07-20',
                endDate: '2026-07-25',
                confidencePct: 90,
                grandTotal: 20000,
                updatedAt: nowIso(),
            },
        ],
        estimateDetails: {
            101: {
                estimateId: 101,
                status: 'Awarded',
                fcoEntries: [{ dollarAdjustment: 5000 }],
                laborRows: [{ position: 'Pipefitter', craftCode: 'PIPE', scheduleJson: '{"2026-06-01":3,"2026-06-02":2}' }],
            },
            102: {
                estimateId: 102,
                status: 'Pending',
                fcoEntries: [{ dollarAdjustment: -1000 }],
                laborRows: [{ position: 'Pipefitter', craftCode: 'PIPE', scheduleJson: '{"2026-07-15":4}' }],
            },
            103: {
                estimateId: 103,
                status: 'Lost',
                fcoEntries: [],
                laborRows: [{ position: 'Boilermaker', craftCode: 'BOIL', scheduleJson: '{"2026-07-20":5}' }],
            },
        },
        staffingPlans: [
            {
                staffingPlanId: 201,
                staffingPlanNumber: 'SP-H-26-0001-BPH',
                name: 'BP Walkdown July',
                client: 'BP',
                status: 'Active',
                startDate: '2026-07-01',
                endDate: '2026-07-20',
                roughLaborTotal: 15000,
                convertedEstimateId: null,
                updatedAt: nowIso(),
            },
            {
                staffingPlanId: 202,
                staffingPlanNumber: 'SP-H-26-0002-BPH',
                name: 'BP Walkdown Converted',
                client: 'BP',
                status: 'Converted',
                startDate: '2026-08-01',
                endDate: '2026-08-10',
                roughLaborTotal: 9000,
                convertedEstimateId: 102,
                updatedAt: nowIso(),
            },
        ],
        staffingDetails: {
            201: {
                staffingPlanId: 201,
                status: 'Active',
                convertedEstimateId: null,
                laborRows: [{ position: 'Welder', craftCode: 'WELD', scheduleJson: '{"2026-07-01":2,"2026-07-10":3}' }],
            },
            202: {
                staffingPlanId: 202,
                status: 'Converted',
                convertedEstimateId: 102,
                laborRows: [{ position: 'Pipefitter', craftCode: 'PIPE', scheduleJson: '{"2026-08-01":2}' }],
            },
        },
        lastRateBookCreatePayload: null,
        lastRateBookUpdatePayload: null,
    };

    await page.route('**/api/auth/login', async route => {
        await json(route, 200, {
            token: 'mock.token.value',
            username: 'dev.user',
            companyCode: 'CSL',
            roles: ['Administrator'],
        });
    });

    await page.route('**/api/v1/**', async route => {
        const request = route.request();
        const method = request.method().toUpperCase();
        const url = new URL(request.url());
        const path = url.pathname.toLowerCase();

        if (method === 'GET' && path.endsWith('/api/v1/rate-books/for-client')) {
            const client = (url.searchParams.get('client') ?? '').trim();
            const city = url.searchParams.get('city');
            const stateCode = url.searchParams.get('state');
            const rows = state.rateBooks
                .map(book => ({
                    rateBookId: book.rateBookId,
                    name: book.name,
                    client: book.client,
                    clientCode: book.clientCode,
                    city: book.city,
                    state: book.state,
                    score: scoreRateBook(book, client, city, stateCode),
                }))
                .filter(row => row.score > 0)
                .sort((a, b) => b.score - a.score);
            return json(route, 200, rows);
        }

        if (method === 'GET' && path.endsWith('/api/v1/rate-books')) {
            const list = state.rateBooks.map(book => ({
                rateBookId: book.rateBookId,
                name: book.name,
                client: book.client,
                clientCode: book.clientCode,
                city: book.city,
                state: book.state,
                isStandardBaseline: book.isStandardBaseline,
                effectiveDate: book.effectiveDate,
                updatedAt: book.updatedAt,
            }));
            return json(route, 200, list);
        }

        const rateBookIdMatch = path.match(/\/api\/v1\/rate-books\/(\d+)$/);
        if (rateBookIdMatch && method === 'GET') {
            const id = Number(rateBookIdMatch[1]);
            const found = state.rateBooks.find(book => book.rateBookId === id);
            if (!found) return json(route, 404, { message: 'Not found' });
            return json(route, 200, found);
        }

        if (method === 'POST' && path.endsWith('/api/v1/rate-books')) {
            const body = request.postDataJSON() as any;
            state.lastRateBookCreatePayload = body;
            const nextId = Math.max(...state.rateBooks.map(book => book.rateBookId)) + 1;
            const created: RateBook = {
                rateBookId: nextId,
                name: String(body.name ?? '').trim(),
                client: body.client ?? null,
                clientCode: body.clientCode ?? null,
                city: body.city ?? null,
                state: body.state ?? null,
                isStandardBaseline: Boolean(body.isStandardBaseline),
                effectiveDate: body.effectiveDate ?? null,
                updatedAt: nowIso(),
                laborRates: (body.laborRates ?? []).map((row: any) => ({
                    position: String(row.position ?? ''),
                    laborType: String(row.laborType ?? 'Direct'),
                    craftCode: row.craftCode ?? null,
                    navCode: row.navCode ?? null,
                    stRate: Number(row.stRate ?? 0),
                    otRate: Number(row.otRate ?? 0),
                    dtRate: Number(row.dtRate ?? 0),
                })),
            };
            state.rateBooks.push(created);
            return json(route, 201, { rateBookId: nextId });
        }

        if (rateBookIdMatch && method === 'PUT') {
            const id = Number(rateBookIdMatch[1]);
            const body = request.postDataJSON() as any;
            state.lastRateBookUpdatePayload = body;
            const index = state.rateBooks.findIndex(book => book.rateBookId === id);
            if (index === -1) return json(route, 404, { message: 'Not found' });

            state.rateBooks[index] = {
                ...state.rateBooks[index],
                name: body.name ?? state.rateBooks[index].name,
                client: body.client ?? null,
                clientCode: body.clientCode ?? null,
                city: body.city ?? null,
                state: body.state ?? null,
                isStandardBaseline: Boolean(body.isStandardBaseline),
                effectiveDate: body.effectiveDate ?? null,
                updatedAt: nowIso(),
                laborRates: (body.laborRates ?? []).map((row: any) => ({
                    position: String(row.position ?? ''),
                    laborType: String(row.laborType ?? 'Direct'),
                    craftCode: row.craftCode ?? null,
                    navCode: row.navCode ?? null,
                    stRate: Number(row.stRate ?? 0),
                    otRate: Number(row.otRate ?? 0),
                    dtRate: Number(row.dtRate ?? 0),
                })),
            };
            return noContent(route);
        }

        const rateBookCloneMatch = path.match(/\/api\/v1\/rate-books\/(\d+)\/clone$/);
        if (rateBookCloneMatch && method === 'POST') {
            const sourceId = Number(rateBookCloneMatch[1]);
            const source = state.rateBooks.find(book => book.rateBookId === sourceId);
            if (!source) return json(route, 404, { message: 'Not found' });
            const body = request.postDataJSON() as any;
            const nextId = Math.max(...state.rateBooks.map(book => book.rateBookId)) + 1;
            const cloned: RateBook = {
                ...source,
                rateBookId: nextId,
                name: String(body.name ?? `${source.name} Copy`),
                client: body.client ?? source.client,
                clientCode: body.clientCode ?? source.clientCode,
                city: body.city ?? source.city,
                state: body.state ?? source.state,
                isStandardBaseline: false,
                updatedAt: nowIso(),
            };
            state.rateBooks.push(cloned);
            return json(route, 200, { rateBookId: nextId, name: cloned.name });
        }

        if (rateBookIdMatch && method === 'DELETE') {
            const id = Number(rateBookIdMatch[1]);
            state.rateBooks = state.rateBooks.filter(book => book.rateBookId !== id);
            return noContent(route);
        }

        if (method === 'GET' && path.endsWith('/api/v1/estimates')) {
            return json(route, 200, paginate(state.estimates, url));
        }

        const estimateIdMatch = path.match(/\/api\/v1\/estimates\/(\d+)$/);
        if (estimateIdMatch && method === 'GET') {
            const id = Number(estimateIdMatch[1]);
            const found = state.estimateDetails[id];
            if (!found) return json(route, 404, { message: 'Not found' });
            return json(route, 200, found);
        }

        if (method === 'GET' && path.endsWith('/api/v1/staffing-plans')) {
            return json(route, 200, paginate(state.staffingPlans, url));
        }

        const staffingIdMatch = path.match(/\/api\/v1\/staffing-plans\/(\d+)$/);
        if (staffingIdMatch && method === 'GET') {
            const id = Number(staffingIdMatch[1]);
            const found = state.staffingDetails[id];
            if (!found) return json(route, 404, { message: 'Not found' });
            return json(route, 200, found);
        }

        return json(route, 404, {
            title: 'Not Found',
            status: 404,
            detail: `No mock handler for ${method} ${url.pathname}`,
        });
    });

    return state;
}

test.describe('Estimating - rate books', () => {
    test('[REQ-RATE-001, REQ-RATE-002, REQ-RATE-003, REQ-RATE-004] validates lookup/create/clone/delete flows and payload correctness', async ({ page }) => {
        await primeAuth(page);
        const state = await installEstimatingMocks(page);

        await page.goto('/estimating/rate-books');
        await expect(page.locator('[data-testid="rb-sidebar"]')).toBeVisible();
        await expect(page.locator('[data-testid^="rb-labor-table-"]').first()).toBeVisible();

        const nameInput = page.locator('input[data-testid="rb-name"]');
        await expect(nameInput).toHaveValue('CSL Standard');

        await page.locator('[data-testid="rb-duplicate"]').click();
        const cloneDialog = page.locator('[data-testid="rb-clone-dialog"]');
        await expect(cloneDialog).toBeVisible();
        await cloneDialog.locator('[data-testid="rb-clone-name"]').fill('BP Pasadena TX Clone');
        await cloneDialog.locator('button:has-text("Duplicate")').click();

        await expect(nameInput).toHaveValue('BP Pasadena TX Clone');
        await expect.poll(() => state.rateBooks.some(book => book.name === 'BP Pasadena TX Clone')).toBeTruthy();

        const lookupClientInput = page.locator('input[data-testid="rb-lookup-client"]');
        const lookupCityInput = page.locator('input[data-testid="rb-lookup-city"]');
        const lookupStateInput = page.locator('input[data-testid="rb-lookup-state"]');
        await lookupClientInput.fill('BP');
        await lookupCityInput.fill('Pasadena');
        await lookupStateInput.fill('TX');
        await page.locator('[data-testid="rb-lookup-find"]').click();
        await expect(page.locator('[data-testid="rb-lookup-table"]')).toBeVisible();
        const houstonRow = page
            .locator('[data-testid="rb-lookup-table"] tbody tr')
            .filter({ hasText: 'BP Houston TX' });
        await expect(houstonRow).toBeVisible();
        await houstonRow.locator('button:has-text("Load")').click();
        await expect(nameInput).toHaveValue('BP Houston TX');

        await expect.poll(() => state.rateBooks.find(book => book.name === 'BP Pasadena TX Clone')?.rateBookId ?? null).not.toBeNull();
        const cloneId = state.rateBooks.find(book => book.name === 'BP Pasadena TX Clone')!.rateBookId;
        await page.locator(`[data-testid="rb-item-${cloneId}"]`).click();
        await expect(nameInput).toHaveValue('BP Pasadena TX Clone');

        await page.locator('[data-testid="rb-delete"]').click();
        const confirmDialog = page.locator('[data-testid="rb-delete-dialog"]');
        await expect(confirmDialog).toBeVisible();
        await confirmDialog.locator('button:has-text("Delete")').click();
        await expect.poll(() => state.rateBooks.some(book => book.name === 'BP Pasadena TX Clone')).toBeFalsy();

        await page.screenshot({ path: 'test-results/rate-book-e2e.png', fullPage: true });
    });
});

test.describe('Estimating - analytics', () => {
    test('[REQ-AN-001, REQ-AN-002, REQ-MF-002, REQ-MF-004] reconciles revenue and manpower calculations from source data', async ({ page }) => {
        await primeAuth(page);
        await installEstimatingMocks(page);

        await page.goto('/estimating/analytics/revenue');
        const financialDash = page.locator('.fd-section').first();
        await expect(financialDash).toContainText('FINANCIAL DASHBOARD');
        await expect(financialDash).toContainText('AWARDED REVENUE');
        await expect(financialDash).toContainText('$100K');
        await expect(financialDash).toContainText('PENDING PIPELINE');
        await expect(financialDash).toContainText('$50K');
        await expect(financialDash).toContainText('LOST REVENUE');
        await expect(financialDash).toContainText('$20K');
        await expect(financialDash).toContainText('TOTAL PIPELINE');
        await expect(financialDash).toContainText('$170K');
        await expect(financialDash).toContainText('WEIGHTED PIPELINE');
        await expect(financialDash).toContainText('$125K');

        await expect(page.locator('[data-testid="fd-revenue-chart"]')).toBeVisible();
        await page.locator('[data-testid="fd-export"]').click();

        await page.screenshot({ path: 'test-results/revenue-forecast-e2e.png', fullPage: true });

        await page.goto('/estimating/analytics/manpower');
        await expect(page.locator('[data-testid="mp-kpi-current"]')).toContainText('Run forecast to view');
        await page.locator('[data-testid="mp-run"]').click();
        await expect(page.locator('[data-testid="mp-kpi-current"]')).toContainText('0');
        await expect(page.locator('[data-testid="mp-kpi-peak"]')).toContainText('4');
        await expect(page.locator('[data-testid="mp-position-breakdown"]')).toContainText('Pipefitter');
        await expect(page.locator('[data-testid="mp-position-breakdown"]')).toContainText('Welder');
        await expect(page.locator('[data-testid="mp-chart"]')).toBeVisible();

        await page.screenshot({ path: 'test-results/manpower-forecast-e2e.png', fullPage: true });
    });
});
