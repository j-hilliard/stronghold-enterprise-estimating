<template>
    <div class="fd-page">
        <!-- Top controls bar -->
        <div class="fd-topbar">
            <div class="fd-topbar-left">
                <div class="fd-view-tabs">
                    <button
                        v-for="tab in viewTabs"
                        :key="tab.value"
                        class="fd-view-tab"
                        :class="{ active: activeView === tab.value }"
                        @click="activeView = tab.value"
                    >{{ tab.label }}</button>
                </div>
                <Dropdown
                    v-model="selectedBranch"
                    :options="branchOptions"
                    optionLabel="label"
                    optionValue="value"
                    class="fd-branch-dropdown"
                    data-testid="fd-branch"
                />
            </div>
            <div class="fd-topbar-right">
                <Button label="Export PDF" icon="pi pi-file-pdf" text size="small" class="fd-topbar-btn" v-tooltip.bottom="'Print to PDF via browser'" @click="() => window.print()" />
                <Button label="Export CSV" icon="pi pi-download" text size="small" class="fd-topbar-btn" data-testid="fd-export" @click="exportCsv" />
                <Button icon="pi pi-refresh" text size="small" class="fd-topbar-btn" :loading="loading" data-testid="fd-refresh" @click="loadData" />
                <Dropdown
                    v-model="selectedYear"
                    :options="yearOptions"
                    optionLabel="label"
                    optionValue="value"
                    class="fd-year-dropdown"
                    data-testid="fd-year"
                />
            </div>
        </div>

        <div v-if="loading" class="flex justify-content-center py-8">
            <ProgressSpinner />
        </div>
        <Message v-else-if="errorMessage" severity="error" :closable="false">{{ errorMessage }}</Message>

        <template v-else>
            <!-- FINANCIAL DASHBOARD section -->
            <div class="fd-section">
                <div class="fd-section-header" @click="dashCollapsed = !dashCollapsed">
                    <span class="fd-section-title">FINANCIAL DASHBOARD</span>
                    <div class="fd-section-header-right">
                        <Dropdown
                            v-model="selectedYear"
                            :options="yearOptions"
                            optionLabel="label"
                            optionValue="value"
                            class="fd-inner-year-dropdown"
                            @click.stop
                        />
                        <i :class="dashCollapsed ? 'pi pi-chevron-right' : 'pi pi-chevron-down'" class="fd-collapse-icon" />
                    </div>
                </div>

                <div v-show="!dashCollapsed" class="fd-section-body">
                    <div class="fd-dashboard-columns">
                        <!-- Left column (70%) -->
                        <div class="fd-left-col">
                            <!-- Time period tabs -->
                            <div class="fd-period-tabs">
                                <button
                                    v-for="tp in timePeriodTabs"
                                    :key="tp.value"
                                    class="fd-period-tab"
                                    :class="{ active: timePeriod === tp.value }"
                                    @click="timePeriod = tp.value"
                                >{{ tp.label }}</button>
                            </div>

                            <!-- KPI row 1 -->
                            <div class="fd-kpi-grid fd-kpi-grid-5">
                                <div class="fd-kpi-card fd-kpi-clickable" :class="{ 'fd-kpi-active': drillFilter === 'awarded' }" @click="toggleDrill('awarded')">
                                    <span class="fd-kpi-label">AWARDED REVENUE <i class="pi pi-angle-down fd-drill-icon" /></span>
                                    <span class="fd-kpi-value fd-green">{{ fmtM(kpis.awardedAmount) }}</span>
                                    <span class="fd-kpi-sub">{{ kpis.awardedCount }} jobs won</span>
                                </div>
                                <div class="fd-kpi-card fd-kpi-clickable" :class="{ 'fd-kpi-active': drillFilter === 'pending' }" @click="toggleDrill('pending')">
                                    <span class="fd-kpi-label">PENDING PIPELINE <i class="pi pi-angle-down fd-drill-icon" /></span>
                                    <span class="fd-kpi-value fd-blue">{{ fmtM(kpis.pendingAmount) }}</span>
                                    <span class="fd-kpi-sub">{{ kpis.pendingCount }} active bids</span>
                                </div>
                                <div class="fd-kpi-card fd-kpi-clickable" :class="{ 'fd-kpi-active': drillFilter === 'lost' }" @click="toggleDrill('lost')">
                                    <span class="fd-kpi-label">LOST REVENUE <i class="pi pi-angle-down fd-drill-icon" /></span>
                                    <span class="fd-kpi-value fd-red">{{ fmtM(kpis.lostAmount) }}</span>
                                    <span class="fd-kpi-sub">{{ kpis.lostCount }} jobs lost</span>
                                </div>
                                <div class="fd-kpi-card fd-kpi-clickable" :class="{ 'fd-kpi-active': drillFilter === 'total' }" @click="toggleDrill('total')">
                                    <span class="fd-kpi-label">TOTAL PIPELINE <i class="pi pi-angle-down fd-drill-icon" /></span>
                                    <span class="fd-kpi-value fd-white">{{ fmtM(kpis.totalPipeline) }}</span>
                                    <span class="fd-kpi-sub">{{ kpis.totalCount }} total jobs</span>
                                </div>
                                <div class="fd-kpi-card">
                                    <span class="fd-kpi-label">WEIGHTED PIPELINE</span>
                                    <span class="fd-kpi-value fd-orange">{{ fmtM(kpis.weightedPipeline) }}</span>
                                    <span class="fd-kpi-sub">w/ {{ pendingWeight }}% pending</span>
                                </div>
                            </div>

                            <!-- Drill-down panel -->
                            <div v-if="drillFilter && drillEstimates.length > 0" class="fd-drill-panel">
                                <div class="fd-drill-header">
                                    <span class="fd-drill-title">{{ drillTitle }} — {{ drillEstimates.length }} job{{ drillEstimates.length !== 1 ? 's' : '' }}</span>
                                    <button class="fd-drill-close" @click="drillFilter = null"><i class="pi pi-times" /></button>
                                </div>
                                <div class="fd-drill-scroll">
                                    <table class="fd-drill-table">
                                        <thead>
                                            <tr>
                                                <th>JOB #</th>
                                                <th>JOB NAME</th>
                                                <th>CLIENT</th>
                                                <th>STATUS</th>
                                                <th>START</th>
                                                <th>END</th>
                                                <th>DAYS</th>
                                                <th class="text-right">TOTAL</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            <tr
                                                v-for="est in drillEstimates"
                                                :key="est.estimateId"
                                                class="fd-drill-row"
                                                @click="router.push(`/estimating/estimates/${est.estimateId}`)"
                                            >
                                                <td class="drill-num">{{ est.estimateNumber }}</td>
                                                <td class="drill-name">{{ est.name }}</td>
                                                <td class="drill-client">{{ est.client }}</td>
                                                <td><span class="drill-status" :class="`status-${(est.status ?? '').toLowerCase()}`">{{ est.status }}</span></td>
                                                <td class="drill-date">{{ fmtDate(est.startDate ?? undefined) }}</td>
                                                <td class="drill-date">{{ fmtDate(est.endDate ?? undefined) }}</td>
                                                <td class="text-right">{{ drillDays(est) }}</td>
                                                <td class="text-right drill-total">{{ fmtM(Number(est.grandTotal ?? 0)) }}</td>
                                            </tr>
                                        </tbody>
                                        <tfoot>
                                            <tr class="drill-foot">
                                                <td colspan="7" class="text-right">TOTAL</td>
                                                <td class="text-right drill-total">{{ fmtM(drillEstimates.reduce((s, e) => s + Number(e.grandTotal ?? 0), 0)) }}</td>
                                            </tr>
                                        </tfoot>
                                    </table>
                                </div>
                            </div>

                            <!-- KPI row 2 -->
                            <div class="fd-kpi-grid fd-kpi-grid-3">
                                <div class="fd-kpi-card">
                                    <span class="fd-kpi-label">AVG MARGIN</span>
                                    <span class="fd-kpi-value" :class="marginColor">{{ fmtPct(kpis.avgMargin) }}</span>
                                    <span class="fd-kpi-sub">on awarded jobs</span>
                                </div>
                                <div class="fd-kpi-card">
                                    <span class="fd-kpi-label">FUTURE REVENUE</span>
                                    <span class="fd-kpi-value fd-purple">{{ fmtM(kpis.futureRevenue) }}</span>
                                    <span class="fd-kpi-sub">{{ kpis.staffingPlanCount }} staffing plans</span>
                                </div>
                                <div class="fd-kpi-card">
                                    <span class="fd-kpi-label">FUTURE PROFIT</span>
                                    <span class="fd-kpi-value fd-green">{{ fmtM(kpis.futureProfit) }}</span>
                                    <span class="fd-kpi-sub">{{ fmtPct(kpis.avgMargin) }} avg margin</span>
                                </div>
                            </div>
                        </div>

                        <!-- Right column (30%) -->
                        <div class="fd-right-col">
                            <div class="fd-manpower-card">
                                <div class="fd-manpower-heading">Manpower Forecast</div>
                                <div class="fd-manpower-sub">Forecast staffing needs from Staffing Plans by week, month, quarter, 6 months, and year.</div>
                                <a class="fd-manpower-link" href="#" @click.prevent="router.push('/estimating/analytics/manpower')">Open report &rarr;</a>
                                <div class="fd-slider-row">
                                    <span class="fd-slider-label">PENDING WEIGHT:</span>
                                    <div class="fd-slider-wrap">
                                        <Slider v-model="pendingWeight" :min="0" :max="100" class="fd-slider" />
                                        <span class="fd-slider-val">{{ pendingWeight }}%</span>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- REVENUE VS COST ANALYSIS section -->
            <div class="fd-section">
                <div class="fd-section-header" @click="chartCollapsed = !chartCollapsed">
                    <span class="fd-section-title">REVENUE VS COST ANALYSIS</span>
                    <div class="fd-section-header-right">
                        <Dropdown
                            v-model="selectedCustomer"
                            :options="customerOptions"
                            optionLabel="label"
                            optionValue="value"
                            class="fd-inner-dropdown"
                            @click.stop
                        />
                        <i :class="chartCollapsed ? 'pi pi-chevron-right' : 'pi pi-chevron-down'" class="fd-collapse-icon" />
                    </div>
                </div>

                <div v-show="!chartCollapsed" class="fd-section-body">
                    <div class="fd-chart-controls">
                        <div class="fd-toggle-group">
                            <button
                                v-for="cm in chartModes"
                                :key="cm.value"
                                class="fd-toggle-btn"
                                :class="{ active: chartMode === cm.value }"
                                @click="chartMode = cm.value"
                            >{{ cm.label }}</button>
                        </div>
                    </div>
                    <div class="fd-chart-container">
                        <Chart
                            type="line"
                            :data="revenueChartData"
                            :options="revenueChartOptions"
                            data-testid="fd-revenue-chart"
                        />
                    </div>
                </div>
            </div>

            <!-- WIN/LOSS BREAKDOWN section -->
            <div class="fd-section">
                <div class="fd-section-header" @click="winlossCollapsed = !winlossCollapsed">
                    <span class="fd-section-title">WIN / LOSS BREAKDOWN</span>
                    <i :class="winlossCollapsed ? 'pi pi-chevron-right' : 'pi pi-chevron-down'" class="fd-collapse-icon" />
                </div>

                <div v-show="!winlossCollapsed" class="fd-section-body">
                    <div class="fd-winloss-row">
                        <div class="fd-winloss-stat">
                            <span class="fd-winloss-label">WON</span>
                            <span class="fd-winloss-count fd-green">{{ kpis.awardedCount }}</span>
                        </div>
                        <div class="fd-winloss-stat">
                            <span class="fd-winloss-label">LOST</span>
                            <span class="fd-winloss-count fd-red">{{ kpis.lostCount }}</span>
                        </div>
                        <div class="fd-winloss-stat">
                            <span class="fd-winloss-label">PENDING</span>
                            <span class="fd-winloss-count fd-orange">{{ kpis.pendingCount }}</span>
                        </div>
                        <div class="fd-winloss-rate-block">
                            <span class="fd-winloss-rate-label">WIN RATE</span>
                            <span class="fd-winloss-rate" :class="winRateColor">{{ fmtPct(winRate) }}</span>
                        </div>
                    </div>
                </div>
            </div>
        </template>
    </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useApiStore } from '@/stores/apiStore';
