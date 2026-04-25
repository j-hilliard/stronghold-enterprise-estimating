<template>
    <Card class="labor-grid-card" data-testid="labor-grid">
        <template #title>
            <div class="flex align-items-center justify-content-between gap-2">
                <div class="flex align-items-center gap-2">
                    <i class="pi pi-users text-primary" />
                    <span>Labor</span>
                    <Tag :value="`${rows.length} row${rows.length !== 1 ? 's' : ''}`" severity="secondary" />
                </div>
                <div class="flex gap-2">
                    <Button
                        label="Load Crew"
                        icon="pi pi-users"
                        size="small"
                        outlined
                        severity="secondary"
                        data-testid="labor-load-crew"
                        @click="$emit('loadCrew')"
                    />
                    <Button
                        label="+ Add Employee"
                        icon="pi pi-plus"
                        size="small"
                        outlined
                        data-testid="labor-add-row"
                        @click="openAddDialog"
                    />
                </div>
            </div>
        </template>
        <template #content>
            <!-- Rate book bar -->
            <div class="rate-book-bar">
                <span class="rate-book-label">ACTIVE RATES:</span>
                <span v-if="rateBookName" class="rate-book-name">{{ rateBookName }}</span>
                <span v-else class="rate-book-none">None loaded</span>
                <Button
                    label="Load Rate Book"
                    icon="pi pi-book"
                    size="small"
                    text
                    class="rate-book-btn"
                    @click="$emit('openRateBook')"
                />
                <Button
                    v-if="rateBookName"
                    icon="pi pi-times"
                    size="small"
                    text
                    severity="secondary"
                    title="Clear rate book"
                    @click="$emit('clearRateBook')"
                />
            </div>

            <!-- Week tab selector (only shown when dates span multiple weeks) -->
            <div v-if="weekTabs.length > 1" class="flex gap-2 mb-3 flex-wrap">
                <Button
                    v-for="tab in weekTabs"
                    :key="tab.key"
                    :label="tab.label"
                    :outlined="activeWeek !== tab.key"
                    size="small"
                    @click="activeWeek = tab.key"
                />
            </div>

            <div class="labor-grid-scroll">
                <table class="labor-table w-full" data-testid="labor-table">
                    <thead>
                        <tr>
                            <th class="col-position">Position</th>
                            <th class="col-type">Type</th>
                            <th class="col-shift">Shift</th>
                            <th class="col-rate col-st">ST Rate</th>
                            <th class="col-rate col-ot">OT Rate</th>
                            <th class="col-rate col-dt">DT Rate</th>
                            <th
                                v-for="d in visibleDates"
                                :key="d.iso"
                                class="col-day"
                                :class="{ 'day-weekend': d.isSunday || d.isSaturday, 'day-sunday': d.isSunday }"
                                :title="d.iso"
                            >
                                <div class="text-xs">{{ d.label }}</div>
                                <div class="text-xs opacity-60">{{ d.dow }}</div>
                            </th>
                            <th class="col-hours col-st">ST</th>
                            <th class="col-hours col-ot">OT</th>
                            <th class="col-hours col-dt">DT</th>
                            <th class="col-money col-st">ST $</th>
                            <th class="col-money col-ot">OT $</th>
                            <th class="col-money col-dt">DT $</th>
                            <th class="col-total">TOTAL</th>
                            <th class="col-del"></th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="(row, idx) in rows" :key="idx">
                            <td class="col-position">
                                <InputText
                                    v-model="row.position"
                                    placeholder="Pipefitter"
                                    class="w-full cell-input"
                                    :data-testid="`labor-position-${idx}`"
                                    @input="onRowChange(idx)"
                                />
                            </td>
                            <td class="col-type">
                                <Tag
                                    :value="row.laborType || 'Direct'"
                                    :severity="row.laborType === 'Indirect' ? 'secondary' : 'info'"
                                    class="type-tag"
                                />
                            </td>
                            <td class="col-shift text-center">
                                <span class="shift-badge" :class="`shift-${row.shift?.toLowerCase()}`">
                                    {{ row.shift === 'Both' ? 'D/N' : row.shift?.[0] ?? 'D' }}
                                </span>
                            </td>
                            <td class="col-rate text-right col-st">
                                <span class="rate-display">{{ fmtRate(row.billStRate) }}</span>
                            </td>
                            <td class="col-rate text-right col-ot">
                                <span class="rate-display">{{ fmtRate(row.billOtRate) }}</span>
                            </td>
                            <td class="col-rate text-right col-dt">
                                <span class="rate-display">{{ fmtRate(row.billDtRate) }}</span>
                            </td>
                            <td
                                v-for="d in visibleDates"
                                :key="d.iso"
                                class="col-day"
                                :class="{ 'day-weekend': d.isSunday || d.isSaturday, 'day-sunday': d.isSunday }"
                            >
                                <InputNumber
                                    :modelValue="getHeadcount(row, d.iso)"
                                    @update:modelValue="v => setHeadcount(row, idx, d.iso, v ?? 0)"
                                    :min="0"
                                    :max="99"
                                    class="w-full cell-headcount"
                                    inputClass="text-center"
                                />
                            </td>
                            <td class="col-hours text-right col-st">{{ fmt(row.stHours) }}</td>
                            <td class="col-hours text-right col-ot">{{ fmt(row.otHours) }}</td>
                            <td class="col-hours text-right col-dt">{{ fmt(row.dtHours) }}</td>
                            <td class="col-money text-right col-st">{{ fmtCurrency(row.stHours * row.billStRate) }}</td>
                            <td class="col-money text-right col-ot">{{ fmtCurrency(row.otHours * row.billOtRate) }}</td>
                            <td class="col-money text-right col-dt">{{ fmtCurrency(row.dtHours * row.billDtRate) }}</td>
                            <td class="col-total text-right font-semibold">{{ fmtCurrency(row.subtotal) }}</td>
                            <td class="col-del">
                                <Button
                                    icon="pi pi-trash"
                                    text
                                    rounded
                                    size="small"
                                    severity="danger"
                                    :data-testid="`labor-remove-${idx}`"
                                    @click="removeRow(idx)"
                                />
                            </td>
                        </tr>
                    </tbody>
                    <tfoot v-if="rows.length > 0">
                        <tr class="totals-row">
                            <td :colspan="6 + visibleDates.length" class="text-right font-semibold pr-3 text-sm">TOTAL</td>
                            <td class="col-hours text-right font-bold col-st">{{ fmt(totalSt) }}</td>
                            <td class="col-hours text-right font-bold col-ot">{{ fmt(totalOt) }}</td>
                            <td class="col-hours text-right font-bold col-dt">{{ fmt(totalDt) }}</td>
                            <td class="col-money text-right font-bold col-st">{{ fmtCurrency(totalStAmt) }}</td>
                            <td class="col-money text-right font-bold col-ot">{{ fmtCurrency(totalOtAmt) }}</td>
                            <td class="col-money text-right font-bold col-dt">{{ fmtCurrency(totalDtAmt) }}</td>
                            <td class="col-total text-right font-bold total-grand">{{ fmtCurrency(laborTotal) }}</td>
                            <td></td>
                        </tr>
                    </tfoot>
                </table>
            </div>

            <div v-if="rows.length === 0" class="text-center py-6 text-slate-400">
                No labor rows. Click "+ Add Employee" to begin.
            </div>
        </template>
    </Card>

    <!-- ── Add Employee Dialog ──────────────────────────────────────────── -->
    <Dialog v-model:visible="addDialogVisible" header="Add Employee" modal style="width: 500px;">
        <!-- No rate book warning -->
        <div v-if="!rateBookRates?.length" class="no-rates-msg">
            <i class="pi pi-exclamation-triangle" style="color: var(--orange-400);" />
            <span>No rate book loaded. Load a rate book first to auto-populate rates, or enter a position name manually below.</span>
        </div>

        <!-- Shift toggle — only when shift = 'Both' -->
        <div v-if="shift === 'Both'" class="shift-toggle mb-3">
            <Button label="Day Shift" size="small" :outlined="addDialogShift !== 'Day'" @click="addDialogShift = 'Day'" />
            <Button label="Night Shift" size="small" :outlined="addDialogShift !== 'Night'" @click="addDialogShift = 'Night'" />
        </div>

        <!-- Search bar (only when rate book loaded) -->
        <span v-if="rateBookRates?.length" class="p-input-icon-left w-full mb-3 block">
            <i class="pi pi-search" />
            <InputText v-model="addSearch" placeholder="Search positions..." class="w-full" />
        </span>

        <!-- Position list from rate book -->
        <div v-if="rateBookRates?.length" class="position-list">
            <div
                v-for="rate in filteredPositions"
                :key="rate.rateBookLaborRateId ?? rate.position"
                class="position-row"
                @click="selectPosition(rate)"
            >
                <span class="position-name">{{ rate.position }}</span>
                <div class="position-meta">
                    <Tag
                        :value="rate.laborType"
                        :severity="rate.laborType === 'Indirect' ? 'secondary' : 'info'"
                        class="type-tag-sm"
                    />
                    <span class="position-rate">ST: {{ fmtRate(rate.stRate) }}/hr</span>
                </div>
            </div>
            <div v-if="!filteredPositions.length" class="text-center py-4 text-slate-400 text-sm">
                No positions match "{{ addSearch }}"
            </div>
        </div>

        <!-- Manual entry fallback -->
        <div v-else class="manual-entry">
            <label class="manual-label">POSITION NAME</label>
            <InputText
                v-model="manualPosition"
                placeholder="e.g. Pipefitter Foreman"
                class="w-full"
                @keydown.enter="addManual"
            />
        </div>

        <template #footer>
            <Button label="Cancel" text @click="addDialogVisible = false" />
            <Button
                v-if="!rateBookRates?.length"
                label="Add"
                icon="pi pi-plus"
                :disabled="!manualPosition.trim()"
                @click="addManual"
            />
        </template>
    </Dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Card from 'primevue/card';
