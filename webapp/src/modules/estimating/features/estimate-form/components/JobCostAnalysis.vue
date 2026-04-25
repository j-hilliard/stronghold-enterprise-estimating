<template>
    <div class="job-cost-analysis">
        <!-- Internal Only Banner -->
        <div class="internal-banner">
            <i class="pi pi-lock" />
            INTERNAL USE ONLY — NOT FOR DISTRIBUTION
        </div>

        <div v-if="loading" class="flex justify-content-center py-6">
            <ProgressSpinner style="width:32px;height:32px" />
        </div>

        <div v-else-if="!costBook" class="no-cost-book">
            <i class="pi pi-exclamation-triangle text-yellow-400 text-2xl" />
            <div>
                <div class="font-semibold">No cost book found for this company.</div>
                <div class="text-sm text-slate-400 mt-1">Go to Cost Book settings and click "Reset to Standard" to seed the default rates.</div>
            </div>
        </div>

        <template v-else>
            <!-- Burden Summary -->
            <div class="burden-bar">
                <span class="burden-label">Labor Burden Applied:</span>
                <span v-for="item in burdenItems" :key="item.code" class="burden-chip">
                    {{ item.name }}
                    <span class="burden-val">{{ item.burdenType === 'dollar_per_hour' ? `$${item.value}/hr` : `${item.value}%` }}</span>
                </span>
                <span class="burden-total-chip">
                    Total: {{ fmtPct(burdenPctSum) }}% + ${{ burdenDollarSum.toFixed(2) }}/hr
                </span>
            </div>

            <!-- Per-Row Cost Table -->
            <div class="cost-table-scroll">
                <table class="cost-table w-full">
                    <thead>
                        <tr>
                            <th class="col-position">Position</th>
                            <th class="col-type">Type</th>
                            <th class="col-hours">ST Hrs</th>
                            <th class="col-hours">OT Hrs</th>
                            <th class="col-hours">DT Hrs</th>
                            <th class="col-rate">Cost ST</th>
                            <th class="col-rate">Cost OT</th>
                            <th class="col-rate">Cost DT</th>
                            <th class="col-rate">Burdened ST</th>
                            <th class="col-rate">Burdened OT</th>
                            <th class="col-rate">Burdened DT</th>
                            <th class="col-subtotal">Internal Cost</th>
                            <th class="col-match"></th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="(row, idx) in costRows" :key="idx" :class="{ 'row-unmatched': !row.matched }">
                            <td class="col-position font-medium">{{ row.position || '—' }}</td>
                            <td class="col-type text-slate-400">{{ row.laborType }}</td>
                            <td class="col-hours text-right">{{ fmt(row.stHours) }}</td>
                            <td class="col-hours text-right">{{ fmt(row.otHours) }}</td>
                            <td class="col-hours text-right">{{ fmt(row.dtHours) }}</td>
                            <td class="col-rate text-right">{{ row.matched ? fmtRate(row.costSt) : '—' }}</td>
                            <td class="col-rate text-right">{{ row.matched ? fmtRate(row.costOt) : '—' }}</td>
                            <td class="col-rate text-right">{{ row.matched ? fmtRate(row.costDt) : '—' }}</td>
                            <td class="col-rate text-right font-medium">{{ row.matched ? fmtRate(row.burdenedSt) : '—' }}</td>
                            <td class="col-rate text-right font-medium">{{ row.matched ? fmtRate(row.burdenedOt) : '—' }}</td>
                            <td class="col-rate text-right font-medium">{{ row.matched ? fmtRate(row.burdenedDt) : '—' }}</td>
                            <td class="col-subtotal text-right font-semibold">{{ row.matched ? fmtCurrency(row.internalCost) : '—' }}</td>
                            <td class="col-match">
                                <i v-if="!row.matched" class="pi pi-exclamation-triangle text-yellow-400" v-tooltip="'No cost book rate found for this position'" />
                                <i v-else class="pi pi-check text-green-500 opacity-50" />
                            </td>
                        </tr>
                    </tbody>
                    <tfoot v-if="costRows.length > 0">
                        <tr>
                            <td :colspan="11" class="text-right font-semibold pr-3 text-sm">Total Internal Labor Cost</td>
                            <td class="col-subtotal text-right font-bold text-primary">{{ fmtCurrency(totalInternalCost) }}</td>
                            <td></td>
                        </tr>
                    </tfoot>
                </table>
            </div>

            <div v-if="costRows.length === 0" class="text-center py-6 text-slate-400">
                No labor rows. Add labor rows above to see internal cost analysis.
            </div>

            <!-- Unmatched warning -->
            <div v-if="unmatchedCount > 0" class="unmatched-warning">
                <i class="pi pi-exclamation-triangle" />
                {{ unmatchedCount }} row{{ unmatchedCount > 1 ? 's' : '' }} have no matching cost book rate — internal cost is understated.
                Add rates for these positions in the Cost Book settings.
            </div>

            <!-- Formula note -->
            <div class="formula-note">
                Burdened rate = base × (1 + {{ fmtPct(burdenPctSum) }}%) + ${{ burdenDollarSum.toFixed(2) }}/hr
            </div>
        </template>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { useApiStore } from '@/stores/apiStore';
