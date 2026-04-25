<template>
    <div class="manpower-forecast-view">
        <BasePageHeader
            icon="pi pi-users"
            title="Manpower Forecast"
            subtitle="Current = awarded work this month. Forecast = staffing plans + active estimates in selected range."
        >
            <div class="flex gap-2 align-items-center">
                <Button
                    label="Export CSV"
                    icon="pi pi-download"
                    severity="secondary"
                    size="small"
                    :disabled="!hasRun"
                    data-testid="mp-export-csv"
                    @click="exportCsv"
                />
                <Button
                    label="Export PNG"
                    icon="pi pi-image"
                    severity="secondary"
                    size="small"
                    :disabled="!hasRun"
                    data-testid="mp-export-png"
                    @click="exportPng"
                />
                <RouterLink to="/estimating/analytics/revenue">
                    <Button label="Reports" icon="pi pi-chart-bar" severity="secondary" size="small" />
                </RouterLink>
            </div>
        </BasePageHeader>

        <!-- Controls row -->
        <div class="filters-card">
            <div class="controls-row">
                <div class="field-col">
                    <label class="field-label">FROM DATE</label>
                    <Calendar v-model="fromDate" dateFormat="yy-mm-dd" showIcon class="ctrl-input" data-testid="mp-from" />
                </div>
                <div class="field-col">
                    <label class="field-label">TO DATE</label>
                    <Calendar v-model="toDate" dateFormat="yy-mm-dd" showIcon class="ctrl-input" data-testid="mp-to" />
                </div>
                <div class="field-col">
                    <label class="field-label">INCLUDE</label>
                    <Dropdown
                        v-model="includeMode"
                        :options="includeModeOptions"
                        optionLabel="label"
                        optionValue="value"
                        class="ctrl-input"
                        data-testid="mp-include"
                    />
                </div>
                <div class="field-col">
                    <label class="field-label">STATUS FILTER</label>
                    <Dropdown
                        v-model="statusFilter"
                        :options="statusFilterOptions"
                        optionLabel="label"
                        optionValue="value"
                        class="ctrl-input"
                        data-testid="mp-status-filter"
                    />
                </div>
                <div class="field-col">
                    <label class="field-label">POSITIONS</label>
                    <div class="positions-toggle">
                        <button
                            class="toggle-btn"
                            :class="{ active: positionMode === 'all' }"
                            data-testid="mp-pos-all"
                            @click="positionMode = 'all'"
                        >All Positions</button>
                        <button
                            class="toggle-btn"
                            :class="{ active: positionMode === 'direct' }"
                            data-testid="mp-pos-direct"
                            @click="positionMode = 'direct'"
                        >Direct Only</button>
                    </div>
                </div>
                <div class="field-col run-col">
                    <Button
                        label="Run Forecast"
                        icon="pi pi-play"
                        :loading="loading"
                        data-testid="mp-run"
                        @click="runForecast"
                    />
                </div>
            </div>
        </div>

        <!-- Error -->
        <Message v-if="errorMessage" severity="error" :closable="false">
            {{ errorMessage }}
        </Message>

        <!-- KPI Cards -->
        <div class="kpi-grid">
            <div class="kpi-card" data-testid="mp-kpi-current">
                <span class="kpi-label">CURRENT FIELDED (WON)</span>
                <span class="kpi-value">{{ hasRun ? fmtNumber(kpi.currentFielded) : '—' }}</span>
                <span class="kpi-sub">{{ hasRun ? `${kpi.estimateCount} estimate(s) | ${kpi.planCount} plan(s)` : 'Run forecast to view' }}</span>
            </div>
            <div class="kpi-card" data-testid="mp-kpi-peak">
                <span class="kpi-label">PEAK NEED IN RANGE</span>
                <span class="kpi-value blue">{{ hasRun ? fmtNumber(kpi.peakNeed) : '—' }}</span>
                <span class="kpi-sub">{{ hasRun ? `Peak month: ${kpi.peakMonthLabel}` : '' }}</span>
            </div>
            <div class="kpi-card" data-testid="mp-kpi-end">
                <span class="kpi-label">END OF RANGE NEED</span>
                <span class="kpi-value">{{ hasRun ? fmtNumber(kpi.endNeed) : '—' }}</span>
                <span class="kpi-sub">{{ hasRun ? `Gap vs current: ${fmtSigned(kpi.endGap)} ${kpi.lastMonthLabel}` : '' }}</span>
            </div>
            <div class="kpi-card" data-testid="mp-kpi-gap">
                <span class="kpi-label">PEAK GAP VS CURRENT</span>
                <span class="kpi-value" :class="kpi.peakGap > 0 ? 'orange' : kpi.peakGap < 0 ? 'red' : ''">
                    {{ hasRun ? fmtSigned(kpi.peakGap) : '—' }}
                </span>
                <span class="kpi-sub">{{ hasRun ? `${kpi.monthsAboveCurrent} month(s) above current` : '' }}</span>
            </div>
        </div>

        <!-- Position Breakdown -->
        <div class="section-card" data-testid="mp-position-breakdown">
            <div class="section-header">
                <span class="section-title">POSITION BREAKDOWN</span>
                <span class="section-badge">{{ pivotPositions.length }} positions</span>
                <button v-if="pivotPositions.length > 0" class="show-all-btn" @click="showAllPositions = !showAllPositions">
                    {{ showAllPositions ? `Hide Extra (${pivotPositions.length})` : `Show All (${pivotPositions.length})` }}
                    <i :class="showAllPositions ? 'pi pi-angle-up' : 'pi pi-angle-down'" />
                </button>
            </div>

            <div v-if="!hasRun" class="placeholder-text">
                Run the forecast to see results.
            </div>

            <div v-else-if="pivotPositions.length === 0" class="placeholder-text">
                No position data found for the selected range.
            </div>

            <div v-else class="pivot-wrap">
                <table class="pivot-table">
                    <thead>
                        <tr>
                            <th class="col-position sticky-col">POSITION</th>
                            <th class="col-current">CURRENT</th>
                            <th v-for="mk in pivotMonthKeys" :key="mk" class="col-month">{{ fmtMonthCol(mk) }}</th>
                            <th class="col-endgap">END GAP</th>
                            <th class="col-peak">PEAK</th>
                        </tr>
                    </thead>
                    <tbody>
                        <template v-for="pos in visiblePositions" :key="pos">
                            <tr>
                                <td class="col-position sticky-col pos-name">{{ pos }}</td>
                                <td class="col-current">{{ fmtNumber(posCurrent(pos)) }}</td>
                                <td
                                    v-for="mk in pivotMonthKeys"
                                    :key="mk"
                                    class="col-month"
                                    :class="deltaClass(posDelta(pos, mk))"
                                >{{ fmtDelta(pos, mk) }}</td>
                                <td class="col-endgap" :class="posEndGap(pos) < 0 ? 'cell-gap-neg' : posEndGap(pos) > 0 ? 'cell-demand' : ''">
                                    {{ fmtSigned(posEndGap(pos)) }}
                                </td>
                                <td class="col-peak">
                                    {{ fmtNumber(posPeak(pos).value) }}
                                    <span class="peak-month">({{ posPeak(pos).label }})</span>
                                </td>
                            </tr>
                        </template>
                        <!-- TOTAL row -->
                        <tr class="total-row">
                            <td class="col-position sticky-col">TOTAL</td>
                            <td class="col-current">{{ fmtNumber(kpi.currentFielded) }}</td>
                            <td v-for="mk in pivotMonthKeys" :key="mk" class="col-month" :class="deltaClass(monthDelta(mk))">
                                {{ fmtMonthDelta(mk) }}
                            </td>
                            <td class="col-endgap">{{ fmtSigned(kpi.endGap) }}</td>
                            <td class="col-peak">{{ fmtNumber(kpi.peakNeed) }}</td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>

        <!-- Headcount Over Time chart -->
        <div v-if="hasRun && monthRows.length > 0" class="section-card" data-testid="mp-chart">
            <div class="section-header">
                <span class="section-title">HEADCOUNT OVER TIME</span>
            </div>
            <Chart
                type="line"
                :data="chartData"
                :options="chartOptions"
                class="chart-height"
            />
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { RouterLink } from 'vue-router';
import { useToast } from 'primevue/usetoast';
import BasePageHeader from '@/components/layout/BasePageHeader.vue';
import { useApiStore } from '@/stores/apiStore';
import type { EstimateListItem, StaffingListItem } from '@/modules/estimating/features/analytics/utils/forecast';
import {
    computeManpowerDemand,
    normalizeStatus,
    round2,
} from '@/modules/estimating/features/analytics/utils/forecast';

