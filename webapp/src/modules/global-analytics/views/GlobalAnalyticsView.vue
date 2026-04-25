<template>
    <div class="ga-page">
        <!-- Header bar -->
        <div class="ga-topbar">
            <div class="ga-topbar-left">
                <span class="ga-title">Global Analytics</span>
                <span class="ga-subtitle">Cross-company pipeline intelligence</span>
            </div>
            <div class="ga-topbar-right">
                <Button
                    icon="pi pi-refresh"
                    text size="small"
                    :loading="loading"
                    @click="load"
                />
            </div>
        </div>

        <div class="ga-layout">
            <!-- ── Left: Filters Panel ────────────────────────────── -->
            <aside class="ga-filters">
                <div class="ga-filter-header">Filters</div>

                <!-- Companies -->
                <div class="ga-filter-section">
                    <div class="ga-filter-label">Companies</div>
                    <div v-for="co in allCompanies" :key="co" class="ga-check-row">
                        <Checkbox v-model="filters.companies" :value="co" :inputId="`co-${co}`" />
                        <label :for="`co-${co}`" class="ga-check-label">{{ co }}</label>
                    </div>
                </div>

                <!-- Statuses -->
                <div class="ga-filter-section">
                    <div class="ga-filter-label">Status</div>
                    <div v-for="s in allStatuses" :key="s" class="ga-check-row">
                        <Checkbox v-model="filters.statuses" :value="s" :inputId="`st-${s}`" />
                        <label :for="`st-${s}`" class="ga-check-label">{{ s }}</label>
                    </div>
                </div>

                <!-- VP -->
                <div class="ga-filter-section">
                    <div class="ga-filter-label">VP</div>
                    <Dropdown
                        v-model="filters.vp"
                        :options="[{label:'All VPs', value:''}, ...vpOptions]"
                        optionLabel="label"
                        optionValue="value"
                        class="ga-filter-dropdown"
                        placeholder="All VPs"
                    />
                </div>

                <!-- Region -->
                <div class="ga-filter-section">
                    <div class="ga-filter-label">Region</div>
                    <Dropdown
                        v-model="filters.region"
                        :options="[{label:'All Regions', value:''}, ...regionOptions]"
                        optionLabel="label"
                        optionValue="value"
                        class="ga-filter-dropdown"
                        placeholder="All Regions"
                    />
                </div>

                <!-- Min Confidence -->
                <div class="ga-filter-section">
                    <div class="ga-filter-label">Min Confidence: {{ filters.minConfidence }}%</div>
                    <Slider v-model="filters.minConfidence" :min="0" :max="100" class="ga-slider" />
                </div>

                <!-- Date range -->
                <div class="ga-filter-section">
                    <div class="ga-filter-label">Start Date</div>
                    <Calendar v-model="filters.startDate" showIcon class="ga-filter-dropdown" dateFormat="mm/dd/yy" />
                </div>
                <div class="ga-filter-section">
                    <div class="ga-filter-label">End Date</div>
                    <Calendar v-model="filters.endDate" showIcon class="ga-filter-dropdown" dateFormat="mm/dd/yy" />
                </div>

                <Button label="Clear Filters" icon="pi pi-times" text class="ga-clear-btn w-full mt-3" @click="clearFilters" />
            </aside>

            <!-- ── Right: Main content ────────────────────────────── -->
            <main class="ga-main">
                <div v-if="loading" class="flex justify-content-center py-8">
                    <ProgressSpinner />
                </div>
                <Message v-else-if="error" severity="error" :closable="false">{{ error }}</Message>

                <template v-else-if="data">
                    <!-- KPI Cards -->
                    <div class="ga-kpi-row">
                        <div class="ga-kpi-card">
                            <div class="ga-kpi-label">Total Pipeline</div>
                            <div class="ga-kpi-value">{{ fmt(data.kpis.totalForecast) }}</div>
                            <div class="ga-kpi-sub">{{ data.kpis.totalJobs }} jobs</div>
                        </div>
                        <div class="ga-kpi-card">
                            <div class="ga-kpi-label">Confidence-Weighted</div>
                            <div class="ga-kpi-value">{{ fmt(data.kpis.confidenceWeighted) }}</div>
                            <div class="ga-kpi-sub">Probability-adjusted</div>
                        </div>
                        <div class="ga-kpi-card awarded">
                            <div class="ga-kpi-label">Awarded</div>
                            <div class="ga-kpi-value">{{ fmt(data.kpis.awarded) }}</div>
                        </div>
                        <div class="ga-kpi-card pending">
                            <div class="ga-kpi-label">Pending + Submitted</div>
                            <div class="ga-kpi-value">{{ fmt(data.kpis.pending) }}</div>
                        </div>
                        <div class="ga-kpi-card active">
                            <div class="ga-kpi-label">Jobs Active Today</div>
                            <div class="ga-kpi-value">{{ data.kpis.jobsInRange }}</div>
                        </div>
                    </div>

                    <!-- Monthly Revenue Chart -->
                    <div class="ga-section">
                        <div class="ga-section-header">
                            <span class="ga-section-title">Monthly Revenue Forecast (Next 12 Months)</span>
                            <div class="ga-chart-toggle">
                                <button :class="['ga-toggle-btn', { active: chartMode === 'status' }]" @click="chartMode = 'status'">By Status</button>
                                <button :class="['ga-toggle-btn', { active: chartMode === 'company' }]" @click="chartMode = 'company'">By Company</button>
                            </div>
                        </div>
                        <div class="ga-chart-container">
                            <Chart type="bar" :data="monthlyChartData" :options="barChartOptions" />
                        </div>
                    </div>

                    <!-- Job Table -->
                    <div class="ga-section">
                        <div class="ga-section-header">
                            <span class="ga-section-title">All Jobs</span>
                            <span class="ga-section-count">{{ data.estimates.length }} estimates</span>
                        </div>
                        <DataTable
                            :value="data.estimates"
                            :rows="20"
                            :paginator="data.estimates.length > 20"
                            :rowsPerPageOptions="[20, 50, 100]"
                            sortField="grandTotal"
                            :sortOrder="-1"
                            class="ga-table"
                            size="small"
                            scrollable
                            scrollHeight="400px"
                        >
                            <Column field="companyCode" header="Co" sortable style="width:60px">
                                <template #body="{ data: row }">
                                    <span :class="`ga-co-pill ga-co-${row.companyCode.toLowerCase()}`">{{ row.companyCode }}</span>
                                </template>
                            </Column>
                            <Column field="estimateNumber" header="Job #" sortable style="width:130px" />
                            <Column field="client" header="Client" sortable />
                            <Column field="name" header="Name" sortable style="min-width:160px" />
                            <Column field="jobType" header="Type" sortable style="width:80px" />
                            <Column field="status" header="Status" sortable style="width:90px">
                                <template #body="{ data: row }">
                                    <span :class="`ga-status-badge ga-status-${row.status.toLowerCase()}`">{{ row.status }}</span>
                                </template>
                            </Column>
                            <Column field="vp" header="VP" sortable style="width:120px" />
                            <Column field="region" header="Region" sortable style="width:90px" />
                            <Column field="confidencePct" header="Conf%" sortable style="width:65px">
                                <template #body="{ data: row }">{{ row.confidencePct }}%</template>
                            </Column>
                            <Column field="grandTotal" header="Total" sortable style="width:110px">
                                <template #body="{ data: row }">{{ fmt(row.grandTotal) }}</template>
                            </Column>
                            <Column field="startDate" header="Start" sortable style="width:90px">
                                <template #body="{ data: row }">{{ fmtDate(row.startDate) }}</template>
                            </Column>
                            <Column field="endDate" header="End" sortable style="width:90px">
                                <template #body="{ data: row }">{{ fmtDate(row.endDate) }}</template>
                            </Column>
                        </DataTable>
                    </div>

                    <!-- Bottom Row: Top Clients + By Region -->
                    <div class="ga-bottom-row">
                        <!-- Top Clients -->
                        <div class="ga-section ga-bottom-card">
                            <div class="ga-section-header">
                                <span class="ga-section-title">Top 10 Clients</span>
                            </div>
                            <div class="ga-chart-container" style="height:280px">
                                <Chart type="bar" :data="topClientsChartData" :options="hbarOptions" />
                            </div>
                        </div>

                        <!-- By Region -->
                        <div class="ga-section ga-bottom-card">
                            <div class="ga-section-header">
                                <span class="ga-section-title">By Region</span>
                            </div>
                            <div class="ga-chart-container" style="height:280px">
                                <Chart type="doughnut" :data="byRegionChartData" :options="donutOptions" />
                            </div>
                        </div>
                    </div>
                </template>
            </main>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { useApiStore } from '@/stores/apiStore';

