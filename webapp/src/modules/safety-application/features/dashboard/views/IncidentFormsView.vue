<template>
    <div class="space-y-6">
        <BasePageHeader
            title="Incidents"
            subtitle="Review the full incident register, apply operational filters, and drill into follow-up work."
            icon="pi pi-file-edit"
        >
            <Button label="Create Incident" icon="pi pi-plus" @click="router.push('/safety-application/incidents/new')" />
        </BasePageHeader>

        <div v-if="activeFilterLabel" class="rounded-2xl border border-blue-500/25 bg-blue-500/10 px-5 py-4 text-sm text-blue-100 shadow-lg shadow-slate-950/15">
            <div class="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
                <div class="flex items-center gap-3">
                    <i class="pi pi-filter-fill text-blue-300" />
                    <div>
                        <p class="m-0 text-xs font-semibold uppercase tracking-[0.24em] text-blue-200">KPI Filter Applied</p>
                        <p class="m-0 text-sm text-blue-100">Showing the incident register filtered to {{ activeFilterLabel.toLowerCase() }}.</p>
                    </div>
                </div>
                <Button label="Clear KPI Filter" text class="!text-blue-100" @click="clearTypeFilter" />
            </div>
        </div>

        <Card class="border border-slate-700/70 bg-slate-800/70 shadow-lg shadow-slate-950/20">
            <template #content>
                <div class="grid gap-4 p-6 md:grid-cols-2 xl:grid-flow-col xl:auto-cols-fr">
                    <div class="rounded-2xl border border-slate-700/70 bg-slate-900/50 p-4">
                        <p class="mb-2 text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">Total Incidents</p>
                        <p class="m-0 text-3xl font-semibold text-white">{{ totalRecords }}</p>
                    </div>
                    <div
                        v-for="statusCard in statusSummaryCards"
                        :key="statusCard.status"
                        class="rounded-2xl border border-slate-700/70 bg-slate-900/50 p-4"
                    >
                        <p class="mb-2 text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">{{ statusCard.label }}</p>
                        <p class="m-0 text-3xl font-semibold text-white">{{ statusCard.count }}</p>
                    </div>
                </div>
            </template>
        </Card>

        <BasePageSubheader title="Incident Register" />

        <Card class="border border-slate-700/70 bg-slate-800/70 shadow-lg shadow-slate-950/20">
            <template #content>
                <div class="space-y-4 p-6">
                    <div class="flex w-full flex-col gap-4 rounded-2xl border border-slate-700/70 bg-slate-900/50 p-4">
                        <div class="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
                            <div class="flex flex-col gap-3 md:col-span-2 xl:col-span-1">
                                <span class="flex flex-col gap-2">
                                    <label class="text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Date Range</label>
                                    <Dropdown
                                        :modelValue="selectedDateRange || null"
                                        :options="dateRangeOptions"
                                        optionLabel="label"
                                        optionValue="value"
                                        placeholder="All Dates"
                                        class="w-full"
                                        :showClear="!!selectedDateRange"
                                        @update:modelValue="handleDateRangeSelection"
                                    />
                                </span>
                                <div v-if="isCustomDateRangeSelected" class="grid gap-3 md:grid-cols-2">
                                    <span class="flex flex-col gap-2">
                                        <label class="text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Start Date</label>
                                        <InputText
                                            :modelValue="customDateStart"
                                            type="date"
                                            class="w-full"
                                            @update:modelValue="updateCustomDate('incidentDateStart', normalizeString($event))"
                                        />
                                    </span>
                                    <span class="flex flex-col gap-2">
                                        <label class="text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">End Date</label>
                                        <InputText
                                            :modelValue="customDateEnd"
                                            type="date"
                                            class="w-full"
                                            @update:modelValue="updateCustomDate('incidentDateEnd', normalizeString($event))"
                                        />
                                    </span>
                                    <p v-if="customDateValidationMessage" class="md:col-span-2 m-0 text-xs text-amber-300">
                                        {{ customDateValidationMessage }}
                                    </p>
                                </div>
                            </div>
                            <span class="flex flex-col gap-2">
                                <label class="text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Company</label>
                                <Dropdown
                                    :modelValue="selectedCompany || null"
                                    :options="companyOptions"
                                    optionLabel="label"
                                    optionValue="value"
                                    placeholder="All Companies"
                                    class="w-full"
                                    :showClear="!!selectedCompany"
                                    @update:modelValue="updateFilter('company', normalizeString($event))"
                                />
                            </span>
                            <span class="flex flex-col gap-2">
                                <label class="text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Customer</label>
                                <Dropdown
                                    :modelValue="selectedCustomer || null"
                                    :options="customerOptions"
                                    optionLabel="label"
                                    optionValue="value"
                                    placeholder="All Customers"
                                    class="w-full"
                                    :showClear="!!selectedCustomer"
                                    @update:modelValue="updateFilter('customer', normalizeString($event))"
                                />
                            </span>
                            <span class="flex flex-col gap-2">
                                <label class="text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Status</label>
                                <MultiSelect
                                    :modelValue="selectedStatuses"
                                    :options="statusOptions"
                                    optionLabel="label"
                                    optionValue="value"
                                    placeholder="All Statuses"
                                    class="w-full"
                                    display="chip"
                                    filter
                                    showClear
                                    @update:modelValue="updateStatusFilter"
                                />
                            </span>
                        </div>
                        <div class="flex flex-wrap items-center justify-between gap-3">
                            <div class="flex flex-wrap items-center gap-2">
                                <Tag v-if="activeFilterLabel" :value="activeFilterLabel" severity="info" class="!rounded-full !px-3 !py-2" />
                                <Tag v-if="selectedDateRangeLabel" :value="selectedDateRangeLabel" severity="secondary" class="!rounded-full !px-3 !py-2" />
                                <Tag v-if="selectedCompany" :value="selectedCompany" severity="contrast" class="!rounded-full !px-3 !py-2" />
                                <Tag v-if="selectedCustomer" :value="selectedCustomer" severity="contrast" class="!rounded-full !px-3 !py-2" />
                                <Tag
                                    v-for="selectedStatus in selectedStatuses"
                                    :key="selectedStatus"
                                    :value="displayStatusLabel(selectedStatus)"
                                    severity="warning"
                                    class="!rounded-full !px-3 !py-2"
                                />
                            </div>
                            <div class="flex items-center gap-3">
                                <span class="text-sm text-slate-400">{{ pageReport }}</span>
                                <Button v-if="showClearFilters" label="Clear Filters" text @click="clearFilters" />
                            </div>
                        </div>
                    </div>

                    <div class="overflow-x-auto rounded-3xl border border-slate-700/70 bg-slate-900/30">
                        <table class="min-w-full border-collapse">
                            <thead class="bg-slate-900/80">
                                <tr>
                                    <th v-for="column in columns" :key="column.field" class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">
                                        <button type="button" class="flex items-center gap-2 bg-transparent p-0 text-left text-inherit" @click="handleSortToggle(column.field)">
                                            <span>{{ column.label }}</span>
                                            <i :class="sortIcon(column.field)" />
                                        </button>
                                    </th>
                                    <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Actions</th>
                                </tr>
                            </thead>
                            <tbody v-if="rows.length">
                                <tr v-for="row in rows" :key="row.incidentId" class="border-t border-slate-800/80 text-sm text-slate-200 cursor-pointer" @dblclick="viewIncident(row)">
                                    <td class="px-4 py-3 font-medium text-white">{{ row.incidentNumber }}</td>
                                    <td class="px-4 py-3">{{ formatDateMMDDYYYY(row.incidentDate) }}</td>
                                    <td class="px-4 py-3">{{ row.company }}</td>
                                    <td class="px-4 py-3">{{ row.region }}</td>
                                    <td class="px-4 py-3">{{ row.customer }}</td>
                                    <td class="px-4 py-3">{{ row.customerSite }}</td>
                                    <td class="px-4 py-3">{{ row.severityActualCode || 'N/A' }}</td>
                                    <td class="px-4 py-3">
                                        <Tag :value="displayStatusLabel(row.status)" :severity="statusTone(row.status)" />
                                    </td>
                                    <td class="px-4 py-3 whitespace-nowrap">
                                        <BaseButtonIconView @click="viewIncident(row)" />
                                        <BaseButtonIconEdit v-if="row.status !== 'Closed'" @click="editIncident(row)" />
                                    </td>
                                </tr>
                            </tbody>
                            <tbody v-else>
                                <tr>
                                    <td colspan="9" class="px-4 py-8 text-center text-sm text-slate-400">
                                        {{ isLoading ? 'Loading incidents...' : emptyMessage }}
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                    </div>

                    <div class="flex flex-col gap-3 rounded-2xl border border-slate-700/70 bg-slate-900/40 px-4 py-3 xl:flex-row xl:items-center xl:justify-between">
                        <p class="m-0 text-sm text-slate-400">{{ pageReport }}</p>
                        <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-end">
                            <div class="flex items-center gap-3">
                                <span class="text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Page Size</span>
                                <Dropdown
                                    :modelValue="pageSize"
                                    :options="pageSizeOptionItems"
                                    optionLabel="label"
                                    optionValue="value"
                                    class="w-28"
                                    @change="updatePageSize($event.value || incidentRegisterDefaultPageSize)"
                                />
                            </div>
                            <Paginator
                                :rows="pageSize"
                                :totalRecords="totalRecords"
                                :first="firstRow"
                                template="PrevPageLink PageLinks NextPageLink"
                                @page="updatePage($event.page + 1)"
                            />
                        </div>
                    </div>
                </div>
            </template>
        </Card>
    </div>