import type { LaborRow } from '../../../stores/estimateStore';

const props = defineProps<{
    laborRows: LaborRow[];
    hoursPerShift: number;
}>();

const emits = defineEmits<{
    (e: 'update:internalCost', v: number): void;
}>();

const apiStore = useApiStore();

// ── Cost book data ───────────────────────────────────────────────────────────

interface CostBookLaborRate {
    position: string;
    laborType: string;
    stRate: number;
    otRate: number;
    dtRate: number;
}

interface OverheadItem {
    category: string;
    code: string;
    name: string;
    burdenType: string;  // 'percentage' | 'dollar_per_hour'
    value: number;
}

interface CostBook {
    costBookId: number;
    name: string;
    laborRates: CostBookLaborRate[];
    overheadItems: OverheadItem[];
}

const costBook = ref<CostBook | null>(null);
const loading = ref(false);

onMounted(async () => {
    loading.value = true;
    try {
        const { data: list } = await apiStore.api.get('/api/v1/cost-books');
        const def = (list as any[]).find(b => b.isDefault) ?? list[0];
        if (!def) return;
        const { data } = await apiStore.api.get(`/api/v1/cost-books/${def.costBookId}`);
        costBook.value = data;
    } catch {
        // no cost book — show warning
    } finally {
        loading.value = false;
    }
});

// ── Burden calculations ───────────────────────────────────────────────────────

const burdenItems = computed<OverheadItem[]>(() =>
    costBook.value?.overheadItems.filter(i => i.category === 'Burden' || !i.category) ?? []
);

const burdenPctSum = computed(() =>
    burdenItems.value
        .filter(i => i.burdenType === 'percentage')
        .reduce((s, i) => s + i.value, 0)
);

const burdenDollarSum = computed(() =>
    burdenItems.value
        .filter(i => i.burdenType === 'dollar_per_hour')
        .reduce((s, i) => s + i.value, 0)
);

function applyBurden(base: number): number {
    return base * (1 + burdenPctSum.value / 100) + burdenDollarSum.value;
}

// ── Per-row cost analysis ────────────────────────────────────────────────────

interface CostRow {
    position: string;
    laborType: string;
    stHours: number;
    otHours: number;
    dtHours: number;
    matched: boolean;
    costSt: number;
    costOt: number;
    costDt: number;
    burdenedSt: number;
    burdenedOt: number;
    burdenedDt: number;
    internalCost: number;
}

const costRows = computed<CostRow[]>(() => {
    if (!costBook.value) return [];

    return props.laborRows.map(row => {
        const posLower = (row.position ?? '').trim().toLowerCase();
        const match = costBook.value!.laborRates.find(
            r => r.position.trim().toLowerCase() === posLower
        );

        if (!match) {
            return {
                position: row.position,
                laborType: row.laborType,
                stHours: row.stHours,
                otHours: row.otHours,
                dtHours: row.dtHours,
                matched: false,
                costSt: 0, costOt: 0, costDt: 0,
                burdenedSt: 0, burdenedOt: 0, burdenedDt: 0,
                internalCost: 0,
            };
        }

        const burdenedSt = applyBurden(match.stRate);
        const burdenedOt = applyBurden(match.otRate);
        const burdenedDt = applyBurden(match.dtRate);
        const internalCost = Math.round(
            (row.stHours * burdenedSt + row.otHours * burdenedOt + row.dtHours * burdenedDt) * 100
        ) / 100;

        return {
            position: row.position,
            laborType: row.laborType,
            stHours: row.stHours,
            otHours: row.otHours,
            dtHours: row.dtHours,
            matched: true,
            costSt: match.stRate,
            costOt: match.otRate,
            costDt: match.dtRate,
            burdenedSt: Math.round(burdenedSt * 100) / 100,
            burdenedOt: Math.round(burdenedOt * 100) / 100,
            burdenedDt: Math.round(burdenedDt * 100) / 100,
            internalCost,
        };
    });
});

