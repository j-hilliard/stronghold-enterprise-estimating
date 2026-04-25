<template>
    <Card class="estimate-summary-card">
        <template #title>
            <div class="flex align-items-center gap-2">
                <i class="pi pi-calculator text-primary" />
                <span>Summary</span>
            </div>
        </template>
        <template #content>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                <!-- Left: inputs -->
                <div class="flex flex-col gap-4">
                    <div class="flex align-items-center justify-content-between">
                        <span class="text-slate-400">Bill Subtotal</span>
                        <span class="font-semibold text-lg">{{ fmtCurrency(modelValue.billSubtotal) }}</span>
                    </div>

                    <div class="flex align-items-center gap-3">
                        <span class="text-slate-400 w-28">Discount</span>
                        <Dropdown v-model="form.discountType" :options="discountTypeOptions" optionLabel="label" optionValue="value" class="w-28" @change="recalc" />
                        <InputNumber
                            v-model="form.discountValue"
                            :minFractionDigits="2"
                            :prefix="form.discountType === 'dollar' ? '$' : ''"
                            :suffix="form.discountType === 'percent' ? '%' : ''"
                            class="flex-1"
                            @input="recalc"
                        />
                    </div>

                    <div class="flex align-items-center gap-3">
                        <span class="text-slate-400 w-28">Tax Rate</span>
                        <InputNumber v-model="form.taxRate" :minFractionDigits="2" suffix="%" class="flex-1" @input="recalc" />
                    </div>
                </div>

                <!-- Right: totals -->
                <div class="flex flex-col gap-3">
                    <div v-if="modelValue.discountAmount > 0" class="flex justify-content-between">
                        <span class="text-slate-400">Discount</span>
                        <span class="text-red-400">- {{ fmtCurrency(modelValue.discountAmount) }}</span>
                    </div>
                    <div v-if="modelValue.taxAmount > 0" class="flex justify-content-between">
                        <span class="text-slate-400">Tax ({{ modelValue.taxRate }}%)</span>
                        <span>{{ fmtCurrency(modelValue.taxAmount) }}</span>
                    </div>
                    <Divider />
                    <div class="flex justify-content-between text-xl font-bold">
                        <span>Grand Total</span>
                        <span class="text-primary">{{ fmtCurrency(modelValue.grandTotal) }}</span>
                    </div>
                    <Divider />
                    <div class="flex justify-content-between text-sm">
                        <span class="text-slate-400">Internal Cost</span>
                        <span>{{ fmtCurrency(modelValue.internalCostTotal) }}</span>
                    </div>
                    <div class="flex justify-content-between text-sm">
                        <span class="text-slate-400">Gross Profit</span>
                        <span>{{ fmtCurrency(modelValue.grossProfit) }}</span>
                    </div>
                    <div class="flex justify-content-between align-items-center">
                        <span class="text-slate-400">Gross Margin</span>
                        <Tag
                            :value="`${modelValue.grossMarginPct.toFixed(1)}%`"
                            :severity="marginSeverity"
                            class="text-base font-bold px-3 py-1"
                        />
                    </div>
                </div>
            </div>
        </template>
    </Card>
</template>

<script setup lang="ts">
import { reactive, watch, computed } from 'vue';
import Card from 'primevue/card';
import Divider from 'primevue/divider';
import type { EstimateSummary } from '../../../stores/estimateStore';

const props = defineProps<{ modelValue: EstimateSummary }>();
const emits = defineEmits<{ (e: 'update:modelValue', v: EstimateSummary): void }>();

const form = reactive({
    discountType: props.modelValue.discountType || 'percent',
    discountValue: props.modelValue.discountValue || 0,
    taxRate: props.modelValue.taxRate || 0,
});

watch(() => props.modelValue, v => {
    form.discountType = v.discountType;
    form.discountValue = v.discountValue;
    form.taxRate = v.taxRate;
}, { deep: true });

function recalc() {
    const sub = props.modelValue.billSubtotal;
    const disc = form.discountType === 'percent'
        ? Math.round(sub * form.discountValue) / 100
        : form.discountValue;
    const taxable = sub - disc;
    const tax = Math.round(taxable * form.taxRate) / 100;
    const grand = taxable + tax;
    const profit = grand - props.modelValue.internalCostTotal;

    emits('update:modelValue', {
        ...props.modelValue,
        discountType: form.discountType,
        discountValue: form.discountValue,
        discountAmount: Math.round(disc * 100) / 100,
        taxRate: form.taxRate,
        taxAmount: Math.round(tax * 100) / 100,
        grandTotal: Math.round(grand * 100) / 100,
        grossProfit: Math.round(profit * 100) / 100,
        grossMarginPct: grand > 0 ? Math.round((profit / grand) * 10000) / 100 : 0,
    });
}

const marginSeverity = computed(() => {
    const pct = props.modelValue.grossMarginPct;
    if (pct >= 25) return 'success';
    if (pct >= 15) return 'warning';
    return 'danger';
});

const discountTypeOptions = [
    { label: '%', value: 'percent' },
    { label: '$', value: 'dollar' },
];

function fmtCurrency(n: number): string {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(n ?? 0);
}
</script>
