<template>
    <Card class="expense-section-card">
        <template #title>
            <div class="flex align-items-center justify-content-between gap-2">
                <div class="flex align-items-center gap-2">
                    <i class="pi pi-receipt text-primary" />
                    <span>Expenses / Per Diem</span>
                    <Tag :value="`${rows.length} row${rows.length !== 1 ? 's' : ''}`" severity="secondary" />
                </div>
                <div class="flex gap-2">
                    <Button label="+ Auto Per Diem" size="small" @click="togglePerDiemPanel" />
                    <Button label="+ Expense" size="small" outlined @click="toggleExpensePanel" />
                </div>
            </div>
        </template>
        <template #content>
            <!-- Per Diem popover -->
            <OverlayPanel ref="perDiemPanel">
                <div class="per-diem-menu">
                    <div
                        v-for="item in perDiemItems"
                        :key="item.description"
                        class="per-diem-item"
                        @click="addAutoRow(item, perDiemPanel)"
                    >
                        <span class="per-diem-name">{{ item.description }}</span>
                        <span class="per-diem-rate">${{ item.rate }}/{{ item.unit }}</span>
                    </div>
                    <div v-if="perDiemItems.length === 0" class="per-diem-empty">No per diem rates in rate book</div>
                </div>
            </OverlayPanel>

            <!-- Travel / Lodging / Mileage popover -->
            <OverlayPanel ref="expensePanel">
                <div class="per-diem-menu">
                    <div
                        v-for="item in travelItems"
                        :key="item.description"
                        class="per-diem-item"
                        @click="addAutoRow(item, expensePanel)"
                    >
                        <span class="per-diem-name">{{ item.description }}</span>
                        <span class="per-diem-rate">${{ item.rate }}/{{ item.unit }}</span>
                    </div>
                    <div v-if="travelItems.length === 0" class="per-diem-empty">No travel/lodging rates in rate book</div>
                    <div class="per-diem-divider" />
                    <div class="per-diem-item" @click="addManualRow">
                        <span class="per-diem-name">+ Manual expense row</span>
                    </div>
                </div>
            </OverlayPanel>

            <DataTable :value="rows" class="expense-table" size="small" :scrollable="true" scrollHeight="400px">
                <Column header="Description" style="min-width:260px">
                    <template #body="{ data, index }">
                        <InputText v-model="data.description" placeholder="Description" class="w-full" @input="onChange(index)" />
                    </template>
                </Column>
                <Column header="Bill Rate" style="min-width:120px">
                    <template #body="{ data, index }">
                        <InputNumber v-model="data.rate" mode="currency" currency="USD" :minFractionDigits="2" class="w-full" @input="recalc(index)" />
                    </template>
                </Column>
                <Column header="Days/QTY" style="min-width:90px">
                    <template #body="{ data, index }">
                        <InputNumber v-model="data.daysOrQty" :min="1" class="w-full" @input="recalc(index)" />
                    </template>
                </Column>
                <Column header="People" style="min-width:80px">
                    <template #body="{ data, index }">
                        <InputNumber v-model="data.people" :min="1" class="w-full" @input="recalc(index)" />
                    </template>
                </Column>
                <Column header="Billable" style="min-width:90px">
                    <template #body="{ data, index }">
                        <ToggleButton v-model="data.billable" onLabel="Yes" offLabel="No" onIcon="pi pi-check" offIcon="pi pi-times" @change="recalc(index)" />
                    </template>
                </Column>
                <Column header="Subtotal" style="min-width:120px">
                    <template #body="{ data }">
                        <span class="font-semibold" :class="data.billable ? 'text-primary' : 'opacity-50'">{{ fmtCurrency(data.subtotal) }}</span>
                    </template>
                </Column>
                <Column style="width:48px">
                    <template #body="{ index }">
                        <Button icon="pi pi-trash" text rounded size="small" severity="danger" @click="removeRow(index)" />
                    </template>
                </Column>
                <template #footer>
                    <div class="flex justify-content-end gap-4 pr-2">
                        <span>Billable: <strong class="text-primary">{{ fmtCurrency(billableTotal) }}</strong></span>
                        <span class="opacity-60">Non-Billable: {{ fmtCurrency(nonBillableTotal) }}</span>
                    </div>
                </template>
            </DataTable>
            <div v-if="rows.length === 0" class="text-center py-4 text-slate-400">
                No expenses added — use buttons above for out-of-town jobs.
            </div>
        </template>
    </Card>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import Card from 'primevue/card';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import OverlayPanel from 'primevue/overlaypanel';
