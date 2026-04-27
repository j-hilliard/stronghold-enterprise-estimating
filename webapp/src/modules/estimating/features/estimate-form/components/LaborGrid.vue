<template>
    <Card class="labor-grid-card" data-testid="labor-grid">
        <template #title>
            <div class="flex align-items-center gap-2">
                <i class="pi pi-users text-primary" />
                <span>Labor</span>
                <Tag :value="`${rows.length} row${rows.length !== 1 ? 's' : ''}`" severity="secondary" />
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
                    label="Re-apply Rates"
                    icon="pi pi-refresh"
                    size="small"
                    text
                    severity="secondary"
                    title="Re-apply rate book rates to all rows"
                    @click="$emit('applyRates')"
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
                            <th class="col-position sl-0">Position</th>
                            <th class="col-type sl-140">Type</th>
                            <th class="col-shift sl-212">Shift</th>
                            <th :class="['col-rate col-st', !isAllView && 'sl-258']">ST Rate</th>
                            <th :class="['col-rate col-ot', !isAllView && 'sl-330']">OT Rate</th>
                            <th :class="['col-rate col-dt', !isAllView && 'sl-402']">DT Rate</th>
                            <th
                                v-for="d in visibleDates"
                                :key="d.iso"
                                class="col-day"
                                :class="{ 'day-weekend': d.isSunday || d.isSaturday, 'day-sunday': d.isSunday, 'day-out-of-range': d.isOutOfRange }"
                                :title="d.iso"
                            >
                                <div class="text-xs">{{ d.label }}</div>
                                <div class="text-xs opacity-60">{{ d.dow }}</div>
                            </th>
                            <th :class="['col-hours col-st', !isAllView && 'sr-456']">ST</th>
                            <th :class="['col-hours col-ot', !isAllView && 'sr-400']">OT</th>
                            <th :class="['col-hours col-dt', !isAllView && 'sr-344']">DT</th>
                            <th :class="['col-money col-st', !isAllView && 'sr-256']">ST $</th>
                            <th :class="['col-money col-ot', !isAllView && 'sr-168']">OT $</th>
                            <th :class="['col-money col-dt', !isAllView && 'sr-80']">DT $</th>
                            <th class="col-total sr-36">TOTAL</th>
                            <th class="col-del"></th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="(row, idx) in rows" :key="idx">
                            <td class="col-position sl-0">
                                <InputText
                                    v-model="row.position"
                                    placeholder="Pipefitter"
                                    class="w-full cell-input"
                                    :data-testid="`labor-position-${idx}`"
                                    @input="onRowChange(idx)"
                                />
                            </td>
                            <td class="col-type sl-140">
                                <Tag
                                    :value="row.laborType || 'Direct'"
                                    :severity="row.laborType === 'Indirect' ? 'secondary' : 'info'"
                                    class="type-tag"
                                />
                            </td>
                            <td class="col-shift text-center sl-212">
                                <span class="shift-badge" :class="`shift-${row.shift?.toLowerCase()}`">
                                    {{ row.shift === 'Both' ? 'D/N' : row.shift?.[0] ?? 'D' }}
                                </span>
                            </td>
                            <td :class="['col-rate text-right col-st', !isAllView && 'sl-258']">
                                <span class="rate-display">{{ fmtRate(row.billStRate) }}</span>
                            </td>
                            <td :class="['col-rate text-right col-ot', !isAllView && 'sl-330']">
                                <span class="rate-display">{{ fmtRate(row.billOtRate) }}</span>
                            </td>
                            <td :class="['col-rate text-right col-dt', !isAllView && 'sl-402']">
                                <span class="rate-display">{{ fmtRate(row.billDtRate) }}</span>
                            </td>
                            <td
                                v-for="d in visibleDates"
                                :key="d.iso"
                                class="col-day"
                                :class="{ 'day-weekend': d.isSunday || d.isSaturday, 'day-sunday': d.isSunday, 'day-out-of-range': d.isOutOfRange }"
                            >
                                <input
                                    type="number"
                                    :value="getHeadcount(row, d.iso)"
                                    @change="(e) => setHeadcount(row, idx, d.iso, Math.max(0, Math.min(99, Number((e.target as HTMLInputElement).value) || 0)))"
                                    min="0"
                                    max="99"
                                    :disabled="d.isOutOfRange"
                                    class="cell-headcount-native"
                                />
                            </td>
                            <td :class="['col-hours text-right col-st', !isAllView && 'sr-456']">{{ fmt(row.stHours) }}</td>
                            <td :class="['col-hours text-right col-ot', !isAllView && 'sr-400']">{{ fmt(row.otHours) }}</td>
                            <td :class="['col-hours text-right col-dt', !isAllView && 'sr-344']">{{ fmt(row.dtHours) }}</td>
                            <td :class="['col-money text-right col-st', !isAllView && 'sr-256']">{{ fmtCurrency(row.stHours * row.billStRate) }}</td>
                            <td :class="['col-money text-right col-ot', !isAllView && 'sr-168']">{{ fmtCurrency(row.otHours * row.billOtRate) }}</td>
                            <td :class="['col-money text-right col-dt', !isAllView && 'sr-80']">{{ fmtCurrency(row.dtHours * row.billDtRate) }}</td>
                            <td class="col-total text-right font-semibold sr-36">{{ fmtCurrency(row.subtotal) }}</td>
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
                            <td class="col-position sl-0 totals-label text-right font-semibold text-sm">TOTAL</td>
                            <td class="col-type totals-label sl-140"></td>
                            <td class="col-shift totals-label sl-212"></td>
                            <td :class="['col-rate totals-label', !isAllView && 'sl-258']"></td>
                            <td :class="['col-rate totals-label', !isAllView && 'sl-330']"></td>
                            <td :class="['col-rate totals-label', !isAllView && 'sl-402']"></td>
                            <td v-for="d in visibleDates" :key="d.iso" :class="['totals-spacer', { 'day-out-of-range': d.isOutOfRange }]"></td>
                            <td :class="['col-hours text-right font-bold col-st', !isAllView && 'sr-456']">{{ fmt(totalSt) }}</td>
                            <td :class="['col-hours text-right font-bold col-ot', !isAllView && 'sr-400']">{{ fmt(totalOt) }}</td>
                            <td :class="['col-hours text-right font-bold col-dt', !isAllView && 'sr-344']">{{ fmt(totalDt) }}</td>
                            <td :class="['col-money text-right font-bold col-st', !isAllView && 'sr-256']">{{ fmtCurrency(totalStAmt) }}</td>
                            <td :class="['col-money text-right font-bold col-ot', !isAllView && 'sr-168']">{{ fmtCurrency(totalOtAmt) }}</td>
                            <td :class="['col-money text-right font-bold col-dt', !isAllView && 'sr-80']">{{ fmtCurrency(totalDtAmt) }}</td>
                            <td class="col-total text-right font-bold total-grand sr-36">{{ fmtCurrency(laborTotal) }}</td>
                            <td class="col-del totals-label"></td>
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
    dtWeekends: string;
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
    (e: 'applyRates'): void;
}>();