const apiStore = useApiStore();

// ── State ──────────────────────────────────────────────────────────────────────
const loading = ref(false);
const error = ref('');
const data = ref<GlobalOverviewResponse | null>(null);
const chartMode = ref<'status' | 'company'>('status');

const allCompanies = ['CSL', 'ETS', 'STS', 'STG'];
const allStatuses  = ['Awarded', 'Submitted', 'Pending', 'Draft', 'Lost'];

const filters = reactive({
    companies:     [] as string[],
    statuses:      [] as string[],
    vp:            '',
    region:        '',
    minConfidence: 0,
    startDate:     null as Date | null,
    endDate:       null as Date | null,
});

// ── Types ──────────────────────────────────────────────────────────────────────
interface GlobalOverviewResponse {
    kpis: {
        totalForecast: number;
        confidenceWeighted: number;
        awarded: number;
        pending: number;
        jobsInRange: number;
        totalJobs: number;
    };
    monthlyRevenue: Array<{
        month: string; label: string;
        awarded: number; pending: number;
        csl: number; ets: number; sts: number; stg: number;
    }>;
    topClients: Array<{ client: string; total: number; jobCount: number; awarded: number }>;
    byRegion: Array<{ region: string; total: number; jobCount: number }>;
    byCompany: Array<{ company: string; total: number; jobCount: number; awarded: number }>;
    estimates: Array<{
        estimateId: number; companyCode: string; estimateNumber: string;
        name: string; client: string; clientCode: string; jobType: string;
        status: string; confidencePct: number; grandTotal: number;
        grossMarginPct: number; startDate: string; endDate: string;
        vp: string; director: string; region: string;
    }>;
    filterOptions: { vps: string[]; directors: string[]; regions: string[]; jobTypes: string[] };
}