// ── Types ──────────────────────────────────────────────────────────────────────

type LaborRow = {
    position?: string | null;
    craftCode?: string | null;
    scheduleJson?: string | null;
};

type EstimateDetailResponse = {
    estimateId: number;
    status: string;
    laborRows?: LaborRow[];
};

type StaffingDetailResponse = {
    staffingPlanId: number;
    status: string;
    convertedEstimateId?: number | null;
    laborRows?: LaborRow[];
};

type IncludeMode = 'plans+estimates' | 'plans' | 'estimates';
type StatusFilter = 'all-active' | 'awarded-only' | 'all-statuses';
type PositionMode = 'all' | 'direct';

// ── Store / composables ────────────────────────────────────────────────────────

const apiStore = useApiStore();
const toast = useToast();

// ── State ──────────────────────────────────────────────────────────────────────

const loading = ref(false);
const errorMessage = ref('');
const hasRun = ref(false);

const today = new Date();
const fromDate = ref<Date>(new Date(today.getFullYear(), today.getMonth(), today.getDate()));
const toDate = ref<Date>(new Date(today.getFullYear(), today.getMonth() + 6, today.getDate()));

const includeMode = ref<IncludeMode>('plans+estimates');
const statusFilter = ref<StatusFilter>('all-active');
const positionMode = ref<PositionMode>('all');
const showAllPositions = ref(false);

