import type { AxiosInstance } from 'axios';

export type InvestigationStatus = 'Assigned' | 'In Progress' | 'Completed' | 'Closed';

export type InvestigationPriority = 'High' | 'Medium' | 'Low';

export type SafetyInvestigationRecord = {
    investigationId: string;
    investigationNumber: string;
    incidentId: string;
    incidentNumber: string;
    incidentDate: string;
    owner: string;
    openDate: string;
    dueDate: string;
    status: InvestigationStatus;
    priority: InvestigationPriority;
    reviewStatus: string;
    classificationStatus: string;
    investigationDetails: string;
    incidentStatus: string;
};

export type InvestigationListQuery = {
    searchTerm?: string;
    status?: InvestigationStatus | '';
};

export type CreateInvestigationRequest = {
    incidentId: string;
    reviewStatus?: string;
    classificationStatus?: string;
    investigationDetails?: string;
};

export type UpdateInvestigationRequest = {
    status?: string;
    reviewStatus?: string;
    classificationStatus?: string;
    investigationDetails?: string;
};

export type IncidentLookupItem = {
    id: string;
    incidentNumber: string;
    incidentDate: string;
    status: string;
};

export type InvestigationAttributeOption = {
    id: string;
    name: string;
    typeCode: string;
    typeName: string;
};

export type InvestigationActionItem = {
    actionId: string;
    actionType: 'Corrective' | 'Preventative' | string;
    actionDescription: string;
    assignedTo: string;
    dueDate: string | null;
};

export type CreateInvestigationActionRequest = {
    actionType: string;
    actionDescription: string;
    assignedTo: string;
    dueDate?: string | null;
};

export async function fetchInvestigationList(
    api: AxiosInstance,
    query: InvestigationListQuery = {},
): Promise<SafetyInvestigationRecord[]> {
    const response = await api.get<SafetyInvestigationRecord[]>('/v1/Investigation', {
        params: {
            searchTerm: query.searchTerm?.trim() || undefined,
            status: query.status || undefined,
        },
    });

    return response.data;
}

export async function fetchInvestigationById(
    api: AxiosInstance,
    investigationId: string,
): Promise<SafetyInvestigationRecord> {
    const response = await api.get<SafetyInvestigationRecord>(`/v1/Investigation/${investigationId}`);
    return response.data;
}

export async function fetchInvestigationByIncident(
    api: AxiosInstance,
    incidentId: string,
): Promise<SafetyInvestigationRecord | null> {
    try {
        const response = await api.get<SafetyInvestigationRecord>(`/v1/Investigation/by-incident/${incidentId}`);
        return response.data;
    } catch (error: unknown) {
        const typedError = error as { response?: { status?: number } };
        if (typedError.response?.status === 404) {
            return null;
        }
        throw error;
    }
}

export async function createInvestigation(
    api: AxiosInstance,
    request: CreateInvestigationRequest,
): Promise<SafetyInvestigationRecord> {
    const response = await api.post<SafetyInvestigationRecord>('/v1/Investigation', request);
    return response.data;
}

export async function updateInvestigation(
    api: AxiosInstance,
    investigationId: string,
    request: UpdateInvestigationRequest,
): Promise<SafetyInvestigationRecord> {
    const response = await api.put<SafetyInvestigationRecord>(`/v1/Investigation/${investigationId}`, request);
    return response.data;
}

export async function fetchInvestigationAttributeOptions(
    api: AxiosInstance,
): Promise<InvestigationAttributeOption[]> {
    const response = await api.get<InvestigationAttributeOption[]>('/v1/Investigation/attributes');
    return response.data ?? [];
}

export async function fetchInvestigationAttributes(
    api: AxiosInstance,
    investigationId: string,
): Promise<string[]> {
    const response = await api.get<string[]>(`/v1/Investigation/${investigationId}/attributes`);
    return response.data ?? [];
}

export async function saveInvestigationAttributes(
    api: AxiosInstance,
    investigationId: string,
    attributeIds: string[],
): Promise<void> {
    await api.post(`/v1/Investigation/${investigationId}/attributes`, attributeIds);
}

export async function fetchInvestigationActions(
    api: AxiosInstance,
    investigationId: string,
): Promise<InvestigationActionItem[]> {
    const response = await api.get<InvestigationActionItem[]>(`/v1/Investigation/${investigationId}/actions`);
    return response.data ?? [];
}

export async function saveInvestigationAction(
    api: AxiosInstance,
    investigationId: string,
    request: CreateInvestigationActionRequest,
): Promise<InvestigationActionItem> {
    const response = await api.post<InvestigationActionItem>(
        `/v1/Investigation/${investigationId}/actions`,
        request,
    );
    return response.data;
}

export type TransitionAction = 'start' | 'complete' | 'approve' | 'reject';

export async function transitionInvestigation(
    api: AxiosInstance,
    investigationId: string,
    action: TransitionAction,
): Promise<SafetyInvestigationRecord> {
    const response = await api.post<SafetyInvestigationRecord>(
        `/v1/Investigation/${investigationId}/transition`,
        { action },
    );
    return response.data;
}

export async function deleteInvestigationAction(
    api: AxiosInstance,
    investigationId: string,
    actionId: string,
): Promise<void> {
    await api.delete(`/v1/Investigation/${investigationId}/actions/${actionId}`);
}

export type IncidentEmployeeRecord = {
    id: string;
    employeeIdentifier: string | null;
    employeeName: string | null;
    injuryTypeCode: string | null;
    recordable: boolean | null;
    hoursWorked: number | null;
};

export async function fetchIncidentEmployees(
    api: AxiosInstance,
    incidentId: string,
): Promise<IncidentEmployeeRecord[]> {
    const response = await api.get<{ employeesInvolved: IncidentEmployeeRecord[] }>(
        `/v1/IncidentReport/${incidentId}`,
    );
    return response.data.employeesInvolved ?? [];
}

export async function searchIncidents(
    api: AxiosInstance,
    searchTerm: string,
): Promise<IncidentLookupItem[]> {
    const normalized = searchTerm.trim();
    if (!normalized) return [];
    const response = await api.get<IncidentLookupItem[]>('/v1/IncidentReport', {
        params: { searchTerm: normalized },
    });
    return response.data ?? [];
}

export async function findIncidentByNumber(
    api: AxiosInstance,
    incidentNumber: string,
): Promise<IncidentLookupItem | null> {
    const normalizedIncidentNumber = incidentNumber.trim().toUpperCase();
    if (!normalizedIncidentNumber) {
        return null;
    }

    const response = await api.get<IncidentLookupItem[]>('/v1/IncidentReport', {
        params: {
            searchTerm: normalizedIncidentNumber,
        },
    });

    const exactMatch = response.data.find(
        incident => incident.incidentNumber?.trim().toUpperCase() === normalizedIncidentNumber,
    );

    return exactMatch ?? null;
}
