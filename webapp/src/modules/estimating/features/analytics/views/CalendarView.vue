<template>
    <div class="calendar-view">
        <BasePageHeader icon="pi pi-calendar" title="Calendar" subtitle="Estimate and staffing plan schedule overview">
            <div class="cal-nav">
                <Button icon="pi pi-chevron-left" text rounded @click="prevMonth" />
                <span class="month-label">{{ currentMonthLabel }}</span>
                <Button icon="pi pi-chevron-right" text rounded @click="nextMonth" />
                <Button label="Today" outlined size="small" @click="goToToday" />
                <Button label="Refresh" icon="pi pi-refresh" :loading="loading" outlined size="small" @click="loadData" />
            </div>
        </BasePageHeader>

        <!-- Status legend -->
        <div class="cal-legend">
            <span v-for="leg in legend" :key="leg.label" class="legend-item">
                <span class="legend-dot" :style="{ background: leg.color }" />
                {{ leg.label }}
            </span>
        </div>

        <!-- Day-of-week header row -->
        <div class="cal-header-row">
            <div v-for="(d, i) in dayNames" :key="d" class="cal-dow" :class="{ weekend: i === 0 || i === 6 }">{{ d }}</div>
        </div>

        <!-- Calendar grid — one div per week row -->
        <div class="cal-grid" v-if="!loading">
            <div v-for="(week, wi) in weeks" :key="wi" class="week-row">

                <!-- Row 1: day number cells -->
                <div
                    v-for="(cell, di) in week"
                    :key="cell.key"
                    class="day-num-cell"
                    :class="{
                        'other-month': !cell.isCurrentMonth,
                        'is-today': cell.isToday,
                        'is-weekend': di === 0 || di === 6,
                        'is-sat': di === 6,
                    }"
                >
                    <span class="day-number" :class="{ 'today-bubble': cell.isToday }">{{ cell.day }}</span>
                </div>

                <!-- Row 2: event bars spanning grid columns -->
                <div class="events-lane">
                    <!-- Column background underlay (for weekend tinting + vertical lines) -->
                    <div
                        v-for="di in 7"
                        :key="`bg-${di}`"
                        class="lane-col-bg"
                        :class="{ 'lane-weekend': di === 1 || di === 7 }"
                        :style="{ gridColumn: di, gridRow: '1 / -1' }"
                    />

                    <!-- Event bars -->
                    <div
                        v-for="bar in weekBars[wi]"
                        :key="`${bar.evt.id}-${wi}`"
                        class="event-bar"
                        :class="{
                            'bar-start': bar.isStart,
                            'bar-end': bar.isEnd,
                            'bar-only': bar.isStart && bar.isEnd,
                        }"
                        :style="{
                            gridColumn: `${bar.colStart} / span ${bar.colSpan}`,
                            gridRow: bar.lane,
                            '--bar-color': barColor(bar.evt.status),
                        }"
                        :title="`${bar.evt.name} — ${bar.evt.client}\n${fmtDate(bar.evt.startDate)} → ${fmtDate(bar.evt.endDate)}`"
                        @click="openEvent(bar.evt)"
                    >
                        <span v-if="bar.isStart || bar.colStart === 1" class="bar-label">
                            <span class="bar-number">{{ bar.evt.number }}</span>
                            {{ bar.evt.name }}
                        </span>
                    </div>

                    <!-- Per-day overflow pills (positioned in lane MAX_LANES + 1) -->
                    <template v-for="(count, di) in weekOverflow[wi]" :key="`ov-${wi}-${di}`">
                        <div
                            v-if="count > 0"
                            class="overflow-pill"
                            :style="{ gridColumn: di + 1, gridRow: MAX_LANES + 1 }"
                            @click.stop="showDayOverflow(week[di])"
                        >
                            +{{ count }} more
                        </div>
                    </template>
                </div>
            </div>
        </div>

        <div v-if="loading" class="flex justify-center py-10">
            <ProgressSpinner />
        </div>

        <div v-if="errorMessage" class="flex justify-center py-4">
            <Message severity="error" :closable="false">{{ errorMessage }}</Message>
        </div>

        <!-- Day overflow dialog -->
        <Dialog
            v-model:visible="overflowDialogVisible"
            :header="overflowDayCell ? overflowDayCell.date.toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric' }) : ''"
            modal
            :style="{ width: '400px' }"
            :dismissableMask="true"
        >
            <div class="overflow-event-list">
                <div
                    v-for="evt in overflowDayEvents"
                    :key="evt.id"
                    class="overflow-event-row"
                    @click="openEvent(evt); overflowDialogVisible = false"
                >
                    <div class="overflow-dot" :style="{ background: barColor(evt.status) }" />
                    <div class="overflow-info">
                        <span class="overflow-name">{{ evt.name }}</span>
                        <span class="overflow-meta">{{ evt.client }} · {{ evt.number }} · {{ evt.status }}</span>
                        <span class="overflow-dates">{{ fmtDate(evt.startDate) }} → {{ fmtDate(evt.endDate) }}</span>
                    </div>
                    <i class="pi pi-arrow-right overflow-arrow" />
                </div>
            </div>
        </Dialog>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useToast } from 'primevue/usetoast';