const includeModeOptions: { label: string; value: IncludeMode }[] = [
    { label: 'Plans + Estimates', value: 'plans+estimates' },
    { label: 'Plans Only', value: 'plans' },
    { label: 'Estimates Only', value: 'estimates' },
];

const statusFilterOptions: { label: string; value: StatusFilter }[] = [
    { label: 'All Active', value: 'all-active' },
    { label: 'Awarded Only', value: 'awarded-only' },
    { label: 'All Statuses', value: 'all-statuses' },
];

const estimateDetails = ref<EstimateDetailResponse[]>([]);
const staffingDetails = ref<StaffingDetailResponse[]>([]);

// ── Data fetching (unchanged logic) ───────────────────────────────────────────

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

async function loadEstimateDetails(list: EstimateListItem[]) {
    const details = await Promise.all(
        list.map(async item => {
            try {
                const { data } = await apiStore.api.get<EstimateDetailResponse>(`/api/v1/estimates/${item.estimateId}`);
                return data;
            } catch {
                return {
                    estimateId: item.estimateId,
                    status: item.status,
                    laborRows: [],
                } as EstimateDetailResponse;
            }
        }),
    );
    estimateDetails.value = details;
}

async function loadStaffingDetails(list: StaffingListItem[]) {
    const details = await Promise.all(
        list.map(async item => {
            try {
                const { data } = await apiStore.api.get<StaffingDetailResponse>(`/api/v1/staffing-plans/${item.staffingPlanId}`);
                return data;
            } catch {
                return {
                    staffingPlanId: item.staffingPlanId,
                    status: item.status,
                    convertedEstimateId: item.convertedEstimateId ?? null,
                    laborRows: [],
                } as StaffingDetailResponse;
            }
        }),
    );
    staffingDetails.value = details;
}