import type {
    EstimateListItem,
    RevenueRow,
    StaffingListItem,
} from '@/modules/estimating/features/analytics/utils/forecast';
import {
    buildRevenueRows,
    groupRevenueByPeriod,
    round2,
    safeDate,
} from '@/modules/estimating/features/analytics/utils/forecast';

type EstimateDetailResponse = {
    estimateId: number;
    fcoEntries?: Array<{ dollarAdjustment?: number | null }>;
};

type TimePeriod = 'MTD' | 'QTD' | 'YTD' | 'ALL';
type ChartMode = 'monthly' | 'quarterly' | 'cumulative';

const router = useRouter();
const apiStore = useApiStore();

// ── UI state ──────────────────────────────────────────────────────────────────
const loading = ref(false);
const errorMessage = ref('');
const activeView = ref('overview');
const selectedBranch = ref('all');
const selectedCustomer = ref('all');
const timePeriod = ref<TimePeriod>('YTD');
const chartMode = ref<ChartMode>('monthly');
const pendingWeight = ref(50);
const dashCollapsed = ref(false);
const chartCollapsed = ref(false);
const winlossCollapsed = ref(false);

const currentYear = new Date().getFullYear();
const selectedYear = ref(currentYear);

// ── Raw data ──────────────────────────────────────────────────────────────────
const estimates = ref<EstimateListItem[]>([]);
const staffingPlans = ref<StaffingListItem[]>([]);
const fcoByEstimateId = ref<Record<number, number>>({});