import type { ExpenseRow, LaborRow, RateBookExpenseItem } from '../../../stores/estimateStore';

const props = defineProps<{
    rows: ExpenseRow[];
    jobDays?: number;
    laborRows?: LaborRow[];
    rateBookExpenseItems?: RateBookExpenseItem[];
}>();
const emits = defineEmits<{ (e: 'update:rows', v: ExpenseRow[]): void; (e: 'change'): void }>();

const perDiemPanel = ref();
const expensePanel = ref();

function togglePerDiemPanel(event: Event) { perDiemPanel.value.toggle(event); }
function toggleExpensePanel(event: Event) { expensePanel.value.toggle(event); }

const perDiemItems = computed(() =>
    (props.rateBookExpenseItems ?? []).filter(r => r.category === 'PerDiem')
);
const travelItems = computed(() =>
    (props.rateBookExpenseItems ?? []).filter(r => r.category === 'Travel' || r.category === 'Lodging')
);

function peakHeadcount(rows: LaborRow[]): number {
    const dailyTotal = new Map<string, number>();
    for (const row of rows) {
        try {
            const sched = JSON.parse(row.scheduleJson ?? '{}') as Record<string, number>;
            for (const [date, count] of Object.entries(sched)) {
                dailyTotal.set(date, (dailyTotal.get(date) ?? 0) + count);
            }
        } catch { /* */ }
    }
    if (dailyTotal.size === 0) return rows.length || 1;
    return Math.max(...dailyTotal.values()) || 1;
}

const directCount = computed(() =>
    peakHeadcount((props.laborRows ?? []).filter(r => r.laborType === 'Direct'))
);
const indirectCount = computed(() =>
    peakHeadcount((props.laborRows ?? []).filter(r => r.laborType === 'Indirect'))
);

function addAutoRow(item: RateBookExpenseItem, panelRef: any) {
    panelRef.hide();
    const isIndirect = item.description.toLowerCase().includes('indirect');
    const people = isIndirect ? indirectCount.value : directCount.value;
    const daysOrQty = props.jobDays || 1;
    const newRow: ExpenseRow = {
        category: item.category,
        type: isIndirect ? 'Indirect' : 'Direct',
        description: item.description,
        rate: item.rate,
        unit: item.unit,
        daysOrQty,
        people,
        billable: true,
        subtotal: Math.round(item.rate * daysOrQty * people * 100) / 100,
    };
    emits('update:rows', [...props.rows, newRow]);
    emits('change');
}

function addManualRow() {
    const newRow: ExpenseRow = {
        category: 'PerDiem',
        type: 'Direct',
        description: '',
        rate: 0,
        unit: 'Day',
        daysOrQty: props.jobDays || 1,
        people: directCount.value,
        billable: true,
        subtotal: 0,
    };
    emits('update:rows', [...props.rows, newRow]);
    emits('change');
}

function recalc(idx: number) {
    const r = props.rows[idx];
    r.subtotal = Math.round(r.rate * r.daysOrQty * r.people * 100) / 100;
    emits('change');
    emitRows();
}

function onChange(_idx: number) {
    emits('change');
    emitRows();
}

function removeRow(idx: number) {
    const updated = [...props.rows];
    updated.splice(idx, 1);
    emits('update:rows', updated);
    emits('change');
}

function emitRows() { emits('update:rows', [...props.rows]); }

const billableTotal = computed(() => props.rows.filter(r => r.billable).reduce((s, r) => s + r.subtotal, 0));
const nonBillableTotal = computed(() => props.rows.filter(r => !r.billable).reduce((s, r) => s + r.subtotal, 0));

function fmtCurrency(n: number): string {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(n ?? 0);
}
</script>

<style scoped>
.per-diem-menu {
    min-width: 260px;
}
.per-diem-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 8px 12px;
    cursor: pointer;
    border-radius: 4px;
    gap: 16px;
}
.per-diem-item:hover {
    background: var(--surface-hover);
}
.per-diem-name {
    font-weight: 500;
}
.per-diem-rate {
    color: var(--primary-color);
    font-weight: 600;
    white-space: nowrap;
}
.per-diem-divider {
    border-top: 1px solid var(--surface-border);
    margin: 4px 0;
}
.per-diem-empty {
    padding: 8px 12px;
    color: var(--text-color-secondary);
    font-style: italic;
    font-size: 0.875rem;
}
</style>