// ── Filter options from API ────────────────────────────────────────────────────
const vpOptions     = computed(() => (data.value?.filterOptions.vps     ?? []).map(v => ({ label: v, value: v })));
const regionOptions = computed(() => (data.value?.filterOptions.regions ?? []).map(r => ({ label: r, value: r })));

// ── Load data ──────────────────────────────────────────────────────────────────
async function load() {
    loading.value = true;
    error.value = '';
    try {
        const params = new URLSearchParams();
        if (filters.companies.length)    params.set('companies',     filters.companies.join(','));
        if (filters.statuses.length)     params.set('statuses',      filters.statuses.join(','));
        if (filters.vp)                  params.set('vp',            filters.vp);
        if (filters.region)              params.set('region',        filters.region);
        if (filters.minConfidence > 0)   params.set('minConfidence', String(filters.minConfidence));
        if (filters.startDate)           params.set('startDate',     filters.startDate.toISOString());
        if (filters.endDate)             params.set('endDate',       filters.endDate.toISOString());

        const qs = params.toString();
        const resp = await apiStore.api.get<GlobalOverviewResponse>(
            `/api/v1.0/global-analytics/overview${qs ? '?' + qs : ''}`
        );
        data.value = resp.data;
    } catch (e: any) {
        error.value = e?.response?.data?.message ?? 'Failed to load global analytics.';
    } finally {
        loading.value = false;
    }
}

function clearFilters() {
    filters.companies     = [];
    filters.statuses      = [];
    filters.vp            = '';
    filters.region        = '';
    filters.minConfidence = 0;
    filters.startDate     = null;
    filters.endDate       = null;
    load();
}

onMounted(load);

let debounceTimer: ReturnType<typeof setTimeout> | null = null;
watch(filters, () => {
    if (debounceTimer) clearTimeout(debounceTimer);
    debounceTimer = setTimeout(load, 350);
}, { deep: true });

// ── Formatters ─────────────────────────────────────────────────────────────────
function fmt(v: number) {
    if (!v) return '$0';
    if (v >= 1_000_000) return `$${(v / 1_000_000).toFixed(1)}M`;
    if (v >= 1_000)     return `$${(v / 1_000).toFixed(0)}K`;
    return `$${v.toFixed(0)}`;
}
function fmtDate(d?: string | null) {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
}