async function loadData() {
    loading.value = true;
    errorMessage.value = '';
    try {
        const [estimateList, staffingList] = await Promise.all([
            fetchPaged<EstimateListItem>('/api/v1/estimates'),
            fetchPaged<StaffingListItem>('/api/v1/staffing-plans'),
        ]);

        // Converted staffing plans are deduped from manpower forecast source list.
        const nonConvertedStaffing = staffingList.filter(sp => !sp.convertedEstimateId);

        await Promise.all([
            loadEstimateDetails(estimateList),
            loadStaffingDetails(nonConvertedStaffing),
        ]);
    } catch (err: unknown) {
        const status = (err as { response?: { status?: number } })?.response?.status
            ? ` (${(err as { response: { status: number } }).response.status})`
            : '';
        errorMessage.value = `Failed to load manpower data${status}. Verify API is running and authenticated.`;
    } finally {
        loading.value = false;
    }
}

async function runForecast() {
    await loadData();
    hasRun.value = true;
}

// ── Computed options from filter state ────────────────────────────────────────

const includePendingEstimates = computed(() =>
    includeMode.value === 'plans+estimates' || includeMode.value === 'estimates',
);

const includeStaffingPlans = computed(() =>
    includeMode.value === 'plans+estimates' || includeMode.value === 'plans',
);

// ── Core demand computation ────────────────────────────────────────────────────

const demand = computed(() =>
    computeManpowerDemand(
        [
            ...estimateDetails.value.map(e => ({
                sourceType: 'estimate' as const,
                sourceStatus: normalizeStatus(e.status),
                rows: e.laborRows ?? [],
            })),
            ...staffingDetails.value.map(sp => ({
                sourceType: 'staffing' as const,
                sourceStatus: normalizeStatus(sp.status),
                rows: sp.laborRows ?? [],
            })),
        ],
        {
            fromDate: fromDate.value,
            toDate: toDate.value,
            includePendingEstimates: includePendingEstimates.value,
            includeStaffingPlans: includeStaffingPlans.value,
        },
    ),
);

// ── Month rows (total headcount per month) ────────────────────────────────────

const monthRows = computed(() => demand.value.monthRows);

// ── KPI computations ──────────────────────────────────────────────────────────

const kpi = computed(() => {
    const currentFielded = demand.value.currentTotal;

    // Count how many estimates are awarded/active and how many plans
    const estimateCount = estimateDetails.value.filter(e => {
        const s = normalizeStatus(e.status);
        return s === 'Awarded' || s === 'Active';
    }).length;
    const planCount = staffingDetails.value.length;

    // Peak need
    const peakRow = [...monthRows.value].sort((a, b) => b.totalPeakHeadcount - a.totalPeakHeadcount)[0];
    const peakNeed = peakRow?.totalPeakHeadcount ?? 0;
    const peakMonthLabel = peakRow?.monthLabel ?? '';

    // End of range need (last month in sorted rows)
    const lastRow = monthRows.value[monthRows.value.length - 1];
    const endNeed = lastRow?.totalPeakHeadcount ?? 0;
    const lastMonthLabel = lastRow?.monthLabel ?? '';
    const endGap = round2(endNeed - currentFielded);

    // Peak gap vs current
    const peakGap = round2(peakNeed - currentFielded);
    const monthsAboveCurrent = monthRows.value.filter(r => r.totalPeakHeadcount > currentFielded).length;

    return {
        currentFielded,
        estimateCount,
        planCount,
        peakNeed,
        peakMonthLabel,
        endNeed,
        lastMonthLabel,
        endGap,
        peakGap,
        monthsAboveCurrent,
    };
});

// ── Pivot table ───────────────────────────────────────────────────────────────

/** Sorted list of all month keys in the from→to range */
const pivotMonthKeys = computed((): string[] => {
    const keys: string[] = [];
    const start = new Date(fromDate.value.getFullYear(), fromDate.value.getMonth(), 1);
    const end = new Date(toDate.value.getFullYear(), toDate.value.getMonth(), 1);
    const cur = new Date(start);
    while (cur <= end) {
        const y = cur.getFullYear();
        const m = String(cur.getMonth() + 1).padStart(2, '0');
        keys.push(`${y}-${m}`);
        cur.setMonth(cur.getMonth() + 1);
    }
    return keys;
});