import { calcOtHours } from '../../../composables/useOtCalculator';
import type { LaborRow } from '../../../stores/estimateStore';

export interface RateBookLaborRate {
    rateBookLaborRateId?: number;
    rateBookId?: number;
    position: string;
    laborType: string;
    craftCode?: string;
    navCode?: string;
    stRate: number;
    otRate: number;
    dtRate: number;
    sortOrder?: number;
}

const props = defineProps<{
    rows: LaborRow[];
    startDate?: string;
    endDate?: string;
    hoursPerShift: number;
    otMethod: string;
    dtWeekends: boolean;
    shift: string;
    rateBookRates?: RateBookLaborRate[];
    rateBookName?: string;
}>();

const emits = defineEmits<{
    (e: 'update:rows', v: LaborRow[]): void;
    (e: 'change'): void;
    (e: 'loadCrew'): void;
    (e: 'openRateBook'): void;
    (e: 'clearRateBook'): void;
}>();

// ── Date columns ─────────────────────────────────────────────────────────────

interface DateCol {
    iso: string;      // YYYY-MM-DD
    label: string;    // M/D
    dow: string;      // Mon/Tue…
    weekKey: string;  // JW-N (job-relative week)
    isSaturday: boolean;
    isSunday: boolean;
}

const DOW_LABELS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