const totalInternalCost = computed(() =>
    Math.round(costRows.value.reduce((s, r) => s + r.internalCost, 0) * 100) / 100
);

const unmatchedCount = computed(() =>
    costRows.value.filter(r => !r.matched && (r.stHours + r.otHours + r.dtHours) > 0).length
);

// Emit whenever total changes
watch(totalInternalCost, val => emits('update:internalCost', val), { immediate: true });

// ── Formatting ──────────────────────────────────────────────────────────────

function fmt(n: number): string { return n.toFixed(1); }
function fmtRate(n: number): string { return `$${n.toFixed(2)}`; }
function fmtPct(n: number): string { return n.toFixed(2); }
function fmtCurrency(n: number): string {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(n ?? 0);
}
</script>

<style scoped>
.job-cost-analysis {
    display: flex;
    flex-direction: column;
    gap: 0;
}

.internal-banner {
    display: flex;
    align-items: center;
    gap: 8px;
    background: color-mix(in srgb, var(--red-500) 12%, transparent);
    border-bottom: 1px solid color-mix(in srgb, var(--red-500) 30%, transparent);
    color: var(--red-400);
    font-size: 0.7rem;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    padding: 6px 14px;
}

.no-cost-book {
    display: flex;
    align-items: center;
    gap: 16px;
    padding: 20px 16px;
    background: color-mix(in srgb, var(--yellow-500) 8%, transparent);
}

.burden-bar {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 6px;
    padding: 8px 14px;
    background: var(--surface-ground);
    border-bottom: 1px solid var(--surface-border);
    font-size: 0.72rem;
}

.burden-label {
    font-weight: 600;
    color: var(--text-color-secondary);
    text-transform: uppercase;
    letter-spacing: 0.05em;
    margin-right: 4px;
}

.burden-chip {
    display: inline-flex;
    align-items: center;
    gap: 3px;
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 10px;
    padding: 1px 7px;
    color: var(--text-color-secondary);
}

.burden-val {
    color: var(--primary-color);
    font-weight: 600;
}

.burden-total-chip {
    display: inline-flex;
    align-items: center;
    background: color-mix(in srgb, var(--primary-color) 12%, transparent);
    border: 1px solid color-mix(in srgb, var(--primary-color) 30%, transparent);
    border-radius: 10px;
    padding: 1px 8px;
    color: var(--primary-color);
    font-weight: 700;
    margin-left: 4px;
}

.cost-table-scroll {
    overflow-x: auto;
}

.cost-table {
    border-collapse: collapse;
    font-size: 0.8rem;
    min-width: 900px;
}

.cost-table th,
.cost-table td {
    padding: 4px 6px;
    border-bottom: 1px solid var(--surface-border);
    white-space: nowrap;
    vertical-align: middle;
}

.cost-table thead th {
    background: var(--surface-100);
    font-weight: 600;
    font-size: 0.72rem;
    text-align: center;
    position: sticky;
    top: 0;
    z-index: 1;
    color: var(--text-color-secondary);
}

.cost-table tfoot td {
    background: var(--surface-50);
    border-top: 2px solid var(--surface-border);
    padding-top: 6px;
}

.col-position { min-width: 140px; }
.col-type     { min-width: 80px; color: var(--text-color-secondary); }
.col-hours    { min-width: 64px; text-align: right; }
.col-rate     { min-width: 80px; text-align: right; font-family: monospace; font-size: 0.77rem; }
.col-subtotal { min-width: 110px; text-align: right; }
.col-match    { width: 28px; text-align: center; }

.row-unmatched td {
    opacity: 0.6;
}

.unmatched-warning {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 8px 14px;
    background: color-mix(in srgb, var(--yellow-500) 8%, transparent);
    border-top: 1px solid color-mix(in srgb, var(--yellow-500) 25%, transparent);
    color: var(--yellow-500);
    font-size: 0.78rem;
}

.formula-note {
    padding: 6px 14px;
    font-size: 0.7rem;
    color: var(--text-color-secondary);
    font-style: italic;
    border-top: 1px solid var(--surface-border);
    background: var(--surface-ground);
}
</style>