/** Unique sorted position/craft names */
const pivotPositions = computed((): string[] => {
    const set = new Set(demand.value.craftMonthlyRows.map(r => r.craft));
    return [...set].sort((a, b) => a.localeCompare(b));
});

/** Positions visible in the table (collapsed to first 10 unless showAll) */
const visiblePositions = computed((): string[] => {
    const positions = pivotPositions.value;
    if (showAllPositions.value || positions.length <= 10) return positions;
    return positions.slice(0, 10);
});

/** Build lookup: craft -> monthKey -> peakHeadcount */
const craftMonthMap = computed((): Map<string, Map<string, number>> => {
    const map = new Map<string, Map<string, number>>();
    for (const row of demand.value.craftMonthlyRows) {
        if (!map.has(row.craft)) map.set(row.craft, new Map());
        map.get(row.craft)!.set(row.monthKey, row.peakHeadcount);
    }
    return map;
});

function pivotCell(craft: string, monthKey: string): number {
    return craftMonthMap.value.get(craft)?.get(monthKey) ?? 0;
}

function pivotMonthTotal(monthKey: string): number {
    let total = 0;
    for (const craft of pivotPositions.value) {
        total += pivotCell(craft, monthKey);
    }
    return round2(total);
}

function posPeak(craft: string): { value: number; label: string } {
    const monthMap = craftMonthMap.value.get(craft);
    if (!monthMap || monthMap.size === 0) return { value: 0, label: '' };
    let maxVal = 0;
    let maxKey = '';
    for (const [mk, val] of monthMap.entries()) {
        if (val > maxVal) { maxVal = val; maxKey = mk; }
    }
    return { value: maxVal, label: fmtMonthCol(maxKey) };
}

function posEndGap(craft: string): number {
    const lastKey = pivotMonthKeys.value[pivotMonthKeys.value.length - 1];
    const endVal = lastKey ? pivotCell(craft, lastKey) : 0;
    return round2(endVal - posCurrent(craft));
}

function posCurrent(craft: string): number {
    return demand.value.currentByCraft[craft] ?? 0;
}

// Delta: this month's absolute headcount vs prior month (or vs current for first month)
function posDelta(craft: string, monthKey: string): number {
    const thisAbs = pivotCell(craft, monthKey);
    const idx = pivotMonthKeys.value.indexOf(monthKey);
    const prevAbs = idx === 0
        ? posCurrent(craft)
        : pivotCell(craft, pivotMonthKeys.value[idx - 1]);
    return round2(thisAbs - prevAbs);
}

function monthDelta(monthKey: string): number {
    const thisAbs = pivotMonthTotal(monthKey);
    const idx = pivotMonthKeys.value.indexOf(monthKey);
    const prevAbs = idx === 0
        ? kpi.value.currentFielded
        : pivotMonthTotal(pivotMonthKeys.value[idx - 1]);
    return round2(thisAbs - prevAbs);
}

function deltaClass(delta: number): string {
    if (delta > 0) return 'cell-demand';
    if (delta < 0) return 'cell-gap-neg';
    return '';
}

// Show +N / −N for non-zero delta, '--' when both this month and prev are zero
function fmtDelta(craft: string, monthKey: string): string {
    const delta = posDelta(craft, monthKey);
    const thisAbs = pivotCell(craft, monthKey);
    const idx = pivotMonthKeys.value.indexOf(monthKey);
    const prevAbs = idx === 0 ? posCurrent(craft) : pivotCell(craft, pivotMonthKeys.value[idx - 1]);
    if (delta === 0 && thisAbs === 0 && prevAbs === 0) return '--';
    return delta >= 0 ? `+${fmtNumber(delta)}` : fmtNumber(delta);
}

function fmtMonthDelta(monthKey: string): string {
    const delta = monthDelta(monthKey);
    const idx = pivotMonthKeys.value.indexOf(monthKey);
    const thisAbs = pivotMonthTotal(monthKey);
    const prevAbs = idx === 0 ? kpi.value.currentFielded : pivotMonthTotal(pivotMonthKeys.value[idx - 1]);
    if (delta === 0 && thisAbs === 0 && prevAbs === 0) return '--';
    return delta >= 0 ? `+${fmtNumber(delta)}` : fmtNumber(delta);
}