// ── Chart data ─────────────────────────────────────────────────────────────────
const COLORS = {
    awarded:  'rgba(34, 197, 94, 0.8)',
    pending:  'rgba(251, 146, 60, 0.8)',
    csl:      'rgba(30, 111, 181, 0.8)',
    ets:      'rgba(46, 125, 79, 0.8)',
    sts:      'rgba(123, 63, 160, 0.8)',
    stg:      'rgba(181, 90, 30, 0.8)',
};

const monthlyChartData = computed(() => {
    const months = data.value?.monthlyRevenue ?? [];
    const labels = months.map(m => m.label);
    if (chartMode.value === 'status') {
        return {
            labels,
            datasets: [
                { label: 'Awarded',          data: months.map(m => Math.round(m.awarded / 1000)),  backgroundColor: COLORS.awarded  },
                { label: 'Pending/Submitted', data: months.map(m => Math.round(m.pending / 1000)),  backgroundColor: COLORS.pending  },
            ],
        };
    }
    return {
        labels,
        datasets: [
            { label: 'CSL', data: months.map(m => Math.round(m.csl / 1000)), backgroundColor: COLORS.csl },
            { label: 'ETS', data: months.map(m => Math.round(m.ets / 1000)), backgroundColor: COLORS.ets },
            { label: 'STS', data: months.map(m => Math.round(m.sts / 1000)), backgroundColor: COLORS.sts },
            { label: 'STG', data: months.map(m => Math.round(m.stg / 1000)), backgroundColor: COLORS.stg },
        ],
    };
});

const barChartOptions = {
    responsive: true, maintainAspectRatio: false,
    plugins: { legend: { labels: { color: '#ccc' } } },
    scales: {
        x: { stacked: true, ticks: { color: '#aaa' }, grid: { color: 'rgba(255,255,255,0.05)' } },
        y: { stacked: true, ticks: { color: '#aaa', callback: (v: number) => `$${v}K` }, grid: { color: 'rgba(255,255,255,0.08)' } },
    },
};

const topClientsChartData = computed(() => {
    const clients = [...(data.value?.topClients ?? [])].slice(0, 10).reverse();
    return {
        labels: clients.map(c => c.client),
        datasets: [{
            label: 'Total ($K)',
            data: clients.map(c => Math.round(c.total / 1000)),
            backgroundColor: 'rgba(99, 179, 237, 0.75)',
        }],
    };
});

const hbarOptions = {
    indexAxis: 'y' as const,
    responsive: true, maintainAspectRatio: false,
    plugins: { legend: { display: false } },
    scales: {
        x: { ticks: { color: '#aaa', callback: (v: number) => `$${v}K` }, grid: { color: 'rgba(255,255,255,0.08)' } },
        y: { ticks: { color: '#ccc' }, grid: { display: false } },
    },
};

const byRegionChartData = computed(() => {
    const regions = data.value?.byRegion ?? [];
    const palette = ['rgba(30,111,181,0.8)', 'rgba(46,125,79,0.8)', 'rgba(123,63,160,0.8)', 'rgba(181,90,30,0.8)', 'rgba(99,179,237,0.8)', 'rgba(251,146,60,0.8)'];
    return {
        labels: regions.map(r => r.region),
        datasets: [{
            data: regions.map(r => Math.round(r.total / 1000)),
            backgroundColor: regions.map((_, i) => palette[i % palette.length]),
        }],
    };
});

const donutOptions = {
    responsive: true, maintainAspectRatio: false,
    plugins: {
        legend: { position: 'right' as const, labels: { color: '#ccc', boxWidth: 12 } },
        tooltip: { callbacks: { label: (ctx: any) => ` $${ctx.raw}K` } },
    },
};
</script>

<style scoped>
.ga-page { display: flex; flex-direction: column; height: 100%; overflow: hidden; }