const allDates = computed<DateCol[]>(() => {
    if (!props.startDate || !props.endDate) return [];
    const result: DateCol[] = [];
    const start = new Date(props.startDate + 'T12:00:00');
    const end = new Date(props.endDate + 'T12:00:00');
    const cur = new Date(start);
    while (cur <= end) {
        const iso = cur.toISOString().slice(0, 10);
        const dow = cur.getDay();
        const jobWeek = getJobWeek(cur, start);
        result.push({
            iso,
            label: `${cur.getMonth() + 1}/${cur.getDate()}`,
            dow: DOW_LABELS[dow],
            weekKey: `JW-${jobWeek}`,
            isSaturday: dow === 6,
            isSunday: dow === 0,
        });
        cur.setDate(cur.getDate() + 1);
    }
    return result;
});

// Job-relative week: Week 1 = job start through first Sunday (Mon–Sun boundaries)
function getJobWeek(d: Date, startDate: Date): number {
    const msPerDay = 86400000;
    const daysSinceStart = Math.round((d.getTime() - startDate.getTime()) / msPerDay);
    const startDow = startDate.getDay(); // 0=Sun, 1=Mon, ..., 6=Sat
    const daysToSunday = startDow === 0 ? 0 : 7 - startDow;
    if (daysSinceStart <= daysToSunday) return 1;
    return 2 + Math.floor((daysSinceStart - daysToSunday - 1) / 7);
}

