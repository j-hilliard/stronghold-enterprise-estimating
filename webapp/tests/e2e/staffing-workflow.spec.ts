/**
 * Staffing plan workflow tests (mocked suite).
 * REQ-SP-001 through REQ-SP-007, REQ-STAT-002, REQ-STAT-004.
 */
import { expect, Page, Route, test } from '@playwright/test';

type StaffingListItem = {
    staffingPlanId: number;
    staffingPlanNumber: string;
    name: string;
    client: string;
    status: string;
    branch?: string | null;
    city?: string | null;
    state?: string | null;
    startDate?: string | null;
    endDate?: string | null;
    roughLaborTotal?: number;
    convertedEstimateId?: number | null;
    laborPreview?: string[];
    laborMoreCount?: number;
    updatedAt: string;
};

type StaffingDetail = {
    staffingPlanId: number;
    staffingPlanNumber: string;
    convertedEstimateId?: number | null;
    name: string;
    client: string;
    clientCode?: string | null;
    branch?: string | null;
    city?: string | null;
    state?: string | null;
    jobLetter?: string | null;
    status: string;
    shift: string;
    hoursPerShift: number;
    days: number;
    startDate?: string | null;
    endDate?: string | null;
    otMethod: string;
    dtWeekends: boolean;
    roughLaborTotal?: number;
    laborRows: Array<{
        position: string;
        laborType: string;
        shift: string;
        craftCode?: string | null;
        navCode?: string | null;
        stRate: number;
        otRate: number;
        dtRate: number;
        scheduleJson?: string | null;
        stHours?: number;
        otHours?: number;
        dtHours?: number;
        subtotal?: number;
        sortOrder?: number;
    }>;
};

type MockState = {
    plans: StaffingListItem[];
    details: Record<number, StaffingDetail>;
    lastCreatePayload: any | null;
    lastUpdatePayload: any | null;
    lastLaborPayload: any[] | null;
    convertCalls: number;
};

const JSON_HEADERS = {
    'access-control-allow-origin': '*',
    'content-type': 'application/json',
};

const MOCK_JWT =
    'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjQxMDI0NDQ4MDB9.signature';

function nowIso() {
    return new Date('2026-03-25T12:00:00.000Z').toISOString();
}

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

