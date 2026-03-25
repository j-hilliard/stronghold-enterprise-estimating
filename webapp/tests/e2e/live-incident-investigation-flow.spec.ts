import { expect, test } from '@playwright/test';
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

type IncidentListItem = {
    id?: string;
    incidentNumber?: string;
    status?: string;
    jobNumber?: string;
};

type IncidentReportDto = {
    id?: string;
    incidentNumber?: string;
    status?: string;
    jobNumber?: string;
    workDescription?: string;
    incidentSummary?: string;
};

type SafetyInvestigationRecord = {
    investigationId: string;
    investigationNumber: string;
    incidentId: string;
    incidentNumber: string;
    status: string;
    reviewStatus: string;
    classificationStatus: string;
    investigationDetails: string;
    incidentStatus: string;
};

test.describe('Live Incident -> Investigation lifecycle (DB-backed)', () => {
    test.skip(!runLiveApi, 'Set PW_RUN_LIVE_API=true for authenticated live API workflow checks.');
    test.describe.configure({ timeout: 180_000 });

    test('creates incident, starts/completes/approves investigation, and verifies API state', async ({ page, request }) => {
        const runtimeErrors = startRuntimeErrorTracking(page);

        const runToken = `${Date.now()}`;
        const jobTag = `PW-INV-${runToken}`;
        const workDescription = `Playwright lifecycle validation ${runToken}`;
        const incidentSummary = `Incident created for lifecycle e2e ${runToken}`;
        const classificationStatus = `Formal ${runToken}`;
        const investigationDetails = `Investigation details entered by Playwright ${runToken}`;

        let createdIncidentId: string | undefined;
        let createdInvestigationId: string | undefined;

        try {
            await cleanupIncidentsByJobTag(request, jobTag);

            await gotoAndStabilize(page, '/safety-application/incidents/new');
            await expect(page.getByRole('heading', { name: 'New Incident Report' })).toBeVisible();

            await selectFirstPrimeDropdownOption(page, page.locator('.p-dropdown').first());

            const regionDropdown = page.locator('.p-dropdown').nth(1);
            await expect(regionDropdown).not.toHaveClass(/p-disabled/);
            await selectFirstPrimeDropdownOption(page, regionDropdown);

            await page
                .locator('xpath=//label[contains(normalize-space(.), "Job Number")]/following::input[1]')
                .fill(jobTag);
            await page
                .getByPlaceholder('Describe the work being performed at the time of the incident')
                .fill(workDescription);
            await page
                .getByPlaceholder('Provide a detailed description of the incident')
                .fill(incidentSummary);

            // PrimeVue hides the native checkbox input — click the visible label instead
            await page.locator('label').filter({ hasText: /^Formal Investigation$/ }).click();

            await page.getByRole('button', { name: 'Submit', exact: true }).click();
            await expect(page).toHaveURL(/\/safety-application\/incidents$/);

            const incidentMatches = await getJsonOrFail<IncidentListItem[]>(
                request,
                `${API_BASE_URL}/v1/IncidentReport?searchTerm=${encodeURIComponent(jobTag)}`,
            );
            const createdIncident = incidentMatches.find(item => item.jobNumber === jobTag);
            expect(createdIncident?.id).toBeTruthy();
            createdIncidentId = createdIncident?.id;

            const savedIncident = await getJsonOrFail<IncidentReportDto>(
                request,
                `${API_BASE_URL}/v1/IncidentReport/${createdIncidentId}`,
            );
            expect(savedIncident.jobNumber).toBe(jobTag);
            expect(savedIncident.workDescription).toBe(workDescription);
            expect(savedIncident.incidentSummary).toBe(incidentSummary);
            expect(savedIncident.status).toBe('Open');
            expect(savedIncident.incidentNumber).toBeTruthy();

            await gotoAndStabilize(page, `/safety-application/incidents/${createdIncidentId}`);
            await expect(page.getByRole('button', { name: 'Begin Investigation' })).toBeVisible();
            await page.getByRole('button', { name: 'Begin Investigation' }).click();

            await expect(page).toHaveURL(/\/safety-application\/investigations\/[0-9a-fA-F-]{36}$/);
            createdInvestigationId = page.url().split('/').pop()?.split('?')[0];
            expect(createdInvestigationId).toBeTruthy();

            const createdInvestigation = await getJsonOrFail<SafetyInvestigationRecord>(
                request,
                `${API_BASE_URL}/v1/Investigation/${createdInvestigationId}`,
            );
            expect(createdInvestigation.incidentId.toLowerCase()).toBe(createdIncidentId?.toLowerCase());
            expect(createdInvestigation.incidentNumber).toBe(savedIncident.incidentNumber);
            expect(createdInvestigation.status).toBe('Assigned');
            expect(createdInvestigation.investigationNumber).toMatch(/^INV-[A-Z0-9]{8}$/);

            const incidentAfterStart = await getJsonOrFail<IncidentReportDto>(
                request,
                `${API_BASE_URL}/v1/IncidentReport/${createdIncidentId}`,
            );
            expect(incidentAfterStart.status).toBe('Investigation');

            await expect(page.getByRole('button', { name: 'Start Investigation' })).toBeVisible();
            await page.getByRole('button', { name: 'Start Investigation' }).click();
            await expect(page.getByRole('button', { name: 'Complete Investigation' })).toBeVisible();

            const startedInvestigation = await getJsonOrFail<SafetyInvestigationRecord>(
                request,
                `${API_BASE_URL}/v1/Investigation/${createdInvestigationId}`,
            );
            expect(startedInvestigation.status).toBe('In Progress');

            await page.getByRole('button', { name: 'Edit Investigation' }).click();
            await page.getByPlaceholder('Describe the investigation findings...').fill(investigationDetails);
            // Review Status is lifecycle-managed (transition buttons only) — not editable in form
            await page
                .locator('xpath=//label[contains(normalize-space(.), "Classification Status")]/following::input[1]')
                .fill(classificationStatus);
            await page.getByRole('button', { name: 'Save Changes' }).click();
            await expect(page.getByRole('button', { name: 'Edit Investigation' })).toBeVisible();

            const updatedInvestigation = await getJsonOrFail<SafetyInvestigationRecord>(
                request,
                `${API_BASE_URL}/v1/Investigation/${createdInvestigationId}`,
            );
            expect(updatedInvestigation.status).toBe('In Progress');
            expect(updatedInvestigation.classificationStatus).toBe(classificationStatus);
            expect(updatedInvestigation.investigationDetails).toBe(investigationDetails);

            await page.getByRole('button', { name: 'Complete Investigation' }).click();
            await expect(page.getByRole('button', { name: 'Approve' })).toBeVisible();
            await expect(page.getByRole('button', { name: 'Reject' })).toBeVisible();

            const completedInvestigation = await getJsonOrFail<SafetyInvestigationRecord>(
                request,
                `${API_BASE_URL}/v1/Investigation/${createdInvestigationId}`,
            );
            expect(completedInvestigation.status).toBe('Completed');

            const incidentAfterComplete = await getJsonOrFail<IncidentReportDto>(
                request,
                `${API_BASE_URL}/v1/IncidentReport/${createdIncidentId}`,
            );
            expect(incidentAfterComplete.status).toBe('In-Review');

            await page.getByRole('button', { name: 'Approve' }).click();
            // Wait for approve transition to complete — Approve button disappears when incident is Closed
            await expect(page.getByRole('button', { name: 'Approve' })).not.toBeVisible();

            const incidentAfterApprove = await getJsonOrFail<IncidentReportDto>(
                request,
                `${API_BASE_URL}/v1/IncidentReport/${createdIncidentId}`,
            );
            expect(incidentAfterApprove.status).toBe('Closed');

            const investigationList = await getJsonOrFail<SafetyInvestigationRecord[]>(
                request,
                `${API_BASE_URL}/v1/Investigation`,
            );
            expect(
                investigationList.some(
                    item => item.investigationId.toLowerCase() === createdInvestigationId?.toLowerCase(),
                ),
            ).toBeTruthy();

            expectNoRuntimeErrors(runtimeErrors);
        } finally {
            if (!KEEP_TEST_DATA) {
                if (createdIncidentId) {
                    await deleteIncidentById(request, createdIncidentId);
                } else {
                    await cleanupIncidentsByJobTag(request, jobTag);
                }
            }
        }
    });
});