// ── Date columns ─────────────────────────────────────────────────────────────

interface DateCol {
    iso: string;      // YYYY-MM-DD
    label: string;    // M/D
    dow: string;      // Mon/Tue…
    weekKey: string;  // JW-N (job-relative week)
    isSaturday: boolean;
    isSunday: boolean;
    isOutOfRange?: boolean; // day exists in the Mon-Sun week but outside job dates
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
const isAllView = computed(() => activeWeek.value === 'all');

const visibleDates = computed<DateCol[]>(() => {
    if (activeWeek.value === 'all') return allDates.value;

    const weekDates = allDates.value.filter(d => d.weekKey === activeWeek.value);
    if (weekDates.length === 0) return [];

    // Find the Monday of this calendar week
    const firstDate = new Date(weekDates[0].iso + 'T12:00:00');
    const dow = firstDate.getDay();
    const monday = new Date(firstDate);
    monday.setDate(monday.getDate() + (dow === 0 ? -6 : 1 - dow));

    const result: DateCol[] = [];
    for (let i = 0; i < 7; i++) {
        const d = new Date(monday);
        d.setDate(monday.getDate() + i);
        const iso = d.toISOString().slice(0, 10);
        const dayDow = d.getDay();
        const isOutOfRange = !!(
            (props.startDate && d < new Date(props.startDate + 'T12:00:00')) ||
            (props.endDate   && d > new Date(props.endDate   + 'T12:00:00'))
        );
        result.push({
            iso,
            label: `${d.getMonth() + 1}/${d.getDate()}`,
            dow: DOW_LABELS[dayDow],
            weekKey: weekDates[0].weekKey,
            isSaturday: dayDow === 6,
            isSunday:   dayDow === 0,
            isOutOfRange,
        });
    }
    return result;
});

// ── Schedule helpers ──────────────────────────────────────────────────────────

function getSchedule(row: LaborRow): Record<string, number> {
    if (!row.scheduleJson) return {};
    try { return JSON.parse(row.scheduleJson); } catch { return {}; }
}

function getHeadcount(row: LaborRow, iso: string): number {
    return getSchedule(row)[iso] ?? 0;
}

function setHeadcount(row: LaborRow, _idx: number, iso: string, val: number) {
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

function onRowChange(_idx: number) {
    emits('change');
    emitRows();
}

function emitRows() {
    emits('update:rows', [...props.rows]);
}

// ── Row management ──────────────────────────────────────────────────────────

function getAllDateIsos(): string[] {
    if (!props.startDate || !props.endDate) return [];
    const result: string[] = [];
    const cur = new Date(props.startDate + 'T12:00:00');
    const end = new Date(props.endDate + 'T12:00:00');
    while (cur <= end) {
        result.push(cur.toISOString().slice(0, 10));
        cur.setDate(cur.getDate() + 1);
    }
    return result;
}

function buildDefaultSchedule(): string {
    const sched: Record<string, number> = {};
    for (const iso of getAllDateIsos()) sched[iso] = 1;
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
    const existing = props.rows.find(r => r.position === rate.position && r.shift === rowShift);
    if (existing) {
        // Increment headcount by 1 on every scheduled day
        const sched = getSchedule(existing);
        const allIsos = getAllDateIsos();
        for (const iso of allIsos) sched[iso] = (sched[iso] ?? 0) + 1;
        existing.scheduleJson = JSON.stringify(sched);
        recalcRow(existing);
        emits('update:rows', [...props.rows]);
    } else {
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
    }
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
    --labor-header-h: 36px;
    --labor-row-h: 47px;
    --labor-footer-h: 38px;
    overflow-x: auto;
    position: relative;
}

.labor-table {
    border-collapse: separate;
    border-spacing: 0;
    font-size: 0.8rem;
    min-width: 800px;
}

.labor-table th, .labor-table td {
    padding: 0 6px;
    border-bottom: 1px solid var(--surface-border);
    white-space: nowrap;
    vertical-align: middle;
    box-sizing: border-box;
}

.labor-table thead th {
    height: var(--labor-header-h);
    background: var(--surface-100);
    font-weight: 600;
    font-size: 0.75rem;
    text-align: center;
    position: sticky;
    top: 0;
    z-index: 2;
}

/* Match header alignment to data alignment */
.labor-table thead th.col-position { text-align: left; }
.labor-table thead th.col-rate,
.labor-table thead th.col-hours,
.labor-table thead th.col-money,
.labor-table thead th.col-total  { text-align: right; }

.labor-table tbody td {
    height: var(--labor-row-h);
}

.labor-table tfoot td {
    height: var(--labor-footer-h);
    background: var(--surface-card);
    border-top: 2px solid var(--surface-border);
}

.col-position { min-width: 140px; width: 140px; }
.col-type     { min-width: 72px;  width: 72px;  text-align: center; }
.col-shift    { min-width: 46px;  width: 46px;  text-align: center; }
.col-rate     { min-width: 72px;  width: 72px; }

.type-tag { font-size: 0.65rem; padding: 2px 6px; }
.rate-display { font-size: 0.78rem; font-variant-numeric: tabular-nums; }
.col-day   { min-width: 52px; text-align: center; }
.col-hours { min-width: 56px; width: 56px; text-align: right; }
.col-money { min-width: 88px; width: 88px; text-align: right; }
.col-total { min-width: 112px; width: 112px; text-align: right; font-weight: 700; }
.col-del   { width: 44px; min-width: 44px; text-align: center; padding: 0 4px; }

.labor-table tbody .col-del :deep(.p-button) {
    opacity: 0;
    pointer-events: none;
    transition: opacity 0.12s ease;
}

.labor-table tbody tr:hover .col-del :deep(.p-button),
.labor-table tbody tr:focus-within .col-del :deep(.p-button) {
    opacity: 1;
    pointer-events: auto;
}

/* ST = green, OT = orange, DT = red */
.col-st { color: #4ade80; }
.col-ot { color: #fb923c; }
.col-dt { color: #f87171; }

.labor-table thead th.col-st { color: #4ade80; }
.labor-table thead th.col-ot { color: #fb923c; }
.labor-table thead th.col-dt { color: #f87171; }

.totals-row td { background: var(--surface-card); border-top: 2px solid var(--surface-border); }
.totals-label { background: var(--surface-card) !important; }
.totals-spacer { background: transparent; }
.total-grand { color: var(--primary-color); font-size: 0.88rem; }

.day-weekend { background-color: rgba(255, 193, 7, 0.06); }
.day-sunday  { background-color: rgba(244, 67, 54, 0.06); }
.col-day.day-out-of-range { opacity: 0.2; pointer-events: none; background: transparent; }

/* ── Sticky left columns ─────────────────────────────────────────────────── */
/* Offsets: Position=0, Type=140, Shift=212, ST Rate=258, OT Rate=330, DT Rate=402 */
.sl-0   { position: sticky; left: 0;     z-index: 2; background: var(--surface-card); }
.sl-140 { position: sticky; left: 140px; z-index: 2; background: var(--surface-card); }
.sl-212 { position: sticky; left: 212px; z-index: 2; background: var(--surface-card); }
.sl-258 { position: sticky; left: 258px; z-index: 2; background: var(--surface-card); }
.sl-330 { position: sticky; left: 330px; z-index: 2; background: var(--surface-card); }
.sl-402 { position: sticky; left: 402px; z-index: 2; background: var(--surface-card); }

/* Header cells that are sticky both top AND left need higher z-index */
.labor-table thead th.sl-0,
.labor-table thead th.sl-140,
.labor-table thead th.sl-212,
.labor-table thead th.sl-258,
.labor-table thead th.sl-330,
.labor-table thead th.sl-402 { z-index: 4; background: var(--surface-100); }

/* ── Sticky right columns ────────────────────────────────────────────────── */
/* Delete is NOT sticky. TOTAL is the rightmost sticky column at right:0.   */
/* Widths: Total=112, DT$=88, OT$=88, ST$=88, DT_h=56, OT_h=56, ST_h=56   */
/* Cumulative right offsets: total=0, dt$=112, ot$=200, st$=288, dt_h=376, ot_h=432, st_h=488 */
.sr-36  { position: sticky; right: 0;     z-index: 2; background: var(--surface-card); }
.sr-80  { position: sticky; right: 112px; z-index: 2; background: var(--surface-card); }
.sr-168 { position: sticky; right: 200px; z-index: 2; background: var(--surface-card); }
.sr-256 { position: sticky; right: 288px; z-index: 2; background: var(--surface-card); }
.sr-344 { position: sticky; right: 376px; z-index: 2; background: var(--surface-card); }
.sr-400 { position: sticky; right: 432px; z-index: 2; background: var(--surface-card); }
.sr-456 { position: sticky; right: 488px; z-index: 2; background: var(--surface-card); }

.labor-table thead th.sr-36,
.labor-table thead th.sr-80,
.labor-table thead th.sr-168,
.labor-table thead th.sr-256,
.labor-table thead th.sr-344,
.labor-table thead th.sr-400,
.labor-table thead th.sr-456 { z-index: 4; background: var(--surface-100); }

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

/* Native number input for headcount cells — compact with browser spinner on hover */
.cell-headcount-native {
    width: 52px;
    height: 26px;
    text-align: center;
    font-size: 0.8rem;
    font-family: inherit;
    background: color-mix(in srgb, var(--shell-surface-alt) 75%, transparent);
    border: 1px solid var(--shell-border);
    border-radius: 4px;
    color: var(--shell-text);
    padding: 0 2px;
    outline: none;
    transition: border-color 0.15s, box-shadow 0.15s;
    /* Hide spinners by default */
    -moz-appearance: textfield;
}

.cell-headcount-native::-webkit-inner-spin-button,
.cell-headcount-native::-webkit-outer-spin-button {
    opacity: 0;
    transition: opacity 0.15s;
}

.cell-headcount-native:hover::-webkit-inner-spin-button,
.cell-headcount-native:hover::-webkit-outer-spin-button,
.cell-headcount-native:focus::-webkit-inner-spin-button,
.cell-headcount-native:focus::-webkit-outer-spin-button {
    opacity: 1;
    -webkit-appearance: inner-spin-button;
}

.cell-headcount-native:hover,
.cell-headcount-native:focus {
    border-color: #4d8fd4;
    box-shadow: 0 0 0 2px rgba(77, 143, 212, 0.22);
    -moz-appearance: number-input;
}

.cell-headcount-native:disabled {
    opacity: 0.25;
    pointer-events: none;
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

.rate-book-sep {
    color: var(--surface-border);
    font-size: 0.9rem;
    padding: 0 2px;
    user-select: none;
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