function buildPreview(rows: StaffingDetail['laborRows']) {
    const grouped = new Map<string, { position: string; shift: string; count: number }>();
    for (const row of rows) {
        const position = row.position?.trim() || 'Position';
        const shift = row.shift?.trim() || '';
        const key = `${position}|${shift}`;
        const entry = grouped.get(key);
        if (entry) {
            entry.count += 1;
        } else {
            grouped.set(key, { position, shift, count: 1 });
        }
    }

    const all = Array.from(grouped.values());
    const preview = all.slice(0, 4).map(row => `${row.count}x ${row.position}${row.shift ? ` (${row.shift})` : ''}`);
    return {
        laborPreview: preview,
        laborMoreCount: Math.max(0, all.length - preview.length),
    };
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

async function installStaffingMocks(page: Page): Promise<MockState> {
    const state: MockState = {
        plans: [
            {
                staffingPlanId: 201,
                staffingPlanNumber: 'SP-S-26-0001-SHL',
                name: 'Shell Deer Park Turnaround',
                client: 'Shell',
                status: 'Approved',
                branch: '300',
                city: 'Deer Park',
                state: 'TX',
                startDate: '2026-03-19',
                endDate: '2026-04-02',
                roughLaborTotal: 129780,
                convertedEstimateId: null,
                updatedAt: nowIso(),
                laborPreview: ['1x Project Manager (Day)', '1x General Foreman (Day)'],
                laborMoreCount: 2,
            },
            {
                staffingPlanId: 202,
                staffingPlanNumber: 'SP-S-26-0002-BP',
                name: 'BP Texas City Piping',
                client: 'BP',
                status: 'Active',
                branch: '200',
                city: 'Texas City',
                state: 'TX',
                startDate: '2026-03-04',
                endDate: '2026-03-19',
                roughLaborTotal: 45440,
                convertedEstimateId: null,
                updatedAt: nowIso(),
                laborPreview: ['1x General Foreman (Day)', '1x Electrician Journeyman (Day)'],
                laborMoreCount: 1,
            },
            {
                staffingPlanId: 203,
                staffingPlanNumber: 'SP-S-26-0003-VLO',
                name: 'Valero Port Arthur Maintenance',
                client: 'Valero',
                status: 'Converted',
                branch: '300',
                city: 'Port Arthur',
                state: 'TX',
                startDate: '2026-04-03',
                endDate: '2026-04-13',
                roughLaborTotal: 52580,
                convertedEstimateId: 901,
                updatedAt: nowIso(),
                laborPreview: ['1x Project Manager (Day)', '1x Pipefitter Journeyman (Day)'],
                laborMoreCount: 1,
            },
        ],
        details: {
            201: {
                staffingPlanId: 201,
                staffingPlanNumber: 'SP-S-26-0001-SHL',
                convertedEstimateId: null,
                name: 'Shell Deer Park Turnaround',
                client: 'Shell',
                clientCode: 'SHL',
                branch: '300',
                city: 'Deer Park',
                state: 'TX',
                jobLetter: 'S',
                status: 'Approved',
                shift: 'Day',
                hoursPerShift: 10,
                days: 15,
                startDate: '2026-03-19T00:00:00',
                endDate: '2026-04-02T00:00:00',
                otMethod: 'daily8_weekly40',
                dtWeekends: false,
                roughLaborTotal: 129780,
                laborRows: [
                    {
                        position: 'Project Manager',
                        laborType: 'Indirect',
                        shift: 'Day',
                        stRate: 120,
                        otRate: 180,
                        dtRate: 240,
                        scheduleJson: '{"2026-03-19":1,"2026-03-20":1}',
                        stHours: 20,
                        otHours: 0,
                        dtHours: 0,
                        subtotal: 2400,
                        sortOrder: 0,
                    },
                ],
            },
            202: {
                staffingPlanId: 202,
                staffingPlanNumber: 'SP-S-26-0002-BP',
                convertedEstimateId: null,
                name: 'BP Texas City Piping',
                client: 'BP',
                clientCode: 'BPT',
                branch: '200',
                city: 'Texas City',
                state: 'TX',
                jobLetter: 'S',
                status: 'Active',
                shift: 'Both',
                hoursPerShift: 12,
                days: 8,
                startDate: '2026-03-04T00:00:00',
                endDate: '2026-03-11T00:00:00',
                otMethod: 'daily8_weekly40',
                dtWeekends: false,
                roughLaborTotal: 45440,
                laborRows: [],
            },
            203: {
                staffingPlanId: 203,
                staffingPlanNumber: 'SP-S-26-0003-VLO',
                convertedEstimateId: 901,
                name: 'Valero Port Arthur Maintenance',
                client: 'Valero',
                clientCode: 'VLO',
                branch: '300',
                city: 'Port Arthur',
                state: 'TX',
                jobLetter: 'S',
                status: 'Converted',
                shift: 'Day',
                hoursPerShift: 10,
                days: 11,
                startDate: '2026-04-03T00:00:00',
                endDate: '2026-04-13T00:00:00',
                otMethod: 'daily8_weekly40',
                dtWeekends: false,
                roughLaborTotal: 52580,
                laborRows: [],
            },
        },
        lastCreatePayload: null,
        lastUpdatePayload: null,
        lastLaborPayload: null,
        convertCalls: 0,
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

        if (method === 'GET' && path.endsWith('/api/v1/staffing-plans')) {
            let filtered = [...state.plans];
            const search = (url.searchParams.get('search') ?? '').trim().toLowerCase();
            const status = (url.searchParams.get('status') ?? '').trim();
            const branch = (url.searchParams.get('branch') ?? '').trim();

            if (search) {
                filtered = filtered.filter(plan =>
                    [
                        plan.staffingPlanNumber,
                        plan.name,
                        plan.client,
                        plan.city,
                        plan.state,
                    ]
                        .filter(Boolean)
                        .join(' ')
                        .toLowerCase()
                        .includes(search),
                );
            }

            if (status) {
                filtered = filtered.filter(plan => plan.status === status);
            }

            if (branch) {
                filtered = filtered.filter(plan => (plan.branch ?? '') === branch);
            }

            filtered.sort((a, b) => b.updatedAt.localeCompare(a.updatedAt));
            return json(route, 200, paginate(filtered, url));
        }

        const staffingIdMatch = path.match(/\/api\/v1\/staffing-plans\/(\d+)$/);
        if (staffingIdMatch && method === 'GET') {
            const id = Number(staffingIdMatch[1]);
            const detail = state.details[id];
            if (!detail) return json(route, 404, { message: 'Not found' });
            return json(route, 200, detail);
        }

        if (method === 'POST' && path.endsWith('/api/v1/staffing-plans')) {
            const body = request.postDataJSON() as any;
            state.lastCreatePayload = body;
            const nextId = Math.max(...state.plans.map(plan => plan.staffingPlanId)) + 1;
            const staffingPlanNumber = `SP-S-26-${String(nextId).padStart(4, '0')}-${String(body.clientCode ?? 'GEN').toUpperCase()}`;

            const detail: StaffingDetail = {
                staffingPlanId: nextId,
                staffingPlanNumber,
                convertedEstimateId: null,
                name: String(body.name ?? ''),
                client: String(body.client ?? ''),
                clientCode: body.clientCode ?? null,
                branch: body.branch ?? null,
                city: body.city ?? null,
                state: body.state ?? null,
                jobLetter: body.jobLetter ?? null,
                status: String(body.status ?? 'Draft'),
                shift: String(body.shift ?? 'Day'),
                hoursPerShift: Number(body.hoursPerShift ?? 10),
                days: Number(body.days ?? 0),
                startDate: body.startDate ? `${body.startDate}T00:00:00` : null,
                endDate: body.endDate ? `${body.endDate}T00:00:00` : null,
                otMethod: String(body.otMethod ?? 'daily8_weekly40'),
                dtWeekends: Boolean(body.dtWeekends),
                roughLaborTotal: 0,
                laborRows: [],
            };

            const preview = buildPreview(detail.laborRows);
            state.details[nextId] = detail;
            state.plans.unshift({
                staffingPlanId: nextId,
                staffingPlanNumber,
                name: detail.name,
                client: detail.client,
                status: detail.status,
                branch: detail.branch,
                city: detail.city,
                state: detail.state,
                startDate: detail.startDate?.slice(0, 10) ?? null,
                endDate: detail.endDate?.slice(0, 10) ?? null,
                roughLaborTotal: 0,
                convertedEstimateId: null,
                updatedAt: nowIso(),
                laborPreview: preview.laborPreview,
                laborMoreCount: preview.laborMoreCount,
            });

            return json(route, 201, { staffingPlanId: nextId, staffingPlanNumber });
        }

        if (staffingIdMatch && method === 'PUT') {
            const id = Number(staffingIdMatch[1]);
            const body = request.postDataJSON() as any;
            state.lastUpdatePayload = body;

            const detail = state.details[id];
            if (!detail) return json(route, 404, { message: 'Not found' });

            detail.name = String(body.name ?? detail.name);
            detail.client = String(body.client ?? detail.client);
            detail.clientCode = body.clientCode ?? detail.clientCode;
            detail.branch = body.branch ?? detail.branch;
            detail.city = body.city ?? detail.city;
            detail.state = body.state ?? detail.state;
            detail.jobLetter = body.jobLetter ?? detail.jobLetter;
            detail.status = body.status ?? detail.status;
            detail.shift = body.shift ?? detail.shift;
            detail.hoursPerShift = Number(body.hoursPerShift ?? detail.hoursPerShift);
            detail.days = Number(body.days ?? detail.days);
            detail.startDate = body.startDate ? `${body.startDate}T00:00:00` : null;
            detail.endDate = body.endDate ? `${body.endDate}T00:00:00` : null;
            detail.otMethod = body.otMethod ?? detail.otMethod;
            detail.dtWeekends = Boolean(body.dtWeekends);

            const plan = state.plans.find(row => row.staffingPlanId === id);
            if (plan) {
                plan.name = detail.name;
                plan.client = detail.client;
                plan.branch = detail.branch;
                plan.city = detail.city;
                plan.state = detail.state;
                plan.status = detail.status;
                plan.startDate = detail.startDate?.slice(0, 10) ?? null;
                plan.endDate = detail.endDate?.slice(0, 10) ?? null;
                plan.updatedAt = nowIso();
            }

            return noContent(route);
        }

        const staffingLaborMatch = path.match(/\/api\/v1\/staffing-plans\/(\d+)\/labor$/);
        if (staffingLaborMatch && method === 'POST') {
            const id = Number(staffingLaborMatch[1]);
            const rows = request.postDataJSON() as any[];
            state.lastLaborPayload = rows;

            const detail = state.details[id];
            if (!detail) return json(route, 404, { message: 'Not found' });

            detail.laborRows = rows.map((row, index) => ({
                position: String(row.position ?? ''),
                laborType: String(row.laborType ?? 'Direct'),
                shift: String(row.shift ?? 'Day'),
                craftCode: row.craftCode ?? null,
                navCode: row.navCode ?? null,
                stRate: Number(row.stRate ?? 0),
                otRate: Number(row.otRate ?? 0),
                dtRate: Number(row.dtRate ?? 0),
                scheduleJson: row.scheduleJson ?? null,
                stHours: Number(row.stHours ?? 0),
                otHours: Number(row.otHours ?? 0),
                dtHours: Number(row.dtHours ?? 0),
                subtotal: Number(row.subtotal ?? 0),
                sortOrder: index,
            }));

            detail.roughLaborTotal = detail.laborRows.reduce((sum, row) => sum + Number(row.subtotal ?? 0), 0);

            const plan = state.plans.find(row => row.staffingPlanId === id);
            if (plan) {
                const preview = buildPreview(detail.laborRows);
                plan.roughLaborTotal = detail.roughLaborTotal;
                plan.laborPreview = preview.laborPreview;
                plan.laborMoreCount = preview.laborMoreCount;
                plan.updatedAt = nowIso();
            }

            return noContent(route);
        }

        const staffingDuplicateMatch = path.match(/\/api\/v1\/staffing-plans\/(\d+)\/duplicate$/);
        if (staffingDuplicateMatch && method === 'POST') {
            const id = Number(staffingDuplicateMatch[1]);
            const source = state.details[id];
            if (!source) return json(route, 404, { message: 'Not found' });

            const nextId = Math.max(...state.plans.map(plan => plan.staffingPlanId)) + 1;
            const staffingPlanNumber = `SP-S-26-${String(nextId).padStart(4, '0')}-${String(source.clientCode ?? 'GEN').toUpperCase()}`;

            const duplicateDetail: StaffingDetail = {
                ...source,
                staffingPlanId: nextId,
                staffingPlanNumber,
                convertedEstimateId: null,
                name: source.name ? `${source.name} (Copy)` : 'Copy',
                status: 'Draft',
                laborRows: source.laborRows.map(row => ({ ...row })),
            };

            const preview = buildPreview(duplicateDetail.laborRows);
            state.details[nextId] = duplicateDetail;
            state.plans.unshift({
                staffingPlanId: nextId,
                staffingPlanNumber,
                name: duplicateDetail.name,
                client: duplicateDetail.client,
                status: duplicateDetail.status,
                branch: duplicateDetail.branch,
                city: duplicateDetail.city,
                state: duplicateDetail.state,
                startDate: duplicateDetail.startDate?.slice(0, 10) ?? null,
                endDate: duplicateDetail.endDate?.slice(0, 10) ?? null,
                roughLaborTotal: duplicateDetail.roughLaborTotal ?? 0,
                convertedEstimateId: null,
                updatedAt: nowIso(),
                laborPreview: preview.laborPreview,
                laborMoreCount: preview.laborMoreCount,
            });

            return json(route, 200, { staffingPlanId: nextId, staffingPlanNumber });
        }

        const staffingConvertMatch = path.match(/\/api\/v1\/staffing-plans\/(\d+)\/convert$/);
        if (staffingConvertMatch && method === 'POST') {
            const id = Number(staffingConvertMatch[1]);
            const detail = state.details[id];
            if (!detail) return json(route, 404, { message: 'Not found' });
            if (detail.convertedEstimateId) {
                return json(route, 400, { message: 'This staffing plan has already been converted.' });
            }

            state.convertCalls += 1;
            detail.convertedEstimateId = 950 + state.convertCalls;
            detail.status = 'Converted';

            const plan = state.plans.find(row => row.staffingPlanId === id);
            if (plan) {
                plan.convertedEstimateId = detail.convertedEstimateId;
                plan.status = 'Converted';
                plan.updatedAt = nowIso();
            }

            return json(route, 200, {
                estimateId: detail.convertedEstimateId,
                estimateNumber: `S-26-${String(detail.convertedEstimateId).padStart(4, '0')}-${detail.clientCode ?? 'GEN'}`,
            });
        }

        if (staffingIdMatch && method === 'DELETE') {
            const id = Number(staffingIdMatch[1]);
            const plan = state.plans.find(row => row.staffingPlanId === id);
            if (!plan) return json(route, 404, { message: 'Not found' });
            if (plan.convertedEstimateId) {
                return json(route, 400, { message: 'Converted staffing plans cannot be deleted.' });
            }

            state.plans = state.plans.filter(row => row.staffingPlanId !== id);
            delete state.details[id];
            return noContent(route);
        }

        return json(route, 404, {
            title: 'Not Found',
            status: 404,
            detail: `No mock handler for ${method} ${url.pathname}`,
        });
    });

    return state;
}