function fmtMonthCol(monthKey: string): string {
    const [y, m] = monthKey.split('-');
    const d = new Date(Number(y), Number(m) - 1, 1);
    const mon = d.toLocaleDateString('en-US', { month: 'short' });
    const yr = String(d.getFullYear()).slice(2);
    return `${mon} ${yr}`;
}

// ── Chart ─────────────────────────────────────────────────────────────────────

const chartData = computed(() => ({
    labels: monthRows.value.map(row => row.monthLabel),
    datasets: [
        {
            label: 'Headcount',
            borderColor: '#38bdf8',
            backgroundColor: 'rgba(56,189,248,0.12)',
            pointBackgroundColor: '#38bdf8',
            tension: 0.25,
            fill: true,
            data: monthRows.value.map(row => row.totalPeakHeadcount),
        },
    ],
}));

const chartOptions = {
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
            grid: { color: 'rgba(148,163,184,0.2)' },
        },
        y: {
            ticks: { color: '#94a3b8' },
            grid: { color: 'rgba(148,163,184,0.2)' },
        },
    },
};

// ── Formatters ─────────────────────────────────────────────────────────────────

function fmtNumber(value: number): string {
    return new Intl.NumberFormat('en-US', { maximumFractionDigits: 1 }).format(Number(value ?? 0));
}

function fmtSigned(value: number): string {
    const rounded = round2(value);
    return `${rounded >= 0 ? '+' : ''}${fmtNumber(rounded)}`;
}

// ── Export actions ─────────────────────────────────────────────────────────────

function exportCsv() {
    if (!hasRun.value || pivotPositions.value.length === 0) return;

    const headers = ['POSITION', 'CURRENT', ...pivotMonthKeys.value.map(fmtMonthCol), 'END GAP', 'PEAK'];
    const rows: string[][] = pivotPositions.value.map(pos => {
        const peak = posPeak(pos);
        return [
            pos,
            '0',
            ...pivotMonthKeys.value.map(mk => String(pivotCell(pos, mk) || '')),
            fmtSigned(posEndGap(pos)),
            `${fmtNumber(peak.value)} (${peak.label})`,
        ];
    });
    // TOTAL row
    const totalPeak = posPeak('__total__');
    rows.push([
        'TOTAL',
        fmtNumber(kpi.value.currentFielded),
        ...pivotMonthKeys.value.map(mk => fmtNumber(pivotMonthTotal(mk))),
        fmtSigned(kpi.value.endGap),
        fmtNumber(kpi.value.peakNeed),
    ]);
    void totalPeak; // suppress unused warning

    const csvContent = [headers, ...rows]
        .map(row => row.map(cell => `"${String(cell).replace(/"/g, '""')}"`).join(','))
        .join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'manpower-forecast.csv';
    link.click();
    URL.revokeObjectURL(url);
}

function exportPng() {
    toast.add({ severity: 'info', summary: 'Export PNG', detail: 'PNG export coming soon', life: 3000 });
}
</script>

<style scoped>
.manpower-forecast-view {
    display: flex;
    flex-direction: column;
    gap: 10px;
    padding-bottom: 24px;
}

/* ── Filter controls ── */
.filters-card {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 8px;
    padding: 12px 16px;
}

.controls-row {
    display: flex;
    align-items: flex-end;
    gap: 12px;
    flex-wrap: wrap;
}

.field-col {
    display: flex;
    flex-direction: column;
    gap: 4px;
}

.field-label {
    font-size: 0.68rem;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    font-weight: 700;
    color: var(--text-color-secondary);
    white-space: nowrap;
}

.ctrl-input {
    min-width: 160px;
}

.run-col {
    margin-left: auto;
}

/* ── Positions toggle ── */
.positions-toggle {
    display: flex;
    border: 1px solid var(--surface-border);
    border-radius: 6px;
    overflow: hidden;
}

