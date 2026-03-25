import type { AxiosInstance } from 'axios';

export type ProfileDateRangePreset =
    | 'last-30-days'
    | 'last-60-days'
    | 'last-90-days'
    | 'q1-this-year'
    | 'q2-this-year'
    | 'q3-this-year'
    | 'q4-this-year'
    | 'q1-last-year'
    | 'q2-last-year'
    | 'q3-last-year'
    | 'q4-last-year';

export type UserProfileSettings = {
    profileId: number;
    userId: number;
    defaultDateRange?: ProfileDateRangePreset;
    defaultCompany?: string;
    defaultCustomer?: string;
    defaultIncidentStatuses?: string[];
    createdOn?: string;
    createdById?: number;
    modifiedOn?: string;
    modifiedById?: number;
};

export type ProfileFilterOptions = {
    companies: { label: string; value: string }[];
    customers: { label: string; value: string }[];
    statuses: { label: string; value: string }[];
};

export const profileDateRangeOptions: { label: string; value: ProfileDateRangePreset }[] = [
    { label: 'Last 30 Days', value: 'last-30-days' },
    { label: 'Last 60 Days', value: 'last-60-days' },
    { label: 'Last 90 Days', value: 'last-90-days' },
    { label: 'Q1 This Year', value: 'q1-this-year' },
    { label: 'Q2 This Year', value: 'q2-this-year' },
    { label: 'Q3 This Year', value: 'q3-this-year' },
    { label: 'Q4 This Year', value: 'q4-this-year' },
    { label: 'Q1 Last Year', value: 'q1-last-year' },
    { label: 'Q2 Last Year', value: 'q2-last-year' },
    { label: 'Q3 Last Year', value: 'q3-last-year' },
    { label: 'Q4 Last Year', value: 'q4-last-year' },
];

export async function fetchMyProfile(api: AxiosInstance): Promise<UserProfileSettings> {
    const response = await api.get<UserProfileSettings>('/v1/Profile/me');
    return response.data;
}

export async function fetchProfileFilterOptions(api: AxiosInstance): Promise<ProfileFilterOptions> {
    const response = await api.get<ProfileFilterOptions>('/v1/Profile/options');
    return response.data;
}

export async function fetchUserProfile(api: AxiosInstance, userId: number): Promise<UserProfileSettings> {
    const response = await api.get<UserProfileSettings>(`/v1/Profile/${userId}`);
    return response.data;
}

export async function saveMyProfile(api: AxiosInstance, profile: UserProfileSettings): Promise<UserProfileSettings> {
    const response = await api.put<UserProfileSettings>('/v1/Profile/me', profile);
    return response.data;
}

export async function saveUserProfile(api: AxiosInstance, userId: number, profile: UserProfileSettings): Promise<UserProfileSettings> {
    const response = await api.put<UserProfileSettings>(`/v1/Profile/${userId}`, profile);
    return response.data;
}

export function buildIncidentQueryFromProfile(
    profile: UserProfileSettings | null | undefined,
    overrides: Record<string, string | undefined> = {},
) {
    const query: Record<string, string> = {};

    if (profile?.defaultDateRange) {
        query.dateRange = profile.defaultDateRange;
    }

    if (profile?.defaultCompany) {
        query.company = profile.defaultCompany;
    }

    if (profile?.defaultCustomer) {
        query.customer = profile.defaultCustomer;
    }

    if (profile?.defaultIncidentStatuses?.length) {
        query.status = profile.defaultIncidentStatuses.join(',');
    }

    for (const [key, value] of Object.entries(overrides)) {
        if (value) {
            query[key] = value;
        } else {
            delete query[key];
        }
    }

    return query;
}

export function resolveProfileDateRange(range?: ProfileDateRangePreset) {
    if (!range) {
        return { incidentDateStart: undefined, incidentDateEnd: undefined };
    }

    const today = new Date();

    switch (range) {
        case 'last-30-days':
            return buildRelativeRange(today, 30);
        case 'last-60-days':
            return buildRelativeRange(today, 60);
        case 'last-90-days':
            return buildRelativeRange(today, 90);
        case 'q1-this-year':
            return buildQuarterRange(today.getFullYear(), 1);
        case 'q2-this-year':
            return buildQuarterRange(today.getFullYear(), 2);
        case 'q3-this-year':
            return buildQuarterRange(today.getFullYear(), 3);
        case 'q4-this-year':
            return buildQuarterRange(today.getFullYear(), 4);
        case 'q1-last-year':
            return buildQuarterRange(today.getFullYear() - 1, 1);
        case 'q2-last-year':
            return buildQuarterRange(today.getFullYear() - 1, 2);
        case 'q3-last-year':
            return buildQuarterRange(today.getFullYear() - 1, 3);
        case 'q4-last-year':
            return buildQuarterRange(today.getFullYear() - 1, 4);
    }
}

function buildRelativeRange(referenceDate: Date, dayCount: number) {
    const incidentDateEnd = toDateOnlyString(referenceDate);
    const incidentDateStartValue = new Date(referenceDate);
    incidentDateStartValue.setDate(incidentDateStartValue.getDate() - (dayCount - 1));

    return {
        incidentDateStart: toDateOnlyString(incidentDateStartValue),
        incidentDateEnd,
    };
}

function buildQuarterRange(year: number, quarter: 1 | 2 | 3 | 4) {
    const startMonth = (quarter - 1) * 3;
    return {
        incidentDateStart: toDateOnlyString(new Date(year, startMonth, 1)),
        incidentDateEnd: toDateOnlyString(new Date(year, startMonth + 3, 0)),
    };
}

function toDateOnlyString(value: Date) {
    const year = value.getFullYear();
    const month = String(value.getMonth() + 1).padStart(2, '0');
    const day = String(value.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}
