<template>
    <Card class="equipment-section-card">
        <template #title>
            <div class="flex align-items-center justify-content-between gap-2">
                <div class="flex align-items-center gap-2">
                    <i class="pi pi-wrench text-primary" />
                    <span>Equipment</span>
                    <Tag :value="`${rows.length} row${rows.length !== 1 ? 's' : ''}`" severity="secondary" />
                </div>
                <Button label="Add Row" icon="pi pi-plus" size="small" outlined @click="addRow" />
            </div>
        </template>
        <template #content>
            <DataTable :value="rows" class="equipment-table" size="small" :scrollable="true" scrollHeight="400px">
                <Column header="Name" style="min-width:200px">
                    <template #body="{ data, index }">
                        <InputText v-model="data.name" placeholder="Equipment name" class="w-full" @input="onChange(index)" />
                    </template>
                </Column>
                <Column header="Rate Type" style="min-width:130px">
                    <template #body="{ data, index }">
                        <Dropdown v-model="data.rateType" :options="rateTypeOptions" class="w-full" @change="recalc(index)" />
                    </template>
                </Column>
                <Column header="Rate" style="min-width:120px">
                    <template #body="{ data, index }">
                        <InputNumber v-model="data.rate" mode="currency" currency="USD" :minFractionDigits="2" class="w-full" @input="recalc(index)" />
                    </template>
                </Column>
                <Column header="Qty" style="min-width:80px">
                    <template #body="{ data, index }">
                        <InputNumber v-model="data.qty" :min="1" class="w-full" @input="recalc(index)" />
                    </template>
                </Column>
                <Column header="Days" style="min-width:80px">
                    <template #body="{ data, index }">
                        <InputNumber v-model="data.days" :min="1" class="w-full" @input="recalc(index)" />
                    </template>
                </Column>
                <Column header="Subtotal" style="min-width:120px">
                    <template #body="{ data }">
                        <span class="font-semibold">{{ fmtCurrency(data.subtotal) }}</span>
                    </template>
                </Column>
                <Column style="width:48px">
                    <template #body="{ index }">
                        <Button icon="pi pi-trash" text rounded size="small" severity="danger" @click="removeRow(index)" />
                    </template>
                </Column>
                <template #footer>
                    <div class="text-right font-bold text-primary pr-2">
                        Total: {{ fmtCurrency(total) }}
                    </div>
                </template>
            </DataTable>
            <div v-if="rows.length === 0" class="text-center py-4 text-slate-400">
                No equipment rows. Click "Add Row" to begin.
            </div>
        </template>
    </Card>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import Card from 'primevue/card';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import type { EquipmentRow } from '../../../stores/estimateStore';

const props = defineProps<{ rows: EquipmentRow[]; defaultDays?: number }>();
const emits = defineEmits<{ (e: 'update:rows', v: EquipmentRow[]): void; (e: 'change'): void }>();

const rateTypeOptions = ['Daily', 'Weekly', 'Monthly', 'Hourly'];

function recalc(idx: number) {
    const r = props.rows[idx];
    r.subtotal = Math.round(r.rate * r.qty * r.days * 100) / 100;
    emits('change');
    emitRows();
}

function onChange(idx: number) {
    emits('change');
    emitRows();
}

function addRow() {
    const newRow: EquipmentRow = {
        name: '',
        rateType: 'Daily',
        rate: 0,
        qty: 1,
        days: props.defaultDays || 1,
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

const total = computed(() => props.rows.reduce((s, r) => s + r.subtotal, 0));

function fmtCurrency(n: number): string {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(n ?? 0);
}
</script>
