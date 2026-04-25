<template>
    <div class="financial-dashboard">
        <BasePageHeader icon="pi pi-dollar" title="Financial Dashboard" subtitle="Pipeline, margin, and win/loss overview" />

        <div v-if="loading" class="flex justify-center p-8">
            <ProgressSpinner />
        </div>

        <template v-else-if="data">
            <!-- Row 1: KPI tiles -->
            <div class="kpi-grid">
                <div class="kpi-tile">
                    <div class="kpi-label">Pipeline Value</div>
                    <div class="kpi-value text-blue-400">{{ fmtCurrency(data.kpis.pipelineValue) }}</div>
                    <div class="kpi-sub">Awarded + Pending</div>
                </div>
                <div class="kpi-tile">
                    <div class="kpi-label">Won YTD</div>
                    <div class="kpi-value text-green-400">{{ fmtCurrency(data.kpis.wonYtd) }}</div>
                    <div class="kpi-sub">{{ new Date().getFullYear() }}</div>
                </div>
                <div class="kpi-tile">
                    <div class="kpi-label">Avg Margin</div>
                    <div class="kpi-value" :class="marginClass(data.kpis.avgMarginPct)">
                        {{ data.kpis.avgMarginPct.toFixed(1) }}%
                    </div>
                    <div class="kpi-sub">Across all estimates</div>
                </div>
                <div class="kpi-tile">
                    <div class="kpi-label">Win Rate</div>
                    <div class="kpi-value text-purple-400">{{ data.kpis.winRatePct.toFixed(1) }}%</div>
                    <div class="kpi-sub">Awarded / (Awarded + Lost)</div>
                </div>
                <div class="kpi-tile">
                    <div class="kpi-label">Active Jobs</div>
                    <div class="kpi-value text-green-400">{{ data.kpis.activeJobCount }}</div>
                    <div class="kpi-sub">
                        {{ data.kpis.pendingCount }} pending · {{ data.kpis.draftCount }} draft
                    </div>
                </div>
            </div>

            <!-- Row 2: Charts -->
            <div class="chart-row">
                <Card class="chart-card">
                    <template #title>Estimates by Status</template>
                    <template #content>
                        <Chart
                            type="bar"
                            :data="statusChartData"
                            :options="statusChartOptions"
                            style="height: 260px"
                        />
                    </template>
                </Card>
                <Card class="chart-card">
                    <template #title>Margin Distribution</template>
                    <template #content>
                        <div class="flex flex-col items-center">
                            <Chart
                                type="doughnut"
                                :data="marginChartData"
                                :options="marginChartOptions"
                                style="height: 220px; width: 220px"
                            />
                            <div class="margin-legend">
                                <span v-for="(b, i) in data.marginBuckets" :key="b.bucket" class="legend-item">
                                    <span class="legend-dot" :style="{ background: marginColors[i] }" />
                                    {{ b.bucket }} — {{ b.count }}
                                </span>
                            </div>
                        </div>
                    </template>
                </Card>
            </div>

            <!-- Row 3: Estimate table -->
            <Card>
                <template #title>All Estimates</template>
                <template #content>
                    <div class="flex gap-3 mb-3 flex-wrap">
                        <span class="p-input-icon-left">
                            <i class="pi pi-search" />
                            <InputText v-model="tableSearch" placeholder="Search name/client..." class="w-56" />
                        </span>
                        <Dropdown
                            v-model="tableStatus"
                            :options="statusOptions"
                            optionLabel="label"
                            optionValue="value"
                            placeholder="All Statuses"
                            showClear
                            class="w-40"
                        />
                    </div>
                    <DataTable
                        :value="filteredEstimates"
                        :paginator="true"
                        :rows="20"
                        sortField="updatedAt"
                        :sortOrder="-1"
                        rowHover
                        size="small"
                    >
                        <Column field="estimateNumber" header="#" sortable style="min-width:130px">
                            <template #body="{ data: row }">
                                <span class="font-mono text-sm font-semibold text-blue-400">{{ row.estimateNumber }}</span>
                            </template>
                        </Column>
                        <Column field="name" header="Name" sortable style="min-width:200px" />
                        <Column field="client" header="Client" sortable style="min-width:140px" />
                        <Column field="status" header="Status" style="min-width:110px">
                            <template #body="{ data: row }">
                                <Tag :value="row.status" :severity="statusSeverity(row.status)" />
                            </template>
                        </Column>
                        <Column field="grandTotal" header="Grand Total" sortable style="min-width:130px">
                            <template #body="{ data: row }">
                                <span class="font-semibold">{{ fmtCurrency(row.grandTotal) }}</span>
                            </template>
                        </Column>
                        <Column field="grossMarginPct" header="Margin" sortable style="min-width:100px">
                            <template #body="{ data: row }">
                                <Tag
                                    v-if="row.grandTotal > 0"
                                    :value="`${row.grossMarginPct.toFixed(1)}%`"
                                    :severity="marginSeverity(row.grossMarginPct)"
                                />
                                <span v-else class="text-surface-400 text-sm">—</span>
                            </template>
                        </Column>
                        <Column field="confidencePct" header="Confidence" sortable style="min-width:110px">
                            <template #body="{ data: row }">
                                <span>{{ row.confidencePct }}%</span>
                            </template>
                        </Column>
                        <Column field="startDate" header="Start" sortable style="min-width:110px">
                            <template #body="{ data: row }">{{ fmtDate(row.startDate) }}</template>
                        </Column>
                        <Column header="" style="min-width:80px">
                            <template #body="{ data: row }">
                                <Button
                                    icon="pi pi-arrow-right"
                                    text
                                    rounded
                                    size="small"
                                    @click="router.push(`/estimating/estimates/${row.estimateId}`)"
                                    v-tooltip="'Open estimate'"
                                />
                            </template>
                        </Column>
                    </DataTable>
                </template>
            </Card>
        </template>

        <div v-else-if="error" class="p-4 text-red-400">{{ error }}</div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useApiStore } from '@/stores/apiStore';
