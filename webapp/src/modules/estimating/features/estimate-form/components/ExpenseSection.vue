<template>
    <Card class="expense-section-card">
        <template #title>
            <div class="flex align-items-center justify-content-between gap-2">
                <div class="flex align-items-center gap-2">
                    <i class="pi pi-receipt text-primary" />
                    <span>Expenses / Per Diem</span>
                    <Tag :value="`${rows.length} row${rows.length !== 1 ? 's' : ''}`" severity="secondary" />
                </div>
                <Button label="Add Row" icon="pi pi-plus" size="small" outlined @click="addRow" />
            </div>
        </template>
        <template #content>
            <DataTable :value="rows" class="expense-table" size="small" :scrollable="true" scrollHeight="400px">
                <Column header="Category" style="min-width:130px">
                    <template #body="{ data, index }">
                        <Dropdown v-model="data.category" :options="categoryOptions" class="w-full" @change="onChange(index)" />
                    </template>
                </Column>
                <Column header="Description" style="min-width:200px">
                    <template #body="{ data, index }">
                        <InputText v-model="data.description" placeholder="Description" class="w-full" @input="onChange(index)" />
                    </template>
                </Column>
                <Column header="Rate" style="min-width:120px">
                    <template #body="{ data, index }">
                        <InputNumber v-model="data.rate" mode="currency" currency="USD" :minFractionDigits="2" class="w-full" @input="recalc(index)" />
                    </template>
                </Column>
                <Column header="Unit" style="min-width:100px">
                    <template #body="{ data, index }">
                        <Dropdown v-model="data.unit" :options="unitOptions" editable class="w-full" @change="onChange(index)" />
                    </template>
                </Column>
                <Column header="Qty / Days" style="min-width:90px">
                    <template #body="{ data, index }">
                        <InputNumber v-model="data.daysOrQty" :min="1" class="w-full" @input="recalc(index)" />
                    </template>
                </Column>
                <Column header="People" style="min-width:80px">
                    <template #body="{ data, index }">
                        <InputNumber v-model="data.people" :min="1" class="w-full" @input="recalc(index)" />
                    </template>
                </Column>
                <Column header="Billable" style="min-width:80px">
                    <template #body="{ data, index }">
                        <ToggleButton v-model="data.billable" onLabel="Yes" offLabel="No" onIcon="pi pi-check" offIcon="pi pi-times" @change="recalc(index)" />
                    </template>
                </Column>
                <Column header="Subtotal" style="min-width:120px">
                    <template #body="{ data }">
                        <span class="font-semibold" :class="data.billable ? '' : 'opacity-50'">{{ fmtCurrency(data.subtotal) }}</span>
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
                No expense rows. Click "Add Row" to begin.
            </div>
        </template>
    </Card>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import Card from 'primevue/card';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import type { ExpenseRow } from '../../../stores/estimateStore';

const props = defineProps<{ rows: ExpenseRow[]; defaultDays?: number }>();
const emits = defineEmits<{ (e: 'update:rows', v: ExpenseRow[]): void; (e: 'change'): void }>();

const categoryOptions = ['PerDiem', 'Travel', 'Lodging', 'Consumables', 'MobDemob', 'Other'];
const unitOptions = ['Day', 'Mile', 'Trip', 'EA', 'Month'];

function recalc(idx: number) {
    const r = props.rows[idx];
    r.subtotal = Math.round(r.rate * r.daysOrQty * r.people * 100) / 100;
    emits('change');
    emitRows();
}

function onChange(idx: number) {
    emits('change');
    emitRows();
}

function addRow() {
    const newRow: ExpenseRow = {
        category: 'PerDiem',
        description: '',
        rate: 0,
        unit: 'Day',
        daysOrQty: props.defaultDays || 1,
        people: 1,
        billable: true,
        subtotal: 0,
    };
    emits('update:rows', [...props.rows, newRow]);
    emits('change');
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