const weekTabs = computed(() => {
    const seen = new Set<string>();
    const tabs: { key: string; label: string }[] = [];
    for (const d of allDates.value) {
        if (!seen.has(d.weekKey)) {
            seen.add(d.weekKey);
            const num = d.weekKey.split('-')[1]; // "JW-3" → "3"
            tabs.push({ key: d.weekKey, label: `Wk ${num}` });
        }
    }
    tabs.unshift({ key: 'all', label: 'All' });
    return tabs;
});

const activeWeek = ref('all');
watch(() => props.startDate, () => { activeWeek.value = 'all'; });

const visibleDates = computed<DateCol[]>(() => {
    if (activeWeek.value === 'all') return allDates.value;
    return allDates.value.filter(d => d.weekKey === activeWeek.value);
});

// ── Schedule helpers ──────────────────────────────────────────────────────────

function getSchedule(row: LaborRow): Record<string, number> {
    if (!row.scheduleJson) return {};
    try { return JSON.parse(row.scheduleJson); } catch { return {}; }
}

function getHeadcount(row: LaborRow, iso: string): number {
    return getSchedule(row)[iso] ?? 0;
}

function setHeadcount(row: LaborRow, idx: number, iso: string, val: number) {
    const sched = getSchedule(row);
    if (val === 0) delete sched[iso];
    else sched[iso] = val;
    row.scheduleJson = JSON.stringify(sched);
    recalcRow(row);
    emits('change');
    emitRows();
}

// ── Recalc ─────────────────────────────────────────────────────────────────

function recalcRow(row: LaborRow) {
    const { stHours, otHours, dtHours } = calcOtHours(
        row.scheduleJson,
        props.hoursPerShift,
        props.otMethod,
        props.dtWeekends,
    );
    row.stHours = stHours;
    row.otHours = otHours;
    row.dtHours = dtHours;
    row.subtotal = Math.round((stHours * row.billStRate + otHours * row.billOtRate + dtHours * row.billDtRate) * 100) / 100;
}

// Recalc all rows when OT settings change
watch([() => props.hoursPerShift, () => props.otMethod, () => props.dtWeekends], () => {
    for (const row of props.rows) recalcRow(row);
    emits('change');
    emitRows();
});

// When date range changes, extend existing rows to cover any new days (default headcount 1 for new days)
watch([() => props.startDate, () => props.endDate], () => {
    if (!props.startDate || !props.endDate) return;
    let changed = false;
    for (const row of props.rows) {
        const sched = getSchedule(row);
        const cur = new Date(props.startDate + 'T12:00:00');
        const end = new Date(props.endDate + 'T12:00:00');
        while (cur <= end) {
            const iso = cur.toISOString().slice(0, 10);
            if (!(iso in sched)) { sched[iso] = 1; changed = true; }
            cur.setDate(cur.getDate() + 1);
        }
        if (changed) { row.scheduleJson = JSON.stringify(sched); recalcRow(row); }
    }
    if (changed) { emits('change'); emitRows(); }
});

function onRateChange(idx: number) {
    recalcRow(props.rows[idx]);
    emits('change');
    emitRows();
}

function onRowChange(idx: number) {
    emits('change');
    emitRows();
}

function emitRows() {
    emits('update:rows', [...props.rows]);
}

// ── Row management ──────────────────────────────────────────────────────────

function buildDefaultSchedule(): string {
    if (!props.startDate || !props.endDate) return '{}';
    const sched: Record<string, number> = {};
    const cur = new Date(props.startDate + 'T12:00:00');
    const end = new Date(props.endDate + 'T12:00:00');
    while (cur <= end) {
        sched[cur.toISOString().slice(0, 10)] = 1;
        cur.setDate(cur.getDate() + 1);
    }
    return JSON.stringify(sched);
}

function removeRow(idx: number) {
    const updated = [...props.rows];
    updated.splice(idx, 1);
    emits('update:rows', updated);
    emits('change');
}

// ── Add Employee dialog ──────────────────────────────────────────────────────

