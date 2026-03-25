<template>
    <div class="space-y-6">
        <BasePageHeader
            :title="apps.safetyApplication.name"
            :subtitle="apps.safetyApplication.description"
            :icon="apps.safetyApplication.icon"
        >
            <Tag value="Operations Dashboard" severity="info" class="!rounded-full !bg-blue-500/15 !px-3 !py-2 !text-sm !font-semibold !text-blue-200" />
        </BasePageHeader>

        <div>
            <BasePageSubheader title="Safety KPIs" />
            <div
                v-if="appliedDashboardFilters.length"
                class="mt-4 rounded-2xl border border-slate-700/70 bg-slate-800/70 px-5 py-4 shadow-lg shadow-slate-950/20"
            >
                <div class="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                    <div>
                        <p class="m-0 text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">Applied Filters</p>
                        <p class="m-0 mt-1 text-sm text-slate-300">Dashboard KPIs are using your current filter context. Remove a chip to temporarily override a saved profile default for this view.</p>
                    </div>
                    <div class="flex flex-wrap items-center gap-2">
                        <button
                            v-for="filter in appliedDashboardFilters"
                            :key="filter.key"
                            type="button"
                            class="inline-flex items-center gap-2 rounded-full border border-slate-600 bg-slate-900/80 px-3 py-2 text-sm text-slate-100 transition hover:border-slate-500 hover:bg-slate-900"
                            @click="removeFilterChip(filter)"
                        >
                            <span class="font-semibold text-slate-200">{{ filter.label }}:</span>
                            <span>{{ filter.value }}</span>
                            <i class="pi pi-times text-xs text-slate-400" />
                        </button>
                    </div>
                </div>
            </div>
            <div class="mt-4 grid gap-4 md:grid-cols-2 xl:grid-cols-5">
                <SafetyMetricCard v-for="metric in metrics" :key="metric.label" v-bind="metric" />
            </div>
        </div>

        <div class="grid gap-4 xl:grid-cols-[1.25fr_0.75fr]">
            <Card class="border border-slate-700/70 bg-slate-800/70 shadow-lg shadow-slate-950/20">
                <template #content>
                    <div class="space-y-5 p-6">
                        <div class="flex items-start justify-between gap-4">
                            <div>
                                <p class="mb-2 text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">Trending By Month</p>
                                <h3 class="m-0 text-xl font-semibold text-white">Incident trend snapshot</h3>
                            </div>
                        </div>
                        <Chart type="line" :data="trendData" :options="trendOptions" class="h-[320px]" />
                    </div>
                </template>
            </Card>

            <div>
                <Card class="border border-slate-700/70 bg-slate-800/70 shadow-lg shadow-slate-950/20">
                    <template #content>
                        <div class="space-y-5 p-6">
                            <div>
                                <p class="mb-2 text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">Operational Highlights</p>
                                <h3 class="m-0 text-xl font-semibold text-white">Current watch items</h3>
                            </div>
                            <div class="space-y-4">
                                <div v-for="highlight in highlights" :key="highlight.title" class="rounded-2xl border border-slate-700/70 bg-slate-900/50 p-4">
                                    <div class="flex items-start justify-between gap-3">
                                        <div>
                                            <h4 class="m-0 text-base font-semibold text-white">{{ highlight.title }}</h4>
                                            <p class="mt-2 text-sm leading-6 text-slate-400">{{ highlight.description }}</p>
                                        </div>
                                        <Tag :value="highlight.badge" :severity="highlight.severity" class="!rounded-full !px-3 !py-1 !text-xs !font-semibold" />
                                    </div>
                                </div>
                            </div>
                        </div>
                    </template>
                </Card>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { apps } from '@/apps.ts';
import BasePageHeader from '@/components/layout/BasePageHeader.vue';
import BasePageSubheader from '@/components/layout/BasePageSubheader.vue';
import SafetyMetricCard from '@/modules/safety-application/features/dashboard/components/SafetyMetricCard.vue';
import {
    fetchIncidentDashboardSummary,
    fetchIncidentTrendByMonth,
    type IncidentDashboardSummary,
    type IncidentTrendPoint,
} from '@/modules/safety-application/features/dashboard/services/incidentRegisterService';
import {
    buildIncidentQueryFromProfile,
    fetchMyProfile,
    profileDateRangeOptions,
    resolveProfileDateRange,
    type UserProfileSettings,
} from '@/modules/safety-application/features/dashboard/services/userProfileService';
import { useApiStore } from '@/stores/apiStore';
import Chart from 'primevue/chart';
import Tag from 'primevue/tag';
import { computed, onMounted, ref } from 'vue';