// ── Static option lists ───────────────────────────────────────────────────────
const viewTabs = [
    { label: 'Overview', value: 'overview' },
    { label: 'All Jobs', value: 'alljobs' },
    { label: 'Compare', value: 'compare' },
];

const timePeriodTabs = [
    { label: 'MTD', value: 'MTD' as TimePeriod },
    { label: 'QTD', value: 'QTD' as TimePeriod },
    { label: 'YTD', value: 'YTD' as TimePeriod },
    { label: 'ALL', value: 'ALL' as TimePeriod },
];

const chartModes = [
    { label: 'Monthly', value: 'monthly' as ChartMode },
    { label: 'Quarterly', value: 'quarterly' as ChartMode },
    { label: 'Cumulative', value: 'cumulative' as ChartMode },
];

const branchOptions = [{ label: 'All Branches', value: 'all' }];
const customerOptions = [{ label: 'All Customers', value: 'all' }];

const yearOptions = computed(() => {
    const years: Array<{ label: string; value: number }> = [];
    for (let y = currentYear - 2; y <= currentYear + 2; y++) {
        years.push({ label: String(y), value: y });
    }
    return years;
});

// ── Date range helpers ────────────────────────────────────────────────────────
const periodDateRange = computed<{ from: Date; to: Date }>(() => {
    const now = new Date();
    const y = selectedYear.value;

    if (timePeriod.value === 'MTD') {
        return {
            from: new Date(now.getFullYear(), now.getMonth(), 1),
            to: now,
        };
    }
    if (timePeriod.value === 'QTD') {
        const q = Math.floor(now.getMonth() / 3);
        return {
            from: new Date(now.getFullYear(), q * 3, 1),
            to: now,
        };
    }
    if (timePeriod.value === 'YTD') {
        return {
            from: new Date(y, 0, 1),
            to: new Date(y, 11, 31, 23, 59, 59),
        };
    }
    // ALL
    return {
        from: new Date(2000, 0, 1),
        to: new Date(2099, 11, 31),
    };
});