const addDialogVisible = ref(false);
const addDialogShift = ref('Day');
const addSearch = ref('');
const manualPosition = ref('');

function openAddDialog() {
    addDialogShift.value = props.shift === 'Both' ? 'Day' : props.shift;
    addSearch.value = '';
    manualPosition.value = '';
    addDialogVisible.value = true;
}

const filteredPositions = computed<RateBookLaborRate[]>(() => {
    const rates = props.rateBookRates ?? [];
    if (!addSearch.value.trim()) return rates;
    const q = addSearch.value.toLowerCase();
    return rates.filter(r => r.position.toLowerCase().includes(q));
});

function selectPosition(rate: RateBookLaborRate) {
    const rowShift = props.shift === 'Both' ? addDialogShift.value : props.shift;
    const newRow: LaborRow = {
        position: rate.position,
        laborType: rate.laborType,
        shift: rowShift,
        craftCode: rate.craftCode,
        navCode: rate.navCode,
        billStRate: rate.stRate,
        billOtRate: rate.otRate,
        billDtRate: rate.dtRate,
        scheduleJson: buildDefaultSchedule(),
        stHours: 0,
        otHours: 0,
        dtHours: 0,
        subtotal: 0,
    };
    recalcRow(newRow);
    emits('update:rows', [...props.rows, newRow]);
    emits('change');
    addDialogVisible.value = false;
}

function addManual() {
    if (!manualPosition.value.trim()) return;
    const rowShift = props.shift === 'Both' ? addDialogShift.value : props.shift;
    const newRow: LaborRow = {
        position: manualPosition.value.trim(),
        laborType: 'Direct',
        shift: rowShift,
        billStRate: 0,
        billOtRate: 0,
        billDtRate: 0,
        scheduleJson: buildDefaultSchedule(),
        stHours: 0,
        otHours: 0,
        dtHours: 0,
        subtotal: 0,
    };
    recalcRow(newRow);
    emits('update:rows', [...props.rows, newRow]);
    emits('change');
    addDialogVisible.value = false;
}

// ── Totals ──────────────────────────────────────────────────────────────────

const totalSt = computed(() => props.rows.reduce((s, r) => s + r.stHours, 0));
const totalOt = computed(() => props.rows.reduce((s, r) => s + r.otHours, 0));
const totalDt = computed(() => props.rows.reduce((s, r) => s + r.dtHours, 0));
const totalStAmt = computed(() => props.rows.reduce((s, r) => s + r.stHours * r.billStRate, 0));
const totalOtAmt = computed(() => props.rows.reduce((s, r) => s + r.otHours * r.billOtRate, 0));
const totalDtAmt = computed(() => props.rows.reduce((s, r) => s + r.dtHours * r.billDtRate, 0));
const laborTotal = computed(() => props.rows.reduce((s, r) => s + r.subtotal, 0));

// ── Options ─────────────────────────────────────────────────────────────────

const shiftOptions = ['Day', 'Night', 'Both'];

// ── Formatting ──────────────────────────────────────────────────────────────

function fmt(n: number): string { return n.toFixed(1); }
function fmtRate(n: number): string {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(n ?? 0);
}
function fmtCurrency(n: number): string {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(n ?? 0);
}
</script>

<style scoped>
.labor-grid-scroll {
    overflow-x: auto;
}

.labor-table {
    border-collapse: collapse;
    font-size: 0.8rem;
    min-width: 800px;
}

.labor-table th, .labor-table td {
    padding: 4px 6px;
    border-bottom: 1px solid var(--surface-border);
    white-space: nowrap;
    vertical-align: middle;
}

.labor-table thead th {
    background: var(--surface-100);
    font-weight: 600;
    font-size: 0.75rem;
    text-align: center;
    position: sticky;
    top: 0;
    z-index: 1;
}

.labor-table tfoot td {
    background: var(--surface-50);
    border-top: 2px solid var(--surface-border);
    padding-top: 6px;
}

.col-position { min-width: 140px; }
.col-type     { min-width: 72px; text-align: center; }
.col-shift    { min-width: 46px; text-align: center; }
.col-rate     { min-width: 72px; }

