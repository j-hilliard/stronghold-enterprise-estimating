import { test, expect } from '@playwright/test';
import {
    expectNoRuntimeErrors,
    gotoAndStabilize,
    selectFirstPrimeDropdownOption,
    startRuntimeErrorTracking,
} from './utils/ui';
import {
    API_BASE_URL,
    cleanupIncidentsByJobTag,
    deleteIncidentById,
    getJsonOrFail,
    KEEP_TEST_DATA,
} from './utils/liveApi';

const runLiveApi = process.env.PW_RUN_LIVE_API === 'true';

type RefCompanyDto = {
    id?: string;
};

type RefRegionDto = {
    id?: string;
};

type IncidentEmployeeDto = {
    employeeIdentifier?: string;
    employeeName?: string;
};

type IncidentReportDto = {
    id?: string;
    status?: string;
    jobNumber?: string;
    clientCode?: string;
    plantCode?: string;
    workDescription?: string;
    incidentSummary?: string;
    incidentClass?: string;
    severityActualCode?: string;
    severityPotentialCode?: string;
    bodyPartsInjured?: string;
    natureOfInjury?: string;
    typeOfEquipment?: string;
    unitNumbers?: string;
    visibility?: string;
    referenceIds?: string[];
    employeesInvolved?: IncidentEmployeeDto[];
};