// ── Filtered estimates for current period ─────────────────────────────────────
const filteredEstimates = computed<EstimateListItem[]>(() => {
    const { from, to } = periodDateRange.value;
    return estimates.value.filter(e => {
        if (timePeriod.value === 'ALL') return true;
        if (!e.startDate) return false;
        const d = safeDate(e.startDate);
        if (!d) return false;
        return d >= from && d <= to;
    });
});

// ── Revenue rows (all time, for chart) ────────────────────────────────────────
const allRevenueRows = computed<RevenueRow[]>(() =>
    buildRevenueRows(
        estimates.value,
        staffingPlans.value,
        fcoByEstimateId.value,
        new Date(currentYear, 0, 1),
        new Date(currentYear, 11, 31, 23, 59, 59),
    ),
);

// ── KPI computations ──────────────────────────────────────────────────────────
const kpis = computed(() => {
    const awarded = filteredEstimates.value.filter(e => {
        const s = (e.status ?? '').toLowerCase();
        return s === 'awarded' || s === 'active';
    });
    const pending = filteredEstimates.value.filter(e => {
        const s = (e.status ?? '').toLowerCase();
        return s === 'pending' || s === 'proposed' || s === 'draft';
    });
    const lost = filteredEstimates.value.filter(e => {
        const s = (e.status ?? '').toLowerCase();
        return s === 'lost';
    });

    const awardedAmount = round2(awarded.reduce((s, e) => s + Number(e.grandTotal ?? 0), 0));
    const pendingAmount = round2(pending.reduce((s, e) => s + Number(e.grandTotal ?? 0), 0));
    const lostAmount = round2(lost.reduce((s, e) => s + Number(e.grandTotal ?? 0), 0));
    const totalPipeline = round2(awardedAmount + pendingAmount + lostAmount);
    const weightedPipeline = round2(awardedAmount + pendingAmount * (pendingWeight.value / 100));
    const totalCount = filteredEstimates.value.length;

    // Staffing plans
    const nonConvertedSPs = staffingPlans.value.filter(sp => !sp.convertedEstimateId);
    const futureRevenue = round2(nonConvertedSPs.reduce((s, sp) => s + Number(sp.roughLaborTotal ?? 0), 0));
    const avgMargin = 0; // stub — no margin data in EstimateListItem
    const futureProfit = round2(futureRevenue * (avgMargin / 100));

    return {
        awardedAmount,
        awardedCount: awarded.length,
        pendingAmount,
        pendingCount: pending.length,
        lostAmount,
        lostCount: lost.length,
        totalPipeline,
        totalCount,
        weightedPipeline,
        avgMargin,
        futureRevenue,
        futureProfit,
        staffingPlanCount: nonConvertedSPs.length,
    };
});

const marginColor = computed(() => {
    const m = kpis.value.avgMargin;
    if (m > 25) return 'fd-green';
    if (m > 15) return 'fd-yellow';
    return 'fd-red';
});

// ── KPI drill-down ────────────────────────────────────────────────────────────
type DrillFilter = 'awarded' | 'pending' | 'lost' | 'total' | null;
const drillFilter = ref<DrillFilter>(null);

function toggleDrill(f: DrillFilter) {
    drillFilter.value = drillFilter.value === f ? null : f;
}

const drillEstimates = computed(() => {
    if (!drillFilter.value) return [];
    return filteredEstimates.value.filter(e => {
        const s = (e.status ?? '').toLowerCase();
        if (drillFilter.value === 'awarded') return s === 'awarded' || s === 'active';
        if (drillFilter.value === 'pending') return s === 'pending' || s === 'proposed' || s === 'draft' || s === 'submitted';
        if (drillFilter.value === 'lost')    return s === 'lost';
        if (drillFilter.value === 'total')   return true;
        return false;
    }).sort((a, b) => Number(b.grandTotal ?? 0) - Number(a.grandTotal ?? 0));
});

