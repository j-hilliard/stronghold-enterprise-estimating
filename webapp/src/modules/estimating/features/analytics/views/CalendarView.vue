<template>
    <div class="calendar-view">
        <BasePageHeader icon="pi pi-calendar" title="Calendar" subtitle="Estimate and staffing plan schedule overview">
            <div class="flex gap-2 align-items-center">
                <Button icon="pi pi-chevron-left" text @click="prevMonth" />
                <span class="month-label">{{ currentMonthLabel }}</span>
                <Button icon="pi pi-chevron-right" text @click="nextMonth" />
                <Button label="Today" outlined size="small" @click="goToToday" />
                <Button label="Refresh" icon="pi pi-refresh" :loading="loading" outlined size="small" @click="loadData" />
            </div>
        </BasePageHeader>

        <!-- Day of week headers -->
        <div class="cal-header-row">
            <div v-for="d in dayNames" :key="d" class="cal-dow">{{ d }}</div>
        </div>

        <!-- Calendar grid -->
        <div class="cal-grid">
            <div
                v-for="cell in calendarCells"
                :key="cell.key"
                class="cal-cell"
                :class="{ 'other-month': !cell.isCurrentMonth, today: cell.isToday }"
            >
                <div class="cell-date">{{ cell.day }}</div>
                <div class="cell-events">
                    <div
                        v-for="evt in cell.events.slice(0, MAX_VISIBLE)"
                        :key="evt.id"
                        class="cal-event"
                        :style="{ background: statusColor(evt.status) }"
                        :title="evt.name + ' \u2014 ' + evt.client"
                        @click="openEvent(evt)"
                    >
                        {{ evt.name }}
                    </div>
                    <div v-if="cell.events.length > MAX_VISIBLE" class="more-events">
                        +{{ cell.events.length - MAX_VISIBLE }} more
                    </div>
                </div>
            </div>
        </div>

        <div v-if="loading" class="flex justify-content-center py-4">
            <ProgressSpinner />
        </div>

        <div v-if="errorMessage" class="flex justify-content-center py-4">
            <Message severity="error" :closable="false">{{ errorMessage }}</Message>
        </div>
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

const MAX_VISIBLE = 4;

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

// ── State ──────────────────────────────────────────────────────────────────

const currentDate = ref(new Date());
const allEvents = ref<CalEvent[]>([]);
const loading = ref(false);
const errorMessage = ref('');

// ── Constants ──────────────────────────────────────────────────────────────

const dayNames = ['SUN', 'MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT'];

// ── Computed ───────────────────────────────────────────────────────────────

const currentMonthLabel = computed(() => {
    return currentDate.value.toLocaleString('default', { month: 'long', year: 'numeric' });
});

const calendarCells = computed((): CalCell[] => {
    const year = currentDate.value.getFullYear();
    const month = currentDate.value.getMonth();

    const firstOfMonth = new Date(year, month, 1);
    const startDow = firstOfMonth.getDay(); // 0 = Sunday

    const today = new Date();
    const todayStr = dateKey(today);

    const cells: CalCell[] = [];

    // Start from the Sunday of the week containing the 1st
    const gridStart = new Date(year, month, 1 - startDow);

    // Build 6 rows × 7 cols = 42 cells
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

    // Trim trailing empty rows (last 7 cells are all other-month and have no events)
    if (cells.slice(35).every(c => !c.isCurrentMonth && c.events.length === 0)) {
        return cells.slice(0, 35);
    }
    return cells;
});

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

function statusColor(status: string): string {
    const s = status.toLowerCase();
    if (s === 'awarded' || s === 'active') return '#15803d';
    if (s === 'pending' || s === 'proposed') return '#b45309';
    if (s === 'approved') return '#7c3aed';
    if (s === 'lost' || s === 'canceled') return '#dc2626';
    if (s === 'converted' || s === 'archived') return '#475569';
    return '#2563eb'; // draft / default
}

// ── Navigation ─────────────────────────────────────────────────────────────

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

// ── Event navigation ───────────────────────────────────────────────────────

function openEvent(evt: CalEvent) {
    if (evt.type === 'estimate') {
        router.push(`/estimating/estimates/${evt.sourceId}`);
    } else {
        router.push(`/estimating/staffing-plans/${evt.sourceId}`);
    }
}

// ── Data loading ───────────────────────────────────────────────────────────

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
    gap: 8px;
    padding-bottom: 24px;
}

.month-label {
    min-width: 160px;
    text-align: center;
    font-weight: 600;
    font-size: 1rem;
    color: var(--text-color);
    line-height: 2rem;
}

.cal-header-row {
    display: grid;
    grid-template-columns: repeat(7, 1fr);
    background: #1e293b;
    border-radius: 6px 6px 0 0;
}

.cal-dow {
    text-align: center;
    padding: 8px 4px;
    font-size: 0.72rem;
    font-weight: 700;
    letter-spacing: 0.08em;
    color: #94a3b8;
    text-transform: uppercase;
}

.cal-dow:first-child,
.cal-dow:last-child {
    color: #f87171;
}

.cal-grid {
    display: grid;
    grid-template-columns: repeat(7, 1fr);
    border: 1px solid var(--surface-border);
    border-radius: 0 0 6px 6px;
    overflow: hidden;
}

.cal-cell {
    min-height: 110px;
    padding: 4px;
    border-right: 1px solid var(--surface-border);
    border-bottom: 1px solid var(--surface-border);
    background: var(--surface-card);
    overflow: hidden;
}

.cal-cell.other-month {
    background: var(--surface-ground);
    opacity: 0.6;
}

.cal-cell.today {
    background: color-mix(in srgb, #3b82f6 8%, var(--surface-card));
}

.cell-date {
    font-size: 0.75rem;
    font-weight: 600;
    color: var(--text-color-secondary);
    margin-bottom: 3px;
}

.cal-cell.today .cell-date {
    color: #3b82f6;
    font-weight: 700;
}

.cell-events {
    display: flex;
    flex-direction: column;
    gap: 2px;
}

.cal-event {
    font-size: 0.68rem;
    padding: 2px 5px;
    border-radius: 3px;
    cursor: pointer;
    color: #fff;
    font-weight: 500;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    opacity: 0.9;
}

.cal-event:hover {
    opacity: 1;
    filter: brightness(1.1);
}

.more-events {
    font-size: 0.65rem;
    color: var(--text-color-secondary);
    padding: 1px 4px;
}
</style>