.type-tag { font-size: 0.65rem; padding: 2px 6px; }
.rate-display { font-size: 0.78rem; font-variant-numeric: tabular-nums; }
.col-day      { min-width: 52px; text-align: center; }
.col-hours    { min-width: 56px; text-align: right; }
.col-money    { min-width: 88px; text-align: right; }
.col-total    { min-width: 100px; text-align: right; font-weight: 700; }
.col-del      { width: 36px; text-align: center; }

/* ST = green, OT = orange, DT = red */
.col-st { color: #4ade80; }
.col-ot { color: #fb923c; }
.col-dt { color: #f87171; }

.labor-table thead th.col-st { color: #4ade80; }
.labor-table thead th.col-ot { color: #fb923c; }
.labor-table thead th.col-dt { color: #f87171; }

.totals-row td { background: var(--surface-50); border-top: 2px solid var(--surface-border); }
.total-grand { color: var(--primary-color); font-size: 0.88rem; }

.day-weekend { background-color: rgba(255, 193, 7, 0.06); }
.day-sunday  { background-color: rgba(244, 67, 54, 0.06); }

.labor-grid-card :deep(.cell-input .p-inputtext),
.labor-grid-card :deep(.cell-select .p-dropdown) {
    font-size: 0.8rem;
    height: 28px;
    padding: 2px 6px;
}

.labor-grid-card :deep(.cell-num .p-inputnumber-input),
.labor-grid-card :deep(.cell-headcount .p-inputnumber-input) {
    font-size: 0.8rem;
    height: 28px;
    padding: 2px 4px;
    text-align: right;
    width: 100%;
}

.labor-grid-card :deep(.cell-headcount) {
    width: 44px;
}

.shift-badge {
    display: inline-block;
    padding: 1px 6px;
    border-radius: 3px;
    font-size: 0.7rem;
    font-weight: 700;
    letter-spacing: 0.04em;
}
.shift-day   { background: rgba(74, 222, 128, 0.15); color: #4ade80; }
.shift-night { background: rgba(147, 197, 253, 0.15); color: #93c5fd; }
.shift-both  { background: rgba(251, 146, 60, 0.15); color: #fb923c; }

/* ── Rate book bar ────────────────────────────────────────────────────────── */
.rate-book-bar {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 5px 2px 8px;
    border-bottom: 1px solid var(--surface-border);
    margin-bottom: 10px;
    font-size: 0.78rem;
}

.rate-book-label {
    font-size: 0.65rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.07em;
    color: var(--text-color-secondary);
    white-space: nowrap;
}

.rate-book-name {
    font-weight: 600;
    color: var(--primary-color);
    font-family: monospace;
    font-size: 0.8rem;
}

.rate-book-none {
    color: var(--text-color-secondary);
    font-style: italic;
    font-size: 0.78rem;
}

/* ── Add Employee dialog ──────────────────────────────────────────────────── */
.no-rates-msg {
    display: flex;
    align-items: flex-start;
    gap: 8px;
    background: color-mix(in srgb, var(--orange-400) 10%, transparent);
    border: 1px solid color-mix(in srgb, var(--orange-400) 40%, transparent);
    border-radius: 5px;
    padding: 10px 12px;
    font-size: 0.82rem;
    color: var(--text-color);
    margin-bottom: 14px;
}

.shift-toggle {
    display: flex;
    gap: 8px;
}

.position-list {
    max-height: 380px;
    overflow-y: auto;
    border: 1px solid var(--surface-border);
    border-radius: 5px;
}

.position-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 9px 12px;
    cursor: pointer;
    border-bottom: 1px solid var(--surface-border);
    transition: background 0.12s;
}

.position-row:last-child { border-bottom: none; }

.position-row:hover { background: var(--surface-hover); }

.position-name {
    font-size: 0.85rem;
    font-weight: 500;
    color: var(--text-color);
}

.position-meta {
    display: flex;
    align-items: center;
    gap: 10px;
}

.type-tag-sm { font-size: 0.62rem; padding: 1px 5px; }

.position-rate {
    font-size: 0.78rem;
    font-family: monospace;
    color: #4ade80;
    font-weight: 600;
    white-space: nowrap;
}

.manual-entry {
    display: flex;
    flex-direction: column;
    gap: 4px;
}

.manual-label {
    font-size: 0.62rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.07em;
    color: var(--text-color-secondary);
}
</style>