const drillTitle = computed(() => {
    if (drillFilter.value === 'awarded') return 'Awarded Jobs';
    if (drillFilter.value === 'pending') return 'Pending Pipeline';
    if (drillFilter.value === 'lost')    return 'Lost Jobs';
    if (drillFilter.value === 'total')   return 'All Jobs';
    return '';
});

function drillDays(est: EstimateListItem): string | number {
    if (est.startDate && est.endDate) {
        const s = safeDate(est.startDate);
        const e = safeDate(est.endDate);
        if (s && e) return Math.round((e.getTime() - s.getTime()) / 86400000) + 1;
    }
    return '--';
}

const winRate = computed(() => {
    const won = kpis.value.awardedCount;
    const lost = kpis.value.lostCount;
    if (won + lost === 0) return 0;
    return (won / (won + lost)) * 100;
});

const winRateColor = computed(() => {
    const r = winRate.value;
    if (r > 70) return 'fd-green';
    if (r > 50) return 'fd-yellow';
    return 'fd-red';
});

// ── Chart data ─────────────────────────────────────────────────────────────────
const periodRows = computed(() => {
    const chartPeriod = chartMode.value === 'quarterly' ? 'quarter' : 'month';
    return groupRevenueByPeriod(allRevenueRows.value, chartPeriod);
});

const revenueChartData = computed(() => {
    const labels = periodRows.value.map(r => r.label);

    let awardedData = periodRows.value.map(r => r.awardedAmount);
    let pipelineData = periodRows.value.map(r => r.pipelineAmount);
    let lostData = periodRows.value.map(r => r.lostAmount);

    if (chartMode.value === 'cumulative') {
        awardedData = cumSum(awardedData);
        pipelineData = cumSum(pipelineData);
        lostData = cumSum(lostData);
    }

    return {
        labels,
        datasets: [
            {
                label: 'Awarded Revenue',
                borderColor: '#22c55e',
                backgroundColor: 'rgba(34,197,94,0.1)',
                pointBackgroundColor: '#22c55e',
                tension: 0.3,
                fill: false,
                data: awardedData,
            },
            {
                label: 'Pipeline',
                borderColor: '#f97316',
                backgroundColor: 'rgba(249,115,22,0.08)',
                borderDash: [5, 5],
                tension: 0.3,
                fill: false,
                data: pipelineData,
            },
            {
                label: 'Lost Revenue',
                borderColor: '#f87171',
                backgroundColor: 'rgba(248,113,113,0.08)',
                tension: 0.3,
                fill: false,
                data: lostData,
            },
        ],
    };
});

const revenueChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
        legend: {
            labels: { color: '#cbd5e1' },
        },
    },
    scales: {
        x: {
            ticks: { color: '#94a3b8' },
            grid: { color: 'rgba(148,163,184,0.15)' },
        },
        y: {
            ticks: {
                color: '#94a3b8',
                callback: (value: number | string) => fmtM(Number(value)),
            },
            grid: { color: 'rgba(148,163,184,0.15)' },
        },
    },
};

// ── Detail rows for CSV export ────────────────────────────────────────────────
const detailRows = computed<RevenueRow[]>(() =>
    buildRevenueRows(
        estimates.value,
        staffingPlans.value,
        fcoByEstimateId.value,
        null,
        null,
    ),
);

// ── Data fetching ─────────────────────────────────────────────────────────────
async function fetchPaged<T>(path: string): Promise<T[]> {
    const pageSize = 200;
    let page = 1;
    let total = 0;
    const all: T[] = [];

    do {
        const { data } = await apiStore.api.get(`${path}?page=${page}&pageSize=${pageSize}`);
        const items = (data.items ?? []) as T[];
        total = Number(data.total ?? items.length);
        all.push(...items);
        page += 1;
    } while (all.length < total);

    return all;
}

async function fetchFcoTotals(items: EstimateListItem[]): Promise<Record<number, number>> {
    const out: Record<number, number> = {};
    await Promise.all(
        items.map(async item => {
            try {
                const { data } = await apiStore.api.get<EstimateDetailResponse>(`/api/v1/estimates/${item.estimateId}`);
                const total = (data.fcoEntries ?? []).reduce(
                    (sum, entry) => sum + Number(entry.dollarAdjustment ?? 0),
                    0,
                );
                out[item.estimateId] = round2(total);
            } catch {
                out[item.estimateId] = 0;
            }
        }),
    );
    return out;
}