import { useApiStore } from '@/stores/apiStore';
import BasePageHeader from '@/components/layout/BasePageHeader.vue';

const router = useRouter();
const toast = useToast();
const apiStore = useApiStore();

const MAX_LANES = 3;

// ── Types ──────────────────────────────────────────────────────────────────

interface CalEvent {
    id: string;
    type: 'estimate' | 'staffing';
    sourceId: number;
    name: string;
    client: string;
    status: string;
    startDate: Date | null;
    endDate: Date | null;
    number: string;
}

interface CalCell {
    key: string;
    date: Date;
    day: number;
    isCurrentMonth: boolean;
    isToday: boolean;
    events: CalEvent[];
}

interface EventBar {
    evt: CalEvent;
    colStart: number;  // 1-based CSS grid column
    colSpan: number;
    isStart: boolean;  // true when the event's real start is in this week
    isEnd: boolean;    // true when the event's real end is in this week
    lane: number;      // 1-based CSS grid-row within events-lane
}

// ── State ──────────────────────────────────────────────────────────────────

const currentDate = ref(new Date());
const allEvents = ref<CalEvent[]>([]);
const loading = ref(false);
const errorMessage = ref('');
const overflowDialogVisible = ref(false);
const overflowDayCell = ref<CalCell | null>(null);

// ── Constants ──────────────────────────────────────────────────────────────

const dayNames = ['SUN', 'MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT'];

const legend = [
    { label: 'Draft', color: 'rgba(37,99,235,0.85)' },
    { label: 'Pending', color: 'rgba(180,83,9,0.85)' },
    { label: 'Submitted', color: 'rgba(14,116,144,0.85)' },
    { label: 'Awarded', color: 'rgba(21,128,61,0.85)' },
    { label: 'Approved', color: 'rgba(124,58,237,0.85)' },
    { label: 'Lost/Canceled', color: 'rgba(220,38,38,0.75)' },
];

// ── Computed — calendar cells (unchanged business logic) ───────────────────

const currentMonthLabel = computed(() => {
    return currentDate.value.toLocaleString('default', { month: 'long', year: 'numeric' });
});