const apiStore = useApiStore();

type AppliedDashboardFilterState = {
    defaultDateRange?: UserProfileSettings['defaultDateRange'];
    defaultCompany?: string;
    defaultCustomer?: string;
    defaultIncidentStatuses: string[];
};

type AppliedFilterChip = {
    key: string;
    label: string;
    value: string;
    remove: () => void;
};

const incidentDashboardSummary = ref<IncidentDashboardSummary>({
    totalIncidents: 0,
    occurredLast30Days: 0,
    openInvestigations: 0,
    openInvestigationsOver30Days: 0,
    recordableIncidents: 0,
    lostTimeIncidents: 0,
    nearMisses: 0,
});
const incidentTrendPoints = ref<IncidentTrendPoint[]>([]);
const profileDefaults = ref<UserProfileSettings | null>(null);
const dashboardFilters = ref<AppliedDashboardFilterState>(createDashboardFilterState());
const appliedDashboardFilters = computed<AppliedFilterChip[]>(() => {
    const filters: AppliedFilterChip[] = [];

    if (dashboardFilters.value.defaultDateRange) {
        filters.push({
            key: `date-range:${dashboardFilters.value.defaultDateRange}`,
            label: 'Date Range',
            value: displayDateRangeLabel(dashboardFilters.value.defaultDateRange),
            remove: () => {
                dashboardFilters.value.defaultDateRange = undefined;
            },
        });
    }

    if (dashboardFilters.value.defaultCompany) {
        filters.push({
            key: `company:${dashboardFilters.value.defaultCompany}`,
            label: 'Company',
            value: dashboardFilters.value.defaultCompany,
            remove: () => {
                dashboardFilters.value.defaultCompany = undefined;
            },
        });
    }

    if (dashboardFilters.value.defaultCustomer) {
        filters.push({
            key: `customer:${dashboardFilters.value.defaultCustomer}`,
            label: 'Customer',
            value: dashboardFilters.value.defaultCustomer,
            remove: () => {
                dashboardFilters.value.defaultCustomer = undefined;
            },
        });
    }

    for (const status of dashboardFilters.value.defaultIncidentStatuses) {
        filters.push({
            key: `status:${status}`,
            label: 'Status',
            value: displayStatusLabel(status),
            remove: () => {
                dashboardFilters.value.defaultIncidentStatuses = dashboardFilters.value.defaultIncidentStatuses.filter(item => item !== status);
            },
        });
    }

    return filters;
});

const metrics = computed(() => [
    {
        label: 'Total Incidents',
        value: incidentDashboardSummary.value.totalIncidents,
        icon: 'pi pi-briefcase',
        trend:
            incidentDashboardSummary.value.occurredLast30Days > 0
                ? `${incidentDashboardSummary.value.occurredLast30Days} occurred in the last 30 days`
                : undefined,
        trendDirection: 'up' as const,
        tooltip: 'Total number of incident records currently available for review.',
        to: { path: '/safety-application/incidents', query: buildIncidentQueryFromProfile(toUserProfileSettings(dashboardFilters.value)) },
    },
    {
        label: 'Open Investigations',
        value: incidentDashboardSummary.value.openInvestigations,
        icon: 'pi pi-search',
        trend:
            incidentDashboardSummary.value.openInvestigationsOver30Days > 0
                ? `${incidentDashboardSummary.value.openInvestigationsOver30Days} investigation incidents are over 30 days old`
                : undefined,
        trendDirection: 'neutral' as const,
        tooltip: 'Investigations with Open status across all data.',
        to: { path: '/safety-application/investigations', query: { status: 'Open' } },
    },
    {
        label: 'Recordable Incidents',
        value: incidentDashboardSummary.value.recordableIncidents,
        icon: 'pi pi-exclamation-circle',
        tooltip: 'Incidents classified as OSHA recordable events.',
        to: {
            path: '/safety-application/incidents',
            query: buildIncidentQueryFromProfile(toUserProfileSettings(dashboardFilters.value), { type: 'recordable' }),
        },
    },
    {
        label: 'Lost Time Incidents',
        value: incidentDashboardSummary.value.lostTimeIncidents,
        icon: 'pi pi-clock',
        tooltip: 'Incidents that resulted in lost work time.',
        to: {
            path: '/safety-application/incidents',
            query: buildIncidentQueryFromProfile(toUserProfileSettings(dashboardFilters.value), { type: 'lost-time' }),
        },
    },
    {
        label: 'Near Misses',
        value: incidentDashboardSummary.value.nearMisses,
        icon: 'pi pi-flag',
        tooltip: 'Near miss reports captured for proactive safety review.',
        to: {
            path: '/safety-application/incidents',
            query: buildIncidentQueryFromProfile(toUserProfileSettings(dashboardFilters.value), { type: 'near-miss' }),
        },
    },
]);