async function loadData() {
    loading.value = true;
    errorMessage.value = '';
    try {
        const [estimateItems, staffingItems] = await Promise.all([
            fetchPaged<EstimateListItem>('/api/v1/estimates'),
            fetchPaged<StaffingListItem>('/api/v1/staffing-plans'),
        ]);

        estimates.value = estimateItems;
        staffingPlans.value = staffingItems;
        fcoByEstimateId.value = await fetchFcoTotals(estimateItems);
    } catch (err: unknown) {
        const anyErr = err as { response?: { status?: number } };
        const status = anyErr?.response?.status ? ` (${anyErr.response.status})` : '';
        errorMessage.value = `Failed to load revenue data${status}. Verify API is running and authenticated.`;
    } finally {
        loading.value = false;
    }
}

// ── Formatting helpers ────────────────────────────────────────────────────────
function fmtM(n: number): string {
    const abs = Math.abs(n);
    if (abs >= 1_000_000) return `$${(n / 1_000_000).toFixed(1)}M`;
    if (abs >= 1_000) return `$${Math.round(n / 1_000)}K`;
    return `$${Math.round(n)}`;
}

function fmtPct(n: number): string {
    return `${Math.round(Number(n ?? 0))}%`;
}

function fmtDate(value?: string): string {
    const d = safeDate(value);
    if (!d) return '-';
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function cumSum(arr: number[]): number[] {
    let acc = 0;
    return arr.map(v => { acc += v; return acc; });
}

function csvEscape(value: string | number): string {
    const asText = String(value ?? '');
    if (asText.includes(',') || asText.includes('"') || asText.includes('\n')) {
        return `"${asText.replace(/"/g, '""')}"`;
    }
    return asText;
}

function exportCsv() {
    const header = [
        'Number', 'Source', 'Name', 'Client', 'Status',
        'BucketDate', 'ConfidencePct', 'BaseAmount', 'WeightedAmount', 'FcoAmount',
    ];
    const lines = detailRows.value.map(row => [
        csvEscape(row.number),
        csvEscape(row.sourceType),
        csvEscape(row.name),
        csvEscape(row.client),
        csvEscape(row.status),
        csvEscape(row.bucketDate),
        csvEscape(row.confidencePct),
        csvEscape(row.baseAmount),
        csvEscape(row.weightedAmount),
        csvEscape(row.fcoAmount),
    ].join(','));

    const csv = [header.join(','), ...lines].join('\n');
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `financial-dashboard-${new Date().toISOString().slice(0, 10)}.csv`);
    link.click();
    URL.revokeObjectURL(url);
}

// ── Sync year dropdown -> YTD period ─────────────────────────────────────────
watch(selectedYear, () => {
    timePeriod.value = 'YTD';
});

onMounted(loadData);
</script>

<style scoped>
/* ── Page shell ─────────────────────────────────────────────────── */
.fd-page {
    display: flex;
    flex-direction: column;
    gap: 0;
    padding-bottom: 32px;
    min-height: 100%;
}

/* ── Top controls bar ───────────────────────────────────────────── */
.fd-topbar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 10px 16px;
    background: var(--surface-card);
    border-bottom: 1px solid var(--surface-border);
    flex-wrap: wrap;
    gap: 8px;
}

.fd-topbar-left {
    display: flex;
    align-items: center;
    gap: 12px;
}

.fd-topbar-right {
    display: flex;
    align-items: center;
    gap: 6px;
}

.fd-view-tabs {
    display: flex;
    border: 1px solid var(--surface-border);
    border-radius: 6px;
    overflow: hidden;
}

.fd-view-tab {
    padding: 6px 14px;
    background: transparent;
    border: none;
    color: var(--text-color-secondary);
    cursor: pointer;
    font-size: 0.82rem;
    font-weight: 600;
    letter-spacing: 0.04em;
    transition: background 0.15s, color 0.15s;
    border-right: 1px solid var(--surface-border);
}

.fd-view-tab:last-child {
    border-right: none;
}

.fd-view-tab.active,
.fd-view-tab:hover {
    background: var(--primary-color);
    color: #fff;
}

.fd-branch-dropdown {
    font-size: 0.82rem;
}

:deep(.fd-branch-dropdown .p-dropdown),
:deep(.fd-year-dropdown .p-dropdown),
:deep(.fd-inner-year-dropdown .p-dropdown),
:deep(.fd-inner-dropdown .p-dropdown) {
    font-size: 0.82rem;
}

.fd-year-dropdown {
    width: 100px;
}

.fd-inner-year-dropdown {
    width: 90px;
}

.fd-inner-dropdown {
    font-size: 0.82rem;
}

.fd-topbar-btn {
    font-size: 0.82rem;
}

/* ── Collapsible sections ───────────────────────────────────────── */
.fd-section {
    border: 1px solid var(--surface-border);
    border-radius: 0;
    margin-top: 8px;
    overflow: hidden;
}

.fd-section-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    background: #1e293b;
    color: #e2e8f0;
    padding: 10px 16px;
    cursor: pointer;
    user-select: none;
}

.fd-section-title {
    font-size: 0.75rem;
    font-weight: 700;
    letter-spacing: 0.1em;
    text-transform: uppercase;
}