import BasePageHeader from '@/components/layout/BasePageHeader.vue';

const router = useRouter();
const apiStore = useApiStore();

// ── Types ─────────────────────────────────────────────────────────────────────

interface DashboardData {
    kpis: {
        pipelineValue: number;
        wonYtd: number;
        avgMarginPct: number;
        winRatePct: number;
        activeJobCount: number;
        draftCount: number;
        pendingCount: number;
        lostCount: number;
    };
    byStatus: Array<{ status: string; count: number; totalValue: number }>;
    marginBuckets: Array<{ bucket: string; count: number }>;
    estimates: Array<{
        estimateId: number;
        estimateNumber: string;
        name: string;
        client: string;
        status: string;
        grandTotal: number;
        grossMarginPct: number;
        confidencePct: number;
        startDate?: string;
        updatedAt: string;
    }>;
}

// ── State ─────────────────────────────────────────────────────────────────────

const data = ref<DashboardData | null>(null);
const loading = ref(false);
const error = ref('');
const tableSearch = ref('');
const tableStatus = ref('');

const statusOptions = [
    { label: 'Draft', value: 'Draft' },
    { label: 'Pending', value: 'Pending' },
    { label: 'Awarded', value: 'Awarded' },
    { label: 'Lost', value: 'Lost' },
    { label: 'Canceled', value: 'Canceled' },
];

// ── Load ──────────────────────────────────────────────────────────────────────

async function loadDashboard() {
    loading.value = true;
    error.value = '';
    try {
        const { data: d } = await apiStore.api.get('/api/v1/analytics/dashboard');
        data.value = d;
    } catch {
        error.value = 'Failed to load dashboard data.';
    } finally {
        loading.value = false;
    }
}

onMounted(loadDashboard);

// ── Filtered table ────────────────────────────────────────────────────────────