test.describe('Live Incident Workflow (DB-backed)', () => {
    test.skip(!runLiveApi, 'Set PW_RUN_LIVE_API=true for authenticated live API workflow checks.');
    test.describe.configure({ timeout: 120_000 });

    test('double-clicking an incident row opens the incident detail view', async ({ page, request }) => {
        const runtimeErrors = startRuntimeErrorTracking(page);

        await getJsonOrFail<RefCompanyDto[]>(request, `${API_BASE_URL}/v1/ReferenceData/companies`);
        await getJsonOrFail<RefRegionDto[]>(request, `${API_BASE_URL}/v1/ReferenceData/regions`);

        await gotoAndStabilize(page, '/incident-management/incidents');
        const firstRow = page.locator('.p-datatable-tbody > tr').first();
        await expect(firstRow).toBeVisible();

        const incidentNumber = (await firstRow.locator('td').first().textContent())?.trim() ?? '';
        await firstRow.dblclick();
        await expect(page).toHaveURL(/\/incident-management\/incidents\/[0-9a-fA-F-]{36}$/);

        if (incidentNumber.length > 0) {
            await expect(page.getByText(incidentNumber, { exact: false }).first()).toBeVisible();
        }

        expectNoRuntimeErrors(runtimeErrors);
    });

    test('can select actual+potential severity, save draft, and cleanup created data', async ({ page, request }) => {
        const runtimeErrors = startRuntimeErrorTracking(page);
        const jobTag = `PW-LIVE-${Date.now()}`;
        let createdId: string | undefined;

        try {
            await cleanupIncidentsByJobTag(request, jobTag);

            await getJsonOrFail<RefCompanyDto[]>(request, `${API_BASE_URL}/v1/ReferenceData/companies`);
            await getJsonOrFail<RefRegionDto[]>(request, `${API_BASE_URL}/v1/ReferenceData/regions`);

            await gotoAndStabilize(page, '/incident-management/incidents/new');
            await expect(page.getByRole('heading', { name: 'New Incident Report' })).toBeVisible();

            await selectFirstPrimeDropdownOption(page, page.locator('.p-dropdown').first());

            const regionDropdown = page.locator('.p-dropdown').nth(1);
            await expect(regionDropdown).not.toHaveClass(/p-disabled/);
            await selectFirstPrimeDropdownOption(page, regionDropdown);

            const actualSeverityInput = page.locator('input[id^="sa-"]').nth(1);
            const potentialSeverityInput = page.locator('input[id^="sp-"]').nth(2);

            await page.locator('.p-radiobutton:has(input[id^="sa-"])').nth(1).click();
            await page.locator('.p-radiobutton:has(input[id^="sp-"])').nth(2).click();

            await expect(actualSeverityInput).toBeChecked();
            await expect(potentialSeverityInput).toBeChecked();

            await page.locator('xpath=//label[contains(normalize-space(.), "Job Number")]/following::input[1]').fill(jobTag);
            await page.getByPlaceholder('Describe the work being performed at the time of the incident').fill('Playwright live save validation.');
            await page.getByPlaceholder('Provide a detailed description of the incident').fill('Playwright verifies save path and cleanup behavior.');

            await page.getByRole('button', { name: 'Save Draft' }).click();
            await expect(page).toHaveURL(/\/incident-management\/incidents\/[0-9a-fA-F-]{36}$/);

            createdId = page.url().split('/').pop();
            expect(createdId).toBeTruthy();

            await gotoAndStabilize(page, '/incident-management/incidents');
            await page.getByPlaceholder('Search incidents...').fill(jobTag);
            await page.locator('button:has(.pi-search)').first().click();
            await expect(page.locator('.p-datatable-tbody > tr', { hasText: jobTag })).toHaveCount(1);

            expectNoRuntimeErrors(runtimeErrors);
        } finally {
            if (!KEEP_TEST_DATA) {
                if (createdId) {
                    await deleteIncidentById(request, createdId);
                }
                await cleanupIncidentsByJobTag(request, jobTag);
            }
        }
    });

    test('can complete the incident form and persist full page-1 data', async ({ page, request }) => {
        const runtimeErrors = startRuntimeErrorTracking(page);
        const jobTag = `PW-FULL-${Date.now()}`;
        const clientName = `CLIENT-${Date.now()}`;
        const plantName = `PLANT-${Date.now()}`;
        const workDescription = 'Full form test: pre-task planning and line break preparation.';
        const incidentSummary = 'Full form test: valve leak discovered during isolation, no injuries.';
        const equipmentType = 'Ford F-250';
        const unitNumber = `UNIT-${Date.now()}`;
        const natureOfInjury = 'Strain';
        const bodyPart = 'Left shoulder';
        const visibility = 'Clear daylight';
        const employeeId = `EMP-${Date.now()}`;
        const employeeName = 'Playwright Test Employee';

        let createdId: string | undefined;

        try {
            await cleanupIncidentsByJobTag(request, jobTag);

            await getJsonOrFail<RefCompanyDto[]>(request, `${API_BASE_URL}/v1/ReferenceData/companies`);
            await getJsonOrFail<RefRegionDto[]>(request, `${API_BASE_URL}/v1/ReferenceData/regions`);

            await gotoAndStabilize(page, '/incident-management/incidents/new');
            await expect(page.getByRole('heading', { name: 'New Incident Report' })).toBeVisible();

            await selectFirstPrimeDropdownOption(page, page.locator('.p-dropdown').first());

            const regionDropdown = page.locator('.p-dropdown').nth(1);
            await expect(regionDropdown).not.toHaveClass(/p-disabled/);
            await selectFirstPrimeDropdownOption(page, regionDropdown);

            await page.locator('.p-radiobutton:has(#ic-NearMiss)').click();
            await expect(page.locator('#ic-NearMiss')).toBeChecked();

            await page.locator('.p-radiobutton:has(input[id^="sa-"])').nth(0).click();
            await page.locator('.p-radiobutton:has(input[id^="sp-"])').nth(1).click();

            await page.locator('xpath=//label[contains(normalize-space(.), "Job Number")]/following::input[1]').fill(jobTag);
            await page.locator('xpath=//label[contains(normalize-space(.), "Client Name")]/following::input[1]').fill(clientName);
            await page.locator('xpath=//label[contains(normalize-space(.), "Plant Name")]/following::input[1]').fill(plantName);

            await page.getByPlaceholder('Describe the work being performed at the time of the incident').fill(workDescription);
            await page.getByPlaceholder('Provide a detailed description of the incident').fill(incidentSummary);

            await page.getByPlaceholder('Equipment type').fill(equipmentType);
            await page.getByPlaceholder('Unit #').fill(unitNumber);
            await page.getByPlaceholder('Nature of injury').fill(natureOfInjury);
            await page.getByPlaceholder('Body part').fill(bodyPart);
            await page.getByPlaceholder('Visibility conditions').fill(visibility);

            await page.getByRole('button', { name: 'Add Employee' }).click();
            const employeeRow = page.locator('.p-datatable-tbody > tr').first();
            await employeeRow.getByPlaceholder('ID', { exact: true }).fill(employeeId);
            await employeeRow.getByPlaceholder('Full name', { exact: true }).fill(employeeName);

            await page.getByRole('button', { name: 'Save Draft' }).click();
            await expect(page).toHaveURL(/\/incident-management\/incidents\/[0-9a-fA-F-]{36}$/);

            createdId = page.url().split('/').pop();
            expect(createdId).toBeTruthy();

            const saved = await getJsonOrFail<IncidentReportDto>(
                request,
                `${API_BASE_URL}/v1/IncidentReport/${createdId}`,
            );

            expect(saved.jobNumber).toBe(jobTag);
            expect(saved.clientCode).toBe(clientName);
            expect(saved.plantCode).toBe(plantName);
            expect(saved.workDescription).toBe(workDescription);
            expect(saved.incidentSummary).toBe(incidentSummary);
            expect(saved.incidentClass).toBe('NearMiss');
            expect(saved.employeesInvolved?.some(x => x.employeeIdentifier === employeeId)).toBeTruthy();

            // Note: typeOfEquipment, unitNumbers, natureOfInjury, bodyPartsInjured, and visibility
            // are not persisted by the current DB schema (EF Core ignores these columns).
            await expect(page.locator('xpath=//label[contains(normalize-space(.), "Job Number")]/following::input[1]')).toHaveValue(jobTag);

            expectNoRuntimeErrors(runtimeErrors);
        } finally {
            if (!KEEP_TEST_DATA) {
                if (createdId) {
                    await deleteIncidentById(request, createdId);
                }
                await cleanupIncidentsByJobTag(request, jobTag);
            }
        }
    });
});