.fd-section-header-right {
    display: flex;
    align-items: center;
    gap: 10px;
}

.fd-collapse-icon {
    font-size: 0.8rem;
    color: #94a3b8;
}

.fd-section-body {
    background: var(--surface-ground);
    padding: 16px;
}

/* ── Dashboard two-column layout ────────────────────────────────── */
.fd-dashboard-columns {
    display: flex;
    gap: 16px;
    align-items: flex-start;
}

.fd-left-col {
    flex: 7;
    display: flex;
    flex-direction: column;
    gap: 12px;
    min-width: 0;
}

.fd-right-col {
    flex: 3;
    min-width: 220px;
}

/* ── Time period tabs ───────────────────────────────────────────── */
.fd-period-tabs {
    display: flex;
    border: 1px solid var(--surface-border);
    border-radius: 6px;
    overflow: hidden;
    width: fit-content;
}

.fd-period-tab {
    padding: 5px 18px;
    background: transparent;
    border: none;
    border-right: 1px solid var(--surface-border);
    color: var(--text-color-secondary);
    cursor: pointer;
    font-size: 0.78rem;
    font-weight: 700;
    letter-spacing: 0.06em;
    transition: background 0.15s, color 0.15s;
}

.fd-period-tab:last-child {
    border-right: none;
}

.fd-period-tab.active {
    background: var(--primary-color);
    color: #fff;
}

.fd-period-tab:not(.active):hover {
    background: rgba(255, 255, 255, 0.05);
    color: var(--text-color);
}

/* ── KPI grids ──────────────────────────────────────────────────── */
.fd-kpi-grid {
    display: grid;
    gap: 8px;
}

.fd-kpi-grid-5 {
    grid-template-columns: repeat(5, 1fr);
}

.fd-kpi-grid-3 {
    grid-template-columns: repeat(3, 1fr);
}

.fd-kpi-card {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 8px;
    padding: 12px 14px;
    display: flex;
    flex-direction: column;
    gap: 2px;
}

.fd-kpi-label {
    font-size: 0.65rem;
    font-weight: 700;
    letter-spacing: 0.1em;
    text-transform: uppercase;
    color: var(--text-color-secondary);
}

.fd-kpi-value {
    font-size: 1.4rem;
    font-weight: 700;
    line-height: 1.2;
}

.fd-kpi-sub {
    font-size: 0.72rem;
    color: var(--text-color-secondary);
    margin-top: 2px;
}