const filteredEstimates = computed(() => {
    if (!data.value) return [];
    return data.value.estimates.filter(e => {
        const matchSearch = !tableSearch.value ||
            e.name.toLowerCase().includes(tableSearch.value.toLowerCase()) ||
            e.client.toLowerCase().includes(tableSearch.value.toLowerCase()) ||
            e.estimateNumber.toLowerCase().includes(tableSearch.value.toLowerCase());
        const matchStatus = !tableStatus.value || e.status === tableStatus.value;
        return matchSearch && matchStatus;
    });
});

// ── Charts ────────────────────────────────────────────────────────────────────

const statusChartData = computed(() => {
    if (!data.value) return {};
    const order = ['Awarded', 'Pending', 'Draft', 'Lost', 'Canceled'];
    const sorted = [...data.value.byStatus].sort(
        (a, b) => order.indexOf(a.status) - order.indexOf(b.status)
    );
    return {
        labels: sorted.map(s => s.status),
        datasets: [
            {
                label: 'Count',
                data: sorted.map(s => s.count),
                backgroundColor: sorted.map(s => statusColor(s.status)),
                borderRadius: 4,
            },
        ],
    };
});

const statusChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { display: false } },
    scales: {
        y: { ticks: { stepSize: 1 }, grid: { color: 'rgba(255,255,255,0.05)' } },
        x: { grid: { display: false } },
    },
};

const marginColors = ['#ef4444', '#f59e0b', '#22c55e'];

const marginChartData = computed(() => {
    if (!data.value) return {};
    return {
        labels: data.value.marginBuckets.map(b => b.bucket),
        datasets: [{
            data: data.value.marginBuckets.map(b => b.count),
            backgroundColor: marginColors,
            borderWidth: 0,
        }],
    };
});

const marginChartOptions = {
    responsive: false,
    plugins: { legend: { display: false } },
    cutout: '65%',
};

// ── Helpers ───────────────────────────────────────────────────────────────────

function fmtCurrency(val: number): string {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(val ?? 0);
}

function fmtDate(val?: string): string {
    if (!val) return '';
    return new Date(val).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function statusSeverity(status: string): string {
    const map: Record<string, string> = { Draft: '', Pending: 'warning', Awarded: 'success', Lost: 'danger', Canceled: 'warning' };
    return map[status] ?? '';
}

function marginSeverity(pct: number): string {
    if (pct >= 25) return 'success';
    if (pct >= 15) return 'warning';
    return 'danger';
}

function marginClass(pct: number): string {
    if (pct >= 25) return 'text-green-400';
    if (pct >= 15) return 'text-yellow-400';
    return 'text-red-400';
}

function statusColor(status: string): string {
    const map: Record<string, string> = {
        Awarded: '#22c55e',
        Pending: '#f59e0b',
        Draft: '#6b7280',
        Lost: '#ef4444',
        Canceled: '#9ca3af',
    };
    return map[status] ?? '#6b7280';
}
</script>

<style scoped>
.financial-dashboard {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
}

.kpi-grid {
    display: grid;
    grid-template-columns: repeat(5, 1fr);
    gap: 1rem;
}

@media (max-width: 1200px) {
    .kpi-grid { grid-template-columns: repeat(3, 1fr); }
}
@media (max-width: 768px) {
    .kpi-grid { grid-template-columns: repeat(2, 1fr); }
}

.kpi-tile {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 8px;
    padding: 1.25rem 1.5rem;
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.kpi-label {
    font-size: 0.75rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--text-color-secondary);
}

.kpi-value {
    font-size: 1.75rem;
    font-weight: 700;
    line-height: 1;
}

.kpi-sub {
    font-size: 0.75rem;
    color: var(--text-color-secondary);
    margin-top: 0.25rem;
}

.chart-row {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1rem;
}

@media (max-width: 900px) {
    .chart-row { grid-template-columns: 1fr; }
}

.chart-card :deep(.p-card-body) {
    padding: 1rem;
}

.margin-legend {
    display: flex;
    gap: 1rem;
    margin-top: 0.75rem;
    flex-wrap: wrap;
    justify-content: center;
}

.legend-item {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    font-size: 0.8rem;
    color: var(--text-color-secondary);
}

.legend-dot {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    flex-shrink: 0;
}
</style>
