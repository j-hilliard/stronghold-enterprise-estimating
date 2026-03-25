import type { AxiosInstance } from 'axios';

export type IncidentRegisterSortField =
    | 'incidentNumber'
    | 'incidentDate'
    | 'company'
    | 'region'
    | 'customer'
    | 'customerSite'
    | 'severityActualCode'
    | 'status';

export type IncidentFilterType = 'recordable' | 'lost-time' | 'near-miss';

export type IncidentRegisterRecord = {
    incidentId: string;
    incidentNumber: string;
    incidentDate: string;
    companyCode: string;
    company: string;
    regionCode: string;
    region: string;
    customer: string;
    customerSite: string;
    status: string;
    incidentClass: string;
    severityActualCode: string;
    severityPotentialCode: string;
    isRecordable: boolean;
    isLostTime: boolean;
    isNearMiss: boolean;
};

export type IncidentRegisterOption = {
    label: string;
    value: string;
};

export type IncidentRegisterStatusCount = {
    status: string;
    count: number;
};

export type IncidentRegisterQuery = {
    type?: IncidentFilterType | '';
    incidentDateStart?: string;
    incidentDateEnd?: string;
    company?: string;
    customer?: string;
    status?: string;
    page?: number;
    pageSize?: number;
    sortField?: IncidentRegisterSortField;
    sortOrder?: 1 | -1;
};

export type IncidentRegisterPage = {
    items: IncidentRegisterRecord[];
    total: number;
    page: number;
    pageSize: number;
    pageCount: number;
    filteredOpenCount: number;
    filteredHighSeverityCount: number;
    companies: IncidentRegisterOption[];
    customers: IncidentRegisterOption[];
    statuses: IncidentRegisterOption[];
    statusCounts: IncidentRegisterStatusCount[];
};

export type IncidentDashboardSummary = {
    totalIncidents: number;
    occurredLast30Days: number;
    openInvestigations: number;
    openInvestigationsOver30Days: number;
    recordableIncidents: number;
    lostTimeIncidents: number;
    nearMisses: number;
};

export type IncidentTrendPoint = {
    monthKey: string;
    monthLabel: string;
    incidentCount: number;
    nearMissCount: number;
};

export const incidentRegisterPageSizeOptions = [20, 50, 100] as const;
export const incidentRegisterDefaultPageSize = 20;
export const incidentRegisterDefaultSortField: IncidentRegisterSortField = 'incidentDate';
export const incidentRegisterDefaultSortOrder: 1 | -1 = -1;

export async function fetchIncidentRegisterPage(
    api: AxiosInstance,
    query: IncidentRegisterQuery,
): Promise<IncidentRegisterPage> {
    const response = await api.get<IncidentRegisterPage>('/v1/Incident/register', {
        params: {
            type: query.type || undefined,
            incidentDateStart: query.incidentDateStart || undefined,
            incidentDateEnd: query.incidentDateEnd || undefined,
            company: query.company || undefined,
            customer: query.customer || undefined,
            status: query.status || undefined,
            page: query.page || 1,
            pageSize: normalizePageSize(query.pageSize),
            sortField: query.sortField || incidentRegisterDefaultSortField,
            sortOrder: (query.sortOrder || incidentRegisterDefaultSortOrder) === 1 ? 'asc' : 'desc',
        },
    });

    return response.data;
}

export async function fetchIncidentDashboardSummary(
    api: AxiosInstance,
    query: Pick<IncidentRegisterQuery, 'type' | 'incidentDateStart' | 'incidentDateEnd' | 'company' | 'customer' | 'status'> = {},
): Promise<IncidentDashboardSummary> {
    const response = await api.get<IncidentDashboardSummary>('/v1/Incident/register-summary', {
        params: {
            type: query.type || undefined,
            incidentDateStart: query.incidentDateStart || undefined,
            incidentDateEnd: query.incidentDateEnd || undefined,
            company: query.company || undefined,
            customer: query.customer || undefined,
            status: query.status || undefined,
        },
    });
    return response.data;
}

export async function fetchIncidentTrendByMonth(api: AxiosInstance): Promise<IncidentTrendPoint[]> {
    const response = await api.get<IncidentTrendPoint[]>('/v1/Incident/register-trend');
    return response.data;
}

function normalizePageSize(pageSize?: number) {
    return incidentRegisterPageSizeOptions.includes(pageSize as (typeof incidentRegisterPageSizeOptions)[number])
        ? (pageSize as (typeof incidentRegisterPageSizeOptions)[number])
        : incidentRegisterDefaultPageSize;
}