/* ── Color utilities ────────────────────────────────────────────── */
.fd-green  { color: #22c55e; }
.fd-blue   { color: #60a5fa; }
.fd-red    { color: #f87171; }
.fd-white  { color: #e2e8f0; }
.fd-orange { color: #fb923c; }
.fd-yellow { color: #facc15; }
.fd-purple { color: #a78bfa; }

/* ── Manpower card (right col) ──────────────────────────────────── */
.fd-manpower-card {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 8px;
    padding: 16px;
    display: flex;
    flex-direction: column;
    gap: 8px;
    height: 100%;
}

.fd-manpower-heading {
    font-size: 0.9rem;
    font-weight: 700;
    color: var(--text-color);
}

.fd-manpower-sub {
    font-size: 0.78rem;
    color: var(--text-color-secondary);
    line-height: 1.5;
}

.fd-manpower-link {
    font-size: 0.82rem;
    color: var(--primary-color);
    text-decoration: none;
    font-weight: 600;
}

.fd-manpower-link:hover {
    text-decoration: underline;
}

.fd-slider-row {
    display: flex;
    flex-direction: column;
    gap: 6px;
    margin-top: 8px;
}

.fd-slider-label {
    font-size: 0.65rem;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: var(--text-color-secondary);
}

.fd-slider-wrap {
    display: flex;
    align-items: center;
    gap: 10px;
}

.fd-slider {
    flex: 1;
}

.fd-slider-val {
    font-size: 0.82rem;
    font-weight: 600;
    color: var(--text-color);
    min-width: 36px;
    text-align: right;
}

/* ── Chart section ──────────────────────────────────────────────── */
.fd-chart-controls {
    display: flex;
    align-items: center;
    gap: 12px;
    margin-bottom: 12px;
}

.fd-toggle-group {
    display: flex;
    border: 1px solid var(--surface-border);
    border-radius: 6px;
    overflow: hidden;
}

.fd-toggle-btn {
    padding: 5px 16px;
    background: transparent;
    border: none;
    border-right: 1px solid var(--surface-border);
    color: var(--text-color-secondary);
    cursor: pointer;
    font-size: 0.78rem;
    font-weight: 600;
    transition: background 0.15s, color 0.15s;
}

.fd-toggle-btn:last-child {
    border-right: none;
}

.fd-toggle-btn.active {
    background: var(--primary-color);
    color: #fff;
}

.fd-toggle-btn:not(.active):hover {
    background: rgba(255, 255, 255, 0.05);
    color: var(--text-color);
}

.fd-chart-container {
    height: 280px;
}

/* ── Win/Loss breakdown ─────────────────────────────────────────── */
.fd-winloss-row {
    display: flex;
    align-items: center;
    gap: 32px;
    flex-wrap: wrap;
    padding: 8px 0;
}

.fd-winloss-stat {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 4px;
}

.fd-winloss-label {
    font-size: 0.65rem;
    font-weight: 700;
    letter-spacing: 0.1em;
    text-transform: uppercase;
    color: var(--text-color-secondary);
}

.fd-winloss-count {
    font-size: 2rem;
    font-weight: 700;
    line-height: 1;
}

.fd-winloss-rate-block {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 4px;
    margin-left: auto;
    padding-left: 24px;
    border-left: 1px solid var(--surface-border);
}

.fd-winloss-rate-label {
    font-size: 0.65rem;
    font-weight: 700;
    letter-spacing: 0.1em;
    text-transform: uppercase;
    color: var(--text-color-secondary);
}

.fd-winloss-rate {
    font-size: 2.5rem;
    font-weight: 700;
    line-height: 1;
}

/* ── Responsive adjustments ─────────────────────────────────────── */
@media (max-width: 1100px) {
    .fd-kpi-grid-5 {
        grid-template-columns: repeat(3, 1fr);
    }

    .fd-dashboard-columns {
        flex-direction: column;
    }

    .fd-right-col {
        width: 100%;
    }
}

@media (max-width: 700px) {
    .fd-kpi-grid-5,
    .fd-kpi-grid-3 {
        grid-template-columns: repeat(2, 1fr);
    }
}

/* ── KPI drill-down ─────────────────────────────────────────────── */
.fd-kpi-clickable {
    cursor: pointer;
    transition: border-color 0.15s, box-shadow 0.15s;
}
.fd-kpi-clickable:hover {
    border-color: var(--primary-color);
    box-shadow: 0 0 0 1px var(--primary-color);
}
.fd-kpi-active {
    border-color: var(--primary-color) !important;
    background: color-mix(in srgb, var(--primary-color) 8%, var(--surface-card)) !important;
}
.fd-drill-icon {
    font-size: 0.6rem;
    opacity: 0.6;
    margin-left: 2px;
}
.fd-drill-panel {
    border: 1px solid var(--primary-color);
    border-radius: 8px;
    overflow: hidden;
    background: var(--surface-ground);
}
.fd-drill-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 8px 14px;
    background: color-mix(in srgb, var(--primary-color) 10%, var(--surface-section));
    border-bottom: 1px solid var(--surface-border);
}
.fd-drill-title {
    font-size: 0.72rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.08em;
    color: var(--text-color);
}
.fd-drill-close {
    background: transparent;
    border: none;
    color: var(--text-color-secondary);
    cursor: pointer;
    padding: 2px 6px;
    border-radius: 4px;
    font-size: 0.75rem;
    transition: background 0.12s;
}
.fd-drill-close:hover { background: var(--surface-hover); }
.fd-drill-scroll {
    overflow-x: auto;
    max-height: 320px;
    overflow-y: auto;
}
.fd-drill-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.82rem;
}
.fd-drill-table th {
    position: sticky;
    top: 0;
    background: var(--surface-section);
    padding: 6px 10px;
    text-align: left;
    font-size: 0.68rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: var(--text-color-secondary);
    border-bottom: 1px solid var(--surface-border);
    white-space: nowrap;
}
.fd-drill-table td {
    padding: 6px 10px;
    border-bottom: 1px solid var(--surface-border);
    white-space: nowrap;
}
.fd-drill-row {
    cursor: pointer;
    transition: background 0.1s;
}
.fd-drill-row:hover { background: var(--surface-hover); }
.drill-num  { font-family: monospace; font-size: 0.78rem; color: var(--primary-color); }
.drill-name { font-weight: 600; color: var(--text-color); max-width: 260px; overflow: hidden; text-overflow: ellipsis; }
.drill-client { color: var(--text-color-secondary); }
.drill-date { color: var(--text-color-secondary); font-size: 0.78rem; }
.drill-total { font-weight: 700; font-variant-numeric: tabular-nums; }
.drill-foot td {
    background: var(--surface-section);
    font-weight: 700;
    font-size: 0.8rem;
    border-top: 2px solid var(--surface-border);
}
.drill-status {
    display: inline-block;
    padding: 1px 7px;
    border-radius: 4px;
    font-size: 0.72rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.05em;
}
.status-awarded, .status-active  { background: rgba(34,197,94,0.15); color: #4ade80; }
.status-pending, .status-submitted { background: rgba(96,165,250,0.15); color: #60a5fa; }
.status-draft    { background: rgba(148,163,184,0.15); color: #94a3b8; }
.status-lost     { background: rgba(248,113,113,0.15); color: #f87171; }
.status-canceled { background: rgba(251,146,60,0.15); color: #fb923c; }
</style>