onMounted(async () => {
    try {
        const [profile, trend] = await Promise.all([
            fetchMyProfile(apiStore.api).catch(() => null),
            fetchIncidentTrendByMonth(apiStore.api),
        ]);

        profileDefaults.value = profile;
        dashboardFilters.value = createDashboardFilterState(profile);
        await refreshSummary();
        incidentTrendPoints.value = trend;
    }
    catch (error) {
        console.error(error);
    }
});

const highlights = [
    {
        title: 'Investigation backlog',
        description: 'A small group of open investigations is approaching the internal follow-up target and should be reviewed by safety leadership.',
        badge: 'Attention',
        severity: 'warning',
    },
    {
        title: 'Reference data readiness',
        description: 'Lookup management is available for maintaining the controlled values that support incident and investigation forms.',
        badge: 'Ready',
        severity: 'success',
    },
];

const trendData = computed(() => ({
    labels: incidentTrendPoints.value.map(point => point.monthLabel),
    datasets: [
        {
            label: 'Incidents',
            data: incidentTrendPoints.value.map(point => point.incidentCount),
            borderColor: '#60a5fa',
            backgroundColor: 'rgba(96, 165, 250, 0.18)',
            fill: true,
            tension: 0.35,
        },
        {
            label: 'Near Misses',
            data: incidentTrendPoints.value.map(point => point.nearMissCount),
            borderColor: '#34d399',
            backgroundColor: 'rgba(52, 211, 153, 0.08)',
            fill: false,
            tension: 0.35,
        },
    ],
}));

const trendOptions = {
    maintainAspectRatio: false,
    plugins: {
        legend: {
            labels: {
                color: '#cbd5e1',
            },
        },
    },
    scales: {
        x: {
            ticks: {
                color: '#94a3b8',
            },
            grid: {
                color: 'rgba(148, 163, 184, 0.12)',
            },
        },
        y: {
            ticks: {
                color: '#94a3b8',
            },
            grid: {
                color: 'rgba(148, 163, 184, 0.12)',
            },
        },
    },
};

function displayDateRangeLabel(value: NonNullable<UserProfileSettings['defaultDateRange']>) {
    return profileDateRangeOptions.find(option => option.value === value)?.label || value;
}

function displayStatusLabel(value: string) {
    return value === 'Investigation' ? 'Active Investigation' : value;
}

async function refreshSummary() {
    const activeFilters = dashboardFilters.value;
    incidentDashboardSummary.value = await fetchIncidentDashboardSummary(apiStore.api, {
        ...resolveProfileDateRange(activeFilters.defaultDateRange),
        company: activeFilters.defaultCompany,
        customer: activeFilters.defaultCustomer,
        status: activeFilters.defaultIncidentStatuses.length
            ? activeFilters.defaultIncidentStatuses.join(',')
            : undefined,
    });
}

async function removeFilterChip(filter: AppliedFilterChip) {
    filter.remove();
    await refreshSummary();
}

function createDashboardFilterState(profile?: UserProfileSettings | null): AppliedDashboardFilterState {
    return {
        defaultDateRange: profile?.defaultDateRange,
        defaultCompany: profile?.defaultCompany,
        defaultCustomer: profile?.defaultCustomer,
        defaultIncidentStatuses: [...(profile?.defaultIncidentStatuses || [])],
    };
}

function toUserProfileSettings(filters: AppliedDashboardFilterState): UserProfileSettings {
    return {
        profileId: profileDefaults.value?.profileId || 0,
        userId: profileDefaults.value?.userId || 0,
        defaultDateRange: filters.defaultDateRange,
        defaultCompany: filters.defaultCompany,
        defaultCustomer: filters.defaultCustomer,
        defaultIncidentStatuses: filters.defaultIncidentStatuses,
    };
}
</script>