const calendarCells = computed((): CalCell[] => {
    const year = currentDate.value.getFullYear();
    const month = currentDate.value.getMonth();

    const firstOfMonth = new Date(year, month, 1);
    const startDow = firstOfMonth.getDay();

    const today = new Date();
    const todayStr = dateKey(today);

    const cells: CalCell[] = [];
    const gridStart = new Date(year, month, 1 - startDow);

    for (let i = 0; i < 42; i++) {
        const cellDate = new Date(gridStart);
        cellDate.setDate(gridStart.getDate() + i);

        const cellStr = dateKey(cellDate);
        const isCurrentMonth = cellDate.getMonth() === month && cellDate.getFullYear() === year;

        const events = allEvents.value.filter(evt => {
            const start = evt.startDate;
            const end = evt.endDate ?? evt.startDate;
            if (!start) return false;
            const startStr = dateKey(start);
            const endStr = end ? dateKey(end) : startStr;
            return cellStr >= startStr && cellStr <= endStr;
        });

        cells.push({
            key: cellStr,
            date: cellDate,
            day: cellDate.getDate(),
            isCurrentMonth,
            isToday: cellStr === todayStr,
            events,
        });
    }

    if (cells.slice(35).every(c => !c.isCurrentMonth && c.events.length === 0)) {
        return cells.slice(0, 35);
    }
    return cells;
});

// ── Computed — week-based layout ───────────────────────────────────────────

const weeks = computed((): CalCell[][] => {
    const cells = calendarCells.value;
    const result: CalCell[][] = [];
    for (let i = 0; i < cells.length; i += 7) {
        result.push(cells.slice(i, i + 7));
    }
    return result;
});

const allWeekData = computed(() => weeks.value.map(week => computeWeekData(week)));
const weekBars = computed(() => allWeekData.value.map(w => w.bars));
const weekOverflow = computed(() => allWeekData.value.map(w => w.overflow));

function computeWeekData(week: CalCell[]): { bars: EventBar[]; overflow: number[] } {
    if (week.length === 0) return { bars: [], overflow: [] };

    const weekStartStr = week[0].key;
    const weekEndStr = week[6].key;

    // All events active during this week, sorted by start then by descending duration
    const active = allEvents.value
        .filter(evt => {
            if (!evt.startDate) return false;
            const s = dateKey(evt.startDate);
            const e = evt.endDate ? dateKey(evt.endDate) : s;
            return s <= weekEndStr && e >= weekStartStr;
        })
        .sort((a, b) => {
            const as = dateKey(a.startDate!);
            const bs = dateKey(b.startDate!);
            if (as !== bs) return as < bs ? -1 : 1;
            const ae = a.endDate ? dateKey(a.endDate) : as;
            const be = b.endDate ? dateKey(b.endDate) : bs;
            // Longer first so they claim lanes before shorter overlapping events
            return be > ae ? 1 : be < ae ? -1 : 0;
        });

    // Greedy lane assignment
    const laneOccupancy: { colStart: number; colEnd: number }[][] = [];
    const bars: EventBar[] = [];
    const hiddenIds = new Set<string>();

    for (const evt of active) {
        const s = dateKey(evt.startDate!);
        const e = evt.endDate ? dateKey(evt.endDate) : s;

        const clipStartStr = s < weekStartStr ? weekStartStr : s;
        const clipEndStr = e > weekEndStr ? weekEndStr : e;

        let colStart = week.findIndex(c => c.key === clipStartStr);
        let colEnd = week.findIndex(c => c.key === clipEndStr);
        if (colStart === -1) colStart = 0;
        if (colEnd === -1) colEnd = 6;
        const colSpan = colEnd - colStart + 1;

        let lane = -1;
        for (let li = 0; li < laneOccupancy.length; li++) {
            if (!laneOccupancy[li].some(seg => seg.colStart <= colEnd && seg.colEnd >= colStart)) {
                lane = li;
                break;
            }
        }
        if (lane === -1) {
            if (laneOccupancy.length < MAX_LANES) {
                lane = laneOccupancy.length;
                laneOccupancy.push([]);
            } else {
                hiddenIds.add(evt.id);
                continue;
            }
        }

        laneOccupancy[lane].push({ colStart, colEnd });
        bars.push({
            evt,
            colStart: colStart + 1,
            colSpan,
            isStart: s >= weekStartStr && s === clipStartStr,
            isEnd: e <= weekEndStr && e === clipEndStr,
            lane: lane + 1,
        });
    }

    const overflow = week.map(cell => {
        if (hiddenIds.size === 0) return 0;
        return active.filter(evt => {
            if (!hiddenIds.has(evt.id)) return false;
            const s = dateKey(evt.startDate!);
            const e = evt.endDate ? dateKey(evt.endDate) : s;
            return s <= cell.key && e >= cell.key;
        }).length;
    });

    return { bars, overflow };
}