.ga-topbar {
    display: flex; align-items: center; justify-content: space-between;
    padding: 12px 20px; border-bottom: 1px solid rgba(255,255,255,0.08);
    flex-shrink: 0;
}
.ga-title { font-size: 1.1rem; font-weight: 600; color: #e2e8f0; }
.ga-subtitle { font-size: 0.78rem; color: #94a3b8; margin-left: 10px; }

.ga-layout { display: flex; flex: 1; overflow: hidden; }

/* ── Filters ──────────────────────────────── */
.ga-filters {
    width: 220px; flex-shrink: 0;
    border-right: 1px solid rgba(255,255,255,0.08);
    padding: 16px; overflow-y: auto;
    background: rgba(15,23,42,0.6);
}
.ga-filter-header { font-size: 0.7rem; text-transform: uppercase; letter-spacing: 0.08em; color: #64748b; margin-bottom: 12px; }
.ga-filter-section { margin-bottom: 16px; }
.ga-filter-label { font-size: 0.75rem; color: #94a3b8; margin-bottom: 6px; }
.ga-check-row { display: flex; align-items: center; gap: 8px; margin-bottom: 4px; }
.ga-check-label { font-size: 0.8rem; color: #cbd5e1; cursor: pointer; }
.ga-filter-dropdown { width: 100%; }
.ga-slider { width: 100%; margin-top: 6px; }
.ga-apply-btn { font-size: 0.8rem; }
.ga-clear-btn { font-size: 0.8rem; }

/* ── Main content ─────────────────────────── */
.ga-main { flex: 1; overflow-y: auto; padding: 16px 20px; }

/* KPI cards */
.ga-kpi-row { display: flex; gap: 12px; margin-bottom: 20px; }
.ga-kpi-card {
    flex: 1; background: rgba(30,41,59,0.8); border: 1px solid rgba(255,255,255,0.08);
    border-radius: 8px; padding: 16px 14px;
}
.ga-kpi-card.awarded { border-color: rgba(34,197,94,0.3); }
.ga-kpi-card.pending  { border-color: rgba(251,146,60,0.3); }
.ga-kpi-card.active   { border-color: rgba(99,179,237,0.3); }
.ga-kpi-label { font-size: 0.72rem; text-transform: uppercase; letter-spacing: 0.06em; color: #64748b; margin-bottom: 6px; }
.ga-kpi-value { font-size: 1.5rem; font-weight: 700; color: #e2e8f0; }
.ga-kpi-sub   { font-size: 0.72rem; color: #64748b; margin-top: 4px; }

/* Sections */
.ga-section {
    background: rgba(30,41,59,0.6); border: 1px solid rgba(255,255,255,0.08);
    border-radius: 8px; margin-bottom: 16px; overflow: hidden;
}
.ga-section-header {
    display: flex; align-items: center; justify-content: space-between;
    padding: 10px 16px; border-bottom: 1px solid rgba(255,255,255,0.06);
}
.ga-section-title { font-size: 0.8rem; text-transform: uppercase; letter-spacing: 0.06em; color: #94a3b8; font-weight: 600; }
.ga-section-count { font-size: 0.75rem; color: #64748b; }

/* Chart toggle */
.ga-chart-toggle { display: flex; gap: 4px; }
.ga-toggle-btn {
    padding: 3px 10px; border-radius: 4px; font-size: 0.72rem; cursor: pointer;
    background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); color: #94a3b8;
}
.ga-toggle-btn.active { background: rgba(99,179,237,0.2); border-color: rgba(99,179,237,0.5); color: #93c5fd; }

.ga-chart-container { padding: 12px 16px; height: 220px; }

/* Bottom row */
.ga-bottom-row { display: flex; gap: 16px; }
.ga-bottom-card { flex: 1; }

/* Table */
.ga-table { font-size: 0.78rem; }

/* Company pill */
.ga-co-pill {
    display: inline-block; padding: 2px 7px; border-radius: 3px; font-size: 0.7rem; font-weight: 700;
}
.ga-co-csl { background: rgba(30,111,181,0.25); color: #93c5fd; }
.ga-co-ets { background: rgba(46,125,79,0.25);  color: #86efac; }
.ga-co-sts { background: rgba(123,63,160,0.25); color: #c4b5fd; }
.ga-co-stg { background: rgba(181,90,30,0.25);  color: #fdba74; }

/* Status badge */
.ga-status-badge {
    display: inline-block; padding: 2px 7px; border-radius: 3px; font-size: 0.68rem; font-weight: 600; text-transform: uppercase;
}
.ga-status-awarded   { background: rgba(34,197,94,0.2);  color: #86efac; }
.ga-status-submitted { background: rgba(59,130,246,0.2); color: #93c5fd; }
.ga-status-pending   { background: rgba(251,191,36,0.2); color: #fde68a; }
.ga-status-draft     { background: rgba(100,116,139,0.2);color: #94a3b8; }
.ga-status-lost      { background: rgba(239,68,68,0.2);  color: #fca5a5; }
</style>