</template>

<script setup lang="ts">
import BaseButtonIconEdit from '@/components/buttons/BaseButtonIconEdit.vue';
import BaseButtonIconView from '@/components/buttons/BaseButtonIconView.vue';
import BasePageHeader from '@/components/layout/BasePageHeader.vue';
import BasePageSubheader from '@/components/layout/BasePageSubheader.vue';
import {
    fetchIncidentRegisterPage,
    incidentRegisterDefaultPageSize,
    incidentRegisterDefaultSortField,
    incidentRegisterDefaultSortOrder,
    incidentRegisterPageSizeOptions,
    type IncidentFilterType,
    type IncidentRegisterOption,
    type IncidentRegisterRecord,
    type IncidentRegisterSortField,
    type IncidentRegisterStatusCount,
} from '@/modules/safety-application/features/dashboard/services/incidentRegisterService';
import { buildIncidentQueryFromProfile, fetchMyProfile } from '@/modules/safety-application/features/dashboard/services/userProfileService';
import { useApiStore } from '@/stores/apiStore';
import Card from 'primevue/card';
import Button from 'primevue/button';
import Dropdown from 'primevue/dropdown';
import InputText from 'primevue/inputtext';
import MultiSelect from 'primevue/multiselect';
import Paginator from 'primevue/paginator';
import Tag from 'primevue/tag';
import { useToast } from 'primevue/usetoast';
import { formatDateMMDDYYYY } from '@/utils';
import { computed, onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';

const route = useRoute();
const router = useRouter();
const toast = useToast();
const apiStore = useApiStore();

const rows = ref<IncidentRegisterRecord[]>([]);
const totalRecords = ref(0);
const statusCounts = ref<IncidentRegisterStatusCount[]>([]);
const isLoading = ref(false);
const companyOptions = ref<IncidentRegisterOption[]>([]);
const customerOptions = ref<IncidentRegisterOption[]>([]);
const statusOptions = ref<IncidentRegisterOption[]>([]);
const latestRequestId = ref(0);
const pageSizeOptions = incidentRegisterPageSizeOptions;
const pageSizeOptionItems = computed(() => pageSizeOptions.map(value => ({ label: String(value), value })));
const firstRow = computed(() => (page.value - 1) * pageSize.value);
type DateRangePreset =
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
    | 'q4-last-year'
    | 'custom';
type DateRangeOption = { label: string; value: DateRangePreset };

const dateRangeOptions: DateRangeOption[] = [
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
    { label: 'Custom Date Range', value: 'custom' },
];
const columns: { field: IncidentRegisterSortField; label: string }[] = [
    { field: 'incidentNumber', label: 'Incident Number' },
    { field: 'incidentDate', label: 'Incident Date' },
    { field: 'company', label: 'Company' },
    { field: 'region', label: 'Region' },
    { field: 'customer', label: 'Customer' },
    { field: 'customerSite', label: 'Customer Site' },
    { field: 'severityActualCode', label: 'Actual Severity' },
    { field: 'status', label: 'Status' },
];

const selectedType = computed<IncidentFilterType | ''>(() => {
    const type = String(route.query.type || '');
    if (type === 'recordable' || type === 'lost-time' || type === 'near-miss') {
        return type;
    }

    return '';
});

const selectedCompany = computed(() => normalizeQueryValue(route.query.company));
const selectedCustomer = computed(() => normalizeQueryValue(route.query.customer));
const selectedStatuses = computed(() => normalizeMultiValueQuery(route.query.status));
const customDateStart = computed(() => normalizeQueryValue(route.query.incidentDateStart));
const customDateEnd = computed(() => normalizeQueryValue(route.query.incidentDateEnd));
const selectedDateRange = computed<DateRangePreset | ''>(() => {
    const normalizedPreset = normalizeDateRangePreset(route.query.dateRange);
    if (normalizedPreset) {
        return normalizedPreset;
    }

    return customDateStart.value || customDateEnd.value ? 'custom' : '';
});
const page = computed(() => normalizePositiveNumber(route.query.page, 1));
const pageSize = computed(() => {
    const queryValue = normalizePositiveNumber(route.query.pageSize, incidentRegisterDefaultPageSize);
    return incidentRegisterPageSizeOptions.includes(queryValue as 20 | 50 | 100) ? queryValue : incidentRegisterDefaultPageSize;
});
const sortField = computed<IncidentRegisterSortField>(() => {
    const value = normalizeQueryValue(route.query.sortField) as IncidentRegisterSortField;
    const allowedFields: IncidentRegisterSortField[] = ['incidentNumber', 'incidentDate', 'company', 'region', 'customer', 'customerSite', 'severityActualCode', 'status'];
    return allowedFields.includes(value) ? value : incidentRegisterDefaultSortField;
});
const sortOrder = computed<1 | -1>(() => (normalizeQueryValue(route.query.sortOrder) === 'asc' ? 1 : -1));
const resolvedDateRange = computed(() => resolveDateRange(selectedDateRange.value, customDateStart.value, customDateEnd.value));
const incidentDateStart = computed(() => resolvedDateRange.value.start);
const incidentDateEnd = computed(() => resolvedDateRange.value.end);
const isCustomDateRangeSelected = computed(() => selectedDateRange.value === 'custom');
const customDateValidationMessage = computed(() => {
    if (!isCustomDateRangeSelected.value) {
        return '';
    }

    if ((customDateStart.value && !customDateEnd.value) || (!customDateStart.value && customDateEnd.value)) {
        return 'Enter both a start date and an end date to apply a custom date range.';
    }

    if (customDateStart.value && customDateEnd.value && customDateStart.value > customDateEnd.value) {
        return 'Start Date cannot be after End Date.';
    }

    return '';
});
const selectedDateRangeLabel = computed(() => {
    if (!selectedDateRange.value) {
        return '';
    }

    const option = dateRangeOptions.find(item => item.value === selectedDateRange.value);
    if (!option) {
        return '';
    }

    if (selectedDateRange.value !== 'custom' || !customDateStart.value || !customDateEnd.value) {
        return option.label;
    }

    return `${option.label}: ${formatDisplayDate(customDateStart.value)} - ${formatDisplayDate(customDateEnd.value)}`;
});

const activeFilters = computed(() => ({
    type: selectedType.value || null,
    company: selectedCompany.value || null,
    customer: selectedCustomer.value || null,
    statuses: selectedStatuses.value,
    dateRange: selectedDateRange.value || null,
    incidentDateStart: incidentDateStart.value || null,
    incidentDateEnd: incidentDateEnd.value || null,
    customDateStart: customDateStart.value || null,
    customDateEnd: customDateEnd.value || null,
    page: page.value,
    pageSize: pageSize.value,
    sortField: sortField.value,
    sortOrder: sortOrder.value,
}));

const activeFilterLabel = computed(() => {
    if (selectedType.value === 'recordable') return 'Recordable Incidents';
    if (selectedType.value === 'lost-time') return 'Lost Time Incidents';
    if (selectedType.value === 'near-miss') return 'Near Miss Incidents';
    return '';
});

const showClearFilters = computed(() =>
    !!selectedType.value ||
    !!selectedDateRange.value ||
    !!selectedCompany.value ||
    !!selectedCustomer.value ||
    !!selectedStatuses.value.length,
);

const emptyMessage = computed(() => showClearFilters.value ? 'No incidents match the active filters.' : 'No incidents found.');

const pageReport = computed(() => {
    if (!totalRecords.value) {
        return 'Showing 0 incidents';
    }

    const startRow = (page.value - 1) * pageSize.value + 1;
    const endRow = Math.min(page.value * pageSize.value, totalRecords.value);
    return `Showing ${startRow}-${endRow} of ${totalRecords.value} incidents`;
});

const statusSummaryCards = computed(() =>
    [...statusCounts.value]
        .sort((left, right) => statusDisplayOrder(left.status) - statusDisplayOrder(right.status))
        .map(statusCount => ({
            ...statusCount,
            label: displayStatusLabel(statusCount.status),
        })),
);

onMounted(async () => {
    const hasProfileParams = !!(route.query.dateRange || route.query.company || route.query.customer || route.query.status || route.query.type);
    if (!hasProfileParams) {
        const profile = await fetchMyProfile(apiStore.api).catch(() => null);
        const profileQuery = profile ? buildIncidentQueryFromProfile(profile) : {};
        if (Object.keys(profileQuery).length > 0) {
            await router.replace({ path: route.path, query: { ...route.query, ...profileQuery } });
        }
    }
});

watch(
    () => route.query,
    async () => {
        await loadIncidents();
    },
    { immediate: true, deep: true },
);

async function loadIncidents() {
    const requestId = latestRequestId.value + 1;
    latestRequestId.value = requestId;
    isLoading.value = true;
    const filters = activeFilters.value;

    try {
        const result = await fetchIncidentRegisterPage(apiStore.api, {
            type: filters.type || '',
            incidentDateStart: filters.incidentDateStart || undefined,
            incidentDateEnd: filters.incidentDateEnd || undefined,
            company: filters.company || undefined,
            customer: filters.customer || undefined,
            status: filters.statuses.length ? filters.statuses.join(',') : undefined,
            page: filters.page,
            pageSize: filters.pageSize,
            sortField: filters.sortField,
            sortOrder: filters.sortOrder,
        });

        if (requestId !== latestRequestId.value) {
            return;
        }

        rows.value = result.items;
        totalRecords.value = result.total;
        statusCounts.value = normalizeStatusCounts(result.statusCounts);
        companyOptions.value = result.companies;
        customerOptions.value = result.customers;
        try {
            statusOptions.value = result.statuses?.length
                ? normalizeStatusOptions(result.statuses)
                : await loadStatusOptionsFromAllMatchingIncidents(result.total, filters);
        }
        catch (statusOptionError) {
            if (requestId !== latestRequestId.value) {
                return;
            }
            statusOptions.value = [];
            console.error(statusOptionError);
        }

        if (requestId !== latestRequestId.value) {
            return;
        }

        if (result.page !== filters.page) {
            await router.replace({
                path: '/safety-application/incidents',
                query: buildIncidentQuery({ page: String(result.page) }, filters),
            });
        }
    }
    catch (error) {
        if (requestId !== latestRequestId.value) {
            return;
        }

        rows.value = [];
        totalRecords.value = 0;
        statusCounts.value = [];
        companyOptions.value = [];
        customerOptions.value = [];
        statusOptions.value = [];

        toast.add({
            severity: 'error',
            summary: 'Incident Register Unavailable',
            detail: 'The live incident register could not be loaded from the API.',
            life: 4000,
        });
        console.error(error);
    }
    finally {
        if (requestId === latestRequestId.value) {
            isLoading.value = false;
        }
    }
}

async function updateFilter(key: 'type' | 'company' | 'customer', value: string | null) {
    await router.replace({
        path: '/safety-application/incidents',
        query: buildIncidentQuery({
            [key]: value,
            page: '1',
        }),
    });
}

async function updateStatusFilter(value: string[] | null | undefined) {
    await router.replace({
        path: '/safety-application/incidents',
        query: buildIncidentQuery({
            status: value && value.length ? value : null,
            page: '1',
        }),
    });
}

async function handleDateRangeSelection(value: DateRangePreset | null) {
    if (!value) {
        await router.replace({
            path: '/safety-application/incidents',
            query: buildIncidentQuery({
                dateRange: null,
                incidentDateStart: null,
                incidentDateEnd: null,
                page: '1',
            }),
        });
        return;
    }

    await router.replace({
        path: '/safety-application/incidents',
        query: buildIncidentQuery({
            dateRange: value,
            incidentDateStart: value === 'custom' ? activeFilters.value.customDateStart : null,
            incidentDateEnd: value === 'custom' ? activeFilters.value.customDateEnd : null,
            page: '1',
        }),
    });
}

async function updateCustomDate(key: 'incidentDateStart' | 'incidentDateEnd', value: string | null) {
    await router.replace({
        path: '/safety-application/incidents',
        query: buildIncidentQuery({
            dateRange: 'custom',
            [key]: value,
            page: '1',
        }),
    });
}

async function updatePageSize(nextPageSize: number | null) {
    await router.replace({
        path: '/safety-application/incidents',
        query: buildIncidentQuery({
            page: '1',
            pageSize: String(nextPageSize || incidentRegisterDefaultPageSize),
        }),
    });
}

async function updatePage(nextPage: number) {
    await router.replace({
        path: '/safety-application/incidents',
        query: buildIncidentQuery({
            page: String(nextPage),
        }),
    });
}

async function handleSort(event: { sortField: IncidentRegisterSortField; sortOrder: 1 | -1 }) {
    await router.replace({
        path: '/safety-application/incidents',
        query: buildIncidentQuery({
            page: '1',
            sortField: event.sortField,
            sortOrder: event.sortOrder === 1 ? 'asc' : 'desc',
        }),
    });
}

async function clearFilters() {
    await router.replace({
        path: '/safety-application/incidents',
        query: buildIncidentQuery({
            type: null,
            company: null,
            customer: null,
            status: null,
            dateRange: null,
            incidentDateStart: null,
            incidentDateEnd: null,
            page: '1',
        }),
    });
}

async function clearTypeFilter() {
    await updateFilter('type', null);
}

function viewIncident(incident: IncidentRegisterRecord) {
    void router.push(`/safety-application/incidents/${incident.incidentId}`);
}

function editIncident(incident: IncidentRegisterRecord) {
    void router.push(`/safety-application/incidents/${incident.incidentId}?mode=edit`);
}

function normalizeQueryValue(value: unknown) {
    return typeof value === 'string' ? value : '';
}

function normalizeMultiValueQuery(value: unknown) {
    if (typeof value !== 'string') {
        return [];
    }

    return [...new Set(value
        .split(',')
        .map(item => item.trim())
        .filter(Boolean))];
}

function normalizeDateRangePreset(value: unknown): DateRangePreset | '' {
    if (typeof value !== 'string') {
        return '';
    }

    return dateRangeOptions.some(option => option.value === value as DateRangePreset) ? (value as DateRangePreset) : '';
}

function normalizePositiveNumber(value: unknown, fallback: number) {
    const parsed = Number(value);
    return Number.isFinite(parsed) && parsed > 0 ? Math.trunc(parsed) : fallback;
}

function normalizeString(value: string | null | undefined) {
    return value ? value : null;
}

function buildIncidentQuery(
    overrides: Partial<Record<'type' | 'company' | 'customer' | 'dateRange' | 'incidentDateStart' | 'incidentDateEnd' | 'page' | 'pageSize' | 'sortField' | 'sortOrder', string | null>> & { status?: string[] | string | null },
    baseFilters = activeFilters.value,
) {
    const nextQuery = new Map<string, string>();

    setQueryValue(nextQuery, 'type', baseFilters.type);
    setQueryValue(nextQuery, 'company', baseFilters.company);
    setQueryValue(nextQuery, 'customer', baseFilters.customer);
    setQueryValue(nextQuery, 'status', baseFilters.statuses.join(','));
    setQueryValue(nextQuery, 'dateRange', baseFilters.dateRange);
    setQueryValue(nextQuery, 'incidentDateStart', baseFilters.customDateStart);
    setQueryValue(nextQuery, 'incidentDateEnd', baseFilters.customDateEnd);
    setQueryValue(nextQuery, 'page', String(baseFilters.page));
    setQueryValue(nextQuery, 'pageSize', String(baseFilters.pageSize));
    setQueryValue(nextQuery, 'sortField', baseFilters.sortField);
    setQueryValue(nextQuery, 'sortOrder', baseFilters.sortOrder === 1 ? 'asc' : 'desc');

    for (const [key, value] of Object.entries(overrides)) {
        setQueryValue(nextQuery, key, value ?? null);
    }

    return Object.fromEntries(nextQuery.entries());
}

function resolveDateRange(selectedRange: DateRangePreset | '', start: string, end: string) {
    if (!selectedRange) {
        return { start: '', end: '' };
    }

    if (selectedRange === 'custom') {
        if (!start || !end || start > end) {
            return { start: '', end: '' };
        }

        return { start, end };
    }

    const today = new Date();

    switch (selectedRange) {
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
        default:
            return { start: '', end: '' };
    }
}

function buildRelativeRange(referenceDate: Date, dayCount: number) {
    const endDate = toDateOnlyString(referenceDate);
    const startDate = new Date(referenceDate);
    startDate.setDate(startDate.getDate() - (dayCount - 1));

    return {
        start: toDateOnlyString(startDate),
        end: endDate,
    };
}

function buildQuarterRange(year: number, quarter: 1 | 2 | 3 | 4) {
    const startMonth = (quarter - 1) * 3;
    const startDate = new Date(year, startMonth, 1);
    const endDate = new Date(year, startMonth + 3, 0);

    return {
        start: toDateOnlyString(startDate),
        end: toDateOnlyString(endDate),
    };
}

function toDateOnlyString(value: Date) {
    const year = value.getFullYear();
    const month = String(value.getMonth() + 1).padStart(2, '0');
    const day = String(value.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}

function formatDisplayDate(value: string) {
    return formatDateMMDDYYYY(value);
}

function setQueryValue(query: Map<string, string>, key: string, value: string | string[] | null) {
    const normalizedValue = Array.isArray(value)
        ? value.map(item => item.trim()).filter(Boolean).join(',')
        : value?.trim();

    if (normalizedValue) {
        query.set(key, normalizedValue);
        return;
    }

    query.delete(key);
}

function normalizeOptions(options: IncidentRegisterOption[] | undefined) {
    const normalizedOptions = (options || [])
        .map(option => ({
            label: option.label?.trim() || '',
            value: option.value?.trim() || '',
        }))
        .filter(option => option.value);

    return dedupeOptions(normalizedOptions);
}

function normalizeStatusOptions(options: IncidentRegisterOption[] | undefined) {
    return normalizeOptions(options).map(option => ({
        ...option,
        label: displayStatusLabel(option.label),
    }));
}

function dedupeOptions(options: IncidentRegisterOption[]) {
    return [...new Map(options.map(option => [option.value, option])).values()].sort((a, b) =>
        a.label.localeCompare(b.label),
    );
}

async function loadStatusOptionsFromAllMatchingIncidents(total: number, filters = activeFilters.value) {
    const aggregateOptions: IncidentRegisterOption[] = [];
    const aggregatePageSize = 100;
    const pageCount = Math.max(1, Math.ceil(total / aggregatePageSize));

    for (let currentPage = 1; currentPage <= pageCount; currentPage += 1) {
        const pageResult = await fetchIncidentRegisterPage(apiStore.api, {
            type: filters.type || '',
            incidentDateStart: filters.incidentDateStart || undefined,
            incidentDateEnd: filters.incidentDateEnd || undefined,
            company: filters.company || undefined,
            customer: filters.customer || undefined,
            status: filters.statuses.length ? filters.statuses.join(',') : undefined,
            page: currentPage,
            pageSize: aggregatePageSize,
            sortField: filters.sortField,
            sortOrder: filters.sortOrder,
        });

        aggregateOptions.push(
            ...pageResult.items
                .map(item => item.status.trim())
                .filter(Boolean)
                .map(value => ({ label: value, value })),
        );
    }

    return normalizeStatusOptions(dedupeOptions(aggregateOptions));
}

function handleSortToggle(field: IncidentRegisterSortField) {
    void handleSort({
        sortField: field,
        sortOrder: sortField.value === field && sortOrder.value === 1 ? -1 : 1,
    });
}

function sortIcon(field: IncidentRegisterSortField) {
    if (sortField.value !== field) return 'pi pi-sort-alt text-slate-500';
    return sortOrder.value === 1 ? 'pi pi-sort-amount-up-alt text-blue-300' : 'pi pi-sort-amount-down text-blue-300';
}

function statusTone(value: IncidentRegisterRecord['status']) {
    if (value === 'Open') return 'warning';
    if (value === 'In Review' || value === 'Investigation') return 'info';
    return 'success';
}

function normalizeStatusCounts(counts: IncidentRegisterStatusCount[] | undefined) {
    return (counts || []).filter(statusCount => statusCount.count > 0);
}

function displayStatusLabel(status: string) {
    return status === 'Investigation' ? 'Active Investigation' : status;
}

function statusDisplayOrder(status: string) {
    if (status === 'Open') return 1;
    if (status === 'Investigation') return 2;
    if (status === 'In Review') return 3;
    if (status === 'Closed') return 4;
    return 10;
}

</script>