.toggle-btn {
    background: transparent;
    border: none;
    color: var(--text-color-secondary);
    padding: 6px 12px;
    font-size: 0.8rem;
    cursor: pointer;
    transition: background 0.15s, color 0.15s;
    white-space: nowrap;
}

.toggle-btn:hover {
    background: var(--surface-hover);
}

.toggle-btn.active {
    background: var(--primary-color);
    color: #fff;
}

/* ── KPI cards ── */
.kpi-grid {
    display: grid;
    gap: 8px;
    grid-template-columns: repeat(4, 1fr);
}

@media (max-width: 900px) {
    .kpi-grid {
        grid-template-columns: repeat(2, 1fr);
    }
}

.kpi-card {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 8px;
    padding: 12px 14px;
    display: flex;
    flex-direction: column;
    gap: 2px;
}

.kpi-label {
    font-size: 0.68rem;
    text-transform: uppercase;
    letter-spacing: 0.08em;
    color: var(--text-color-secondary);
    font-weight: 700;
}

.kpi-value {
    font-size: 1.6rem;
    font-weight: 700;
    color: var(--text-color);
    line-height: 1.1;
}

.kpi-value.blue  { color: #38bdf8; }
.kpi-value.orange { color: #f59e0b; }
.kpi-value.red   { color: #f87171; }

.kpi-sub {
    font-size: 0.75rem;
    color: var(--text-color-secondary);
    margin-top: 2px;
}

/* ── Section cards ── */
.section-card {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 8px;
    padding: 14px 16px;
    display: flex;
    flex-direction: column;
    gap: 12px;
}

.section-header {
    display: flex;
    align-items: center;
    gap: 10px;
}

.section-title {
    font-size: 0.72rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.09em;
    color: var(--text-color-secondary);
}

.section-badge {
    background: var(--surface-section);
    border: 1px solid var(--surface-border);
    border-radius: 12px;
    padding: 2px 8px;
    font-size: 0.72rem;
    color: var(--text-color-secondary);
}

.show-all-btn {
    background: transparent;
    border: 1px solid var(--surface-border);
    border-radius: 6px;
    color: var(--text-color-secondary);
    padding: 3px 10px;
    font-size: 0.75rem;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    gap: 4px;
    margin-left: auto;
    transition: background 0.15s;
}

.show-all-btn:hover {
    background: var(--surface-hover);
}

.placeholder-text {
    color: var(--text-color-secondary);
    font-size: 0.9rem;
    padding: 24px 0;
    text-align: center;
}

/* ── Pivot table ── */
.pivot-wrap {
    overflow-x: auto;
}

.pivot-table {
    border-collapse: collapse;
    width: 100%;
    font-size: 0.82rem;
}

.pivot-table th,
.pivot-table td {
    border: 1px solid var(--surface-border);
    padding: 5px 8px;
    text-align: right;
    white-space: nowrap;
}

.pivot-table thead th {
    background: var(--surface-section);
    font-size: 0.68rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: var(--text-color-secondary);
    position: sticky;
    top: 0;
    z-index: 1;
}

.sticky-col {
    position: sticky;
    left: 0;
    z-index: 2;
    text-align: left !important;
    background: var(--surface-card);
}

.pivot-table thead th.sticky-col {
    background: var(--surface-section);
    z-index: 3;
}

.col-position { min-width: 140px; }
.col-current  { min-width: 70px; }
.col-month    { min-width: 64px; }
.col-endgap   { min-width: 70px; }
.col-peak     { min-width: 100px; }

.pos-name {
    color: var(--text-color);
}

.peak-month {
    font-size: 0.72rem;
    color: var(--text-color-secondary);
    margin-left: 3px;
}

/* Demand and gap cell colors */
.cell-demand {
    background: rgba(34, 197, 94, 0.12);
    color: #4ade80;
}

.cell-gap-neg {
    background: rgba(248, 113, 113, 0.15);
    color: #f87171;
}

/* TOTAL row */
.total-row td {
    background: var(--surface-section);
    font-weight: 700;
}

.total-row td.sticky-col {
    background: var(--surface-section);
}

/* ── Chart ── */
.chart-height {
    height: 260px;
}
</style>