// ── Overflow dialog ─────────────────────────────────────────────────────────

const overflowDayEvents = computed(() => {
    if (!overflowDayCell.value) return [];
    const dayStr = overflowDayCell.value.key;
    return allEvents.value.filter(evt => {
        if (!evt.startDate) return false;
        const s = dateKey(evt.startDate);
        const e = evt.endDate ? dateKey(evt.endDate) : s;
        return s <= dayStr && e >= dayStr;
    });
});

function showDayOverflow(cell: CalCell) {
    overflowDayCell.value = cell;
    overflowDialogVisible.value = true;
}

// ── Helpers ────────────────────────────────────────────────────────────────

function dateKey(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
}

function parseDate(val: string | null | undefined): Date | null {
    if (!val) return null;
    const d = new Date(val);
    return isNaN(d.getTime()) ? null : d;
}

function fmtDate(d: Date | null): string {
    if (!d) return '—';
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function barColor(status: string): string {
    const s = status.toLowerCase();
    if (s === 'awarded' || s === 'active') return 'rgba(21,128,61,0.88)';
    if (s === 'pending' || s === 'proposed') return 'rgba(180,83,9,0.88)';
    if (s === 'approved') return 'rgba(124,58,237,0.88)';
    if (s === 'lost' || s === 'canceled') return 'rgba(220,38,38,0.75)';
    if (s === 'converted' || s === 'archived') return 'rgba(71,85,105,0.88)';
    if (s === 'submitted for approval') return 'rgba(14,116,144,0.88)';
    return 'rgba(37,99,235,0.88)'; // draft / default
}

// ── Navigation (unchanged) ─────────────────────────────────────────────────

function prevMonth() {
    const d = new Date(currentDate.value);
    d.setDate(1);
    d.setMonth(d.getMonth() - 1);
    currentDate.value = d;
}

function nextMonth() {
    const d = new Date(currentDate.value);
    d.setDate(1);
    d.setMonth(d.getMonth() + 1);
    currentDate.value = d;
}

function goToToday() {
    const d = new Date();
    d.setDate(1);
    currentDate.value = d;
}

// ── Event navigation (unchanged) ───────────────────────────────────────────

function openEvent(evt: CalEvent) {
    if (evt.type === 'estimate') {
        router.push(`/estimating/estimates/${evt.sourceId}`);
    } else {
        router.push(`/estimating/staffing-plans/${evt.sourceId}`);
    }
}

// ── Data loading (unchanged) ───────────────────────────────────────────────

async function loadData() {
    loading.value = true;
    errorMessage.value = '';
    try {
        const [estRes, spRes] = await Promise.all([
            apiStore.api.get('/api/v1/estimates?page=1&pageSize=200'),
            apiStore.api.get('/api/v1/staffing-plans?page=1&pageSize=200'),
        ]);

        const estimates: CalEvent[] = (estRes.data.items ?? []).map((item: {
            estimateId: number;
            name: string;
            client: string;
            status: string;
            startDate?: string;
            endDate?: string;
            estimateNumber: string;
        }) => ({
            id: `e-${item.estimateId}`,
            type: 'estimate' as const,
            sourceId: item.estimateId,
            name: item.name,
            client: item.client ?? '',
            status: item.status ?? '',
            startDate: parseDate(item.startDate),
            endDate: parseDate(item.endDate),
            number: item.estimateNumber ?? '',
        }));

        const staffingPlans: CalEvent[] = (spRes.data.items ?? []).map((item: {
            staffingPlanId: number;
            name: string;
            client: string;
            status: string;
            startDate?: string;
            endDate?: string;
            staffingPlanNumber: string;
        }) => ({
            id: `sp-${item.staffingPlanId}`,
            type: 'staffing' as const,
            sourceId: item.staffingPlanId,
            name: item.name,
            client: item.client ?? '',
            status: item.status ?? '',
            startDate: parseDate(item.startDate),
            endDate: parseDate(item.endDate),
            number: item.staffingPlanNumber ?? '',
        }));

        allEvents.value = [...estimates, ...staffingPlans];
    } catch {
        errorMessage.value = 'Failed to load calendar data.';
        toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load calendar data', life: 3000 });
    } finally {
        loading.value = false;
    }
}

// ── Lifecycle ──────────────────────────────────────────────────────────────

onMounted(() => {
    const d = new Date();
    d.setDate(1);
    currentDate.value = d;
    loadData();
});
</script>

<style scoped>
.calendar-view {
    display: flex;
    flex-direction: column;
    gap: 0;
    padding-bottom: 24px;
}

/* ── Nav ─────────────────────────────────────────────────────────────────── */
.cal-nav {
    display: flex;
    align-items: center;
    gap: 4px;
}

.month-label {
    min-width: 170px;
    text-align: center;
    font-weight: 600;
    font-size: 1rem;
    color: var(--text-color);
}

/* ── Legend ──────────────────────────────────────────────────────────────── */
.cal-legend {
    display: flex;
    gap: 1rem;
    align-items: center;
    padding: 6px 12px;
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-bottom: none;
    border-radius: 8px 8px 0 0;
    flex-wrap: wrap;
}

.legend-item {
    display: flex;
    align-items: center;
    gap: 5px;
    font-size: 0.68rem;
    color: var(--text-color-secondary);
    font-weight: 500;
    letter-spacing: 0.02em;
}

.legend-dot {
    width: 8px;
    height: 8px;
    border-radius: 2px;
    flex-shrink: 0;
}

/* ── DOW header ──────────────────────────────────────────────────────────── */
.cal-header-row {
    display: grid;
    grid-template-columns: repeat(7, 1fr);
    background: var(--surface-ground);
    border: 1px solid var(--surface-border);
    border-top: none;
    border-bottom: none;
}

.cal-dow {
    text-align: center;
    padding: 6px 4px;
    font-size: 0.68rem;
    font-weight: 700;
    letter-spacing: 0.1em;
    color: var(--text-color-secondary);
    text-transform: uppercase;
}

.cal-dow.weekend {
    color: #f87171;
    opacity: 0.85;
}

/* ── Calendar grid ───────────────────────────────────────────────────────── */
.cal-grid {
    display: flex;
    flex-direction: column;
    border: 1px solid var(--surface-border);
    border-radius: 0 0 8px 8px;
    overflow: hidden;
}

/* ── Week row ────────────────────────────────────────────────────────────── */
.week-row {
    display: grid;
    grid-template-columns: repeat(7, 1fr);
    grid-template-rows: 28px auto;
    border-bottom: 1px solid var(--surface-border);
}

.week-row:last-child {
    border-bottom: none;
}

/* ── Day number cells (row 1 of the week-row grid) ───────────────────────── */
.day-num-cell {
    grid-row: 1;
    display: flex;
    align-items: center;
    justify-content: flex-end;
    padding: 4px 8px 4px 4px;
    background: var(--surface-card);
    border-right: 1px solid var(--surface-border);
}

.day-num-cell:last-child {
    border-right: none;
}

.day-num-cell.other-month .day-number {
    opacity: 0.28;
}

.day-num-cell.is-today {
    background: color-mix(in srgb, #3b82f6 6%, var(--surface-card));
}

.day-num-cell.is-weekend {
    background: color-mix(in srgb, var(--surface-ground) 60%, var(--surface-card));
}

.day-number {
    font-size: 0.75rem;
    font-weight: 600;
    color: var(--text-color-secondary);
    width: 22px;
    height: 22px;
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 50%;
    flex-shrink: 0;
}

.today-bubble {
    background: #3b82f6;
    color: #fff;
    font-weight: 700;
}

/* ── Events lane (row 2, spans all 7 columns) ────────────────────────────── */
.events-lane {
    grid-column: 1 / -1;
    grid-row: 2;
    display: grid;
    grid-template-columns: repeat(7, 1fr);
    grid-auto-rows: 22px;
    row-gap: 2px;
    padding: 3px 0 5px;
    min-height: calc(v-bind(MAX_LANES) * 24px + 10px);
    position: relative;
}

/* Column background underlay */
.lane-col-bg {
    pointer-events: none;
    background: transparent;
}

.lane-col-bg.lane-weekend {
    background: color-mix(in srgb, var(--surface-ground) 45%, transparent);
}

/* ── Event bars ──────────────────────────────────────────────────────────── */
.event-bar {
    background: var(--bar-color, #2563eb);
    color: rgba(255, 255, 255, 0.95);
    font-size: 0.67rem;
    font-weight: 500;
    height: 20px;
    display: flex;
    align-items: center;
    padding: 0 4px;
    cursor: pointer;
    overflow: hidden;
    /* default: middle of a multi-day span — no rounding, flush edges */
    border-radius: 0;
    margin: 0;
    opacity: 0.9;
    transition: opacity 0.1s, filter 0.1s;
    position: relative;
    z-index: 1;
}

.event-bar:hover {
    opacity: 1;
    filter: brightness(1.15);
    z-index: 10;
}

/* Left-rounded: event starts in this week on this column */
.event-bar.bar-start {
    border-radius: 4px 0 0 4px;
    margin-left: 3px;
    padding-left: 7px;
}

/* Right-rounded: event ends in this week on this column */
.event-bar.bar-end {
    border-radius: 0 4px 4px 0;
    margin-right: 3px;
}

/* Single-day event: fully rounded */
.event-bar.bar-only {
    border-radius: 4px;
    margin-left: 3px;
    margin-right: 3px;
}

.bar-label {
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    flex: 1;
    min-width: 0;
}

.bar-number {
    font-size: 0.6rem;
    opacity: 0.75;
    margin-right: 4px;
    font-variant-numeric: tabular-nums;
}

/* ── Overflow pill ───────────────────────────────────────────────────────── */
.overflow-pill {
    display: flex;
    align-items: center;
    padding: 0 6px;
    height: 18px;
    font-size: 0.62rem;
    font-weight: 600;
    color: var(--text-color-secondary);
    cursor: pointer;
    border-radius: 4px;
    margin: 0 3px;
    transition: background 0.1s, color 0.1s;
}

.overflow-pill:hover {
    background: var(--surface-hover);
    color: var(--text-color);
}

/* ── Overflow dialog ─────────────────────────────────────────────────────── */
.overflow-event-list {
    display: flex;
    flex-direction: column;
    gap: 4px;
}

.overflow-event-row {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 8px 10px;
    border-radius: 6px;
    cursor: pointer;
    transition: background 0.1s;
}

.overflow-event-row:hover {
    background: var(--surface-hover);
}

.overflow-dot {
    width: 10px;
    height: 10px;
    border-radius: 3px;
    flex-shrink: 0;
}

.overflow-info {
    display: flex;
    flex-direction: column;
    gap: 2px;
    flex: 1;
    min-width: 0;
}

.overflow-name {
    font-size: 0.85rem;
    font-weight: 600;
    color: var(--text-color);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.overflow-meta {
    font-size: 0.72rem;
    color: var(--text-color-secondary);
}

.overflow-dates {
    font-size: 0.7rem;
    color: var(--text-color-secondary);
    opacity: 0.75;
}

.overflow-arrow {
    color: var(--text-color-secondary);
    font-size: 0.75rem;
    flex-shrink: 0;
}
</style>