test.describe('Staffing plans', () => {
    test('[REQ-SP-001, REQ-SP-004, REQ-STAT-002, REQ-STAT-004] list toolbar, filters, duplicate/delete, and convert behavior', async ({ page }) => {
        await primeAuth(page);
        const state = await installStaffingMocks(page);

        await page.goto('/estimating/staffing-plans');

        await expect(page.locator('[data-testid="sp-toolbar"]')).toBeVisible();
        await expect(page.locator('[data-testid="sp-count"]')).toContainText('Plans: 3');
        await expect(page.locator('[data-testid="sp-card-201"]')).toBeVisible();
        await expect(page.locator('[data-testid="sp-pills-201"]')).toContainText('Project Manager');

        await page.locator('[data-testid="sp-status"]').click();
        await page.locator('.p-dropdown-item', { hasText: 'Converted' }).click();
        await expect(page.locator('[data-testid="sp-card-203"]')).toBeVisible();
        await expect(page.locator('[data-testid="sp-card-201"]')).toHaveCount(0);

        await page.locator('[data-testid="sp-status"]').click();
        await page.locator('.p-dropdown-item', { hasText: 'All Status' }).click();

        await page.locator('[data-testid="sp-branch"]').click();
        await page.locator('.p-dropdown-item', { hasText: '200' }).click();
        await expect(page.locator('[data-testid="sp-card-202"]')).toBeVisible();
        await expect(page.locator('[data-testid="sp-card-201"]')).toHaveCount(0);

        await page.locator('[data-testid="sp-branch"]').click();
        await page.locator('.p-dropdown-item', { hasText: 'All Branches' }).click();

        await page.locator('[data-testid="sp-quick"]').fill('Port Arthur');
        await expect(page.locator('[data-testid="sp-card-203"]')).toBeVisible();
        await expect(page.locator('[data-testid="sp-card-201"]')).toHaveCount(0);

        await page.locator('[data-testid="sp-quick"]').fill('');
        await expect(page.locator('[data-testid="sp-card-201"]')).toBeVisible();

        await page.locator('[data-testid="sp-duplicate-201"]').click();
        await expect
            .poll(() => state.plans.some(plan => plan.name.includes('(Copy)')))
            .toBeTruthy();

        const duplicate = state.plans.find(plan => plan.name.includes('(Copy)'))!;
        await expect(page.locator(`[data-testid="sp-card-${duplicate.staffingPlanId}"]`)).toBeVisible();
        await page.locator(`[data-testid="sp-delete-${duplicate.staffingPlanId}"]`).click();
        await expect(page.locator('[data-testid="sp-delete-dialog"]')).toBeVisible();
        await page.locator('[data-testid="sp-delete-dialog"] button:has-text("Delete")').click();
        await expect
            .poll(() => state.plans.some(plan => plan.staffingPlanId === duplicate.staffingPlanId))
            .toBeFalsy();

        await page.locator('[data-testid="sp-convert-201"]').click();
        await expect
            .poll(() => state.plans.find(plan => plan.staffingPlanId === 201)?.status)
            .toBe('Converted');
        await expect(page.locator('[data-testid="sp-converted-201"]')).toBeVisible();

        await page.locator('[data-testid="sp-refresh"]').click();
        await expect(page.locator('[data-testid="sp-count"]')).toContainText(`Plans: ${state.plans.length}`);

        await page.screenshot({ path: 'test-results/staffing-list-e2e.png', fullPage: true });
    });

    test('[REQ-SP-001, REQ-SP-004, REQ-SP-005, REQ-SP-006] new staffing form saves, writes labor rows, and converts to estimate', async ({ page }) => {
        await primeAuth(page);
        const state = await installStaffingMocks(page);

        await page.goto('/estimating/staffing-plans/new');

        await page.locator('[data-testid="sp-name"]').fill('Shell Pasadena Walkdown');
        await page.locator('[data-testid="sp-client"]').fill('Shell');
        await page.locator('[data-testid="sp-client-code"]').fill('SHL');
        await page.locator('[data-testid="sp-job-letter"]').fill('S');
        await page.locator('[data-testid="sp-branch"]').fill('300');
        await page.locator('[data-testid="sp-city"]').fill('Pasadena');
        await page.locator('[data-testid="sp-state"]').fill('TX');
        await page.locator('[data-testid="sp-start-date"]').fill('2026-06-06');
        await page.locator('[data-testid="sp-end-date"]').fill('2026-06-13');

        await expect(page.locator('[data-testid="sp-days"] input')).toHaveValue('8');

        await page.locator('[data-testid="labor-add-row"]').click();
        const addDialog = page.getByRole('dialog', { name: 'Add Employee' });
        await expect(addDialog).toBeVisible();
        await addDialog.getByPlaceholder('e.g. Pipefitter Foreman').fill('General Foreman');
        await addDialog.getByRole('button', { name: /^Add$/ }).click();
        await expect(addDialog).toBeHidden();
        await expect(page.locator('[data-testid="labor-table"] tbody tr')).toHaveCount(1);

        await page.locator('[data-testid="sp-save"]').click();

        await expect(page).toHaveURL(/\/estimating\/staffing-plans\/\d+$/);
        await expect.poll(() => state.lastCreatePayload?.name ?? '').toBe('Shell Pasadena Walkdown');
        await expect.poll(() => Array.isArray(state.lastLaborPayload)).toBeTruthy();

        await page.locator('[data-testid="sp-convert"]').click();
        await expect(page.locator('[data-testid="sp-convert-dialog"]')).toBeVisible();
        await page.locator('[data-testid="sp-convert-confirm"]').click();

        await expect(page.locator('[data-testid="sp-converted-message"]')).toBeVisible();
        await expect(page.locator('[data-testid="sp-view-estimate"]')).toBeVisible();

        await page.screenshot({ path: 'test-results/staffing-form-e2e.png', fullPage: true });
    });
});
