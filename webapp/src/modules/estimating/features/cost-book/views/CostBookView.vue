<template>
    <div class="cost-book-view">
        <BasePageHeader icon="pi pi-chart-bar" title="Cost Book" subtitle="Internal cost rates and labor burden — internal use only">
            <div class="flex gap-2 align-items-center">
                <span class="internal-hdr-badge">
                    <i class="pi pi-lock" />
                    INTERNAL USE ONLY
                </span>
                <Dropdown
                    v-model="selectedBookId"
                    :options="bookList"
                    optionLabel="name"
                    optionValue="costBookId"
                    placeholder="Select Cost Book"
                    style="min-width:220px"
                    @change="loadBook"
                />
                <Button
                    label="Save All"
                    icon="pi pi-save"
                    :loading="saving"
                    :disabled="!dirty"
                    @click="saveAll"
                />
                <Button
                    label="Reset Defaults"
                    icon="pi pi-refresh"
                    outlined
                    severity="secondary"
                    @click="confirmReset"
                />
            </div>
        </BasePageHeader>

        <div v-if="loading" class="flex justify-content-center py-8">
            <ProgressSpinner />
        </div>

        <div v-else-if="apiError" class="no-book-msg error-msg">
            <i class="pi pi-times-circle text-red-400 text-2xl" />
            <div>
                <div class="font-semibold">Cannot reach the API ({{ apiError }})</div>
                <div class="text-sm text-slate-400 mt-1">
                    Make sure the .NET API is running at <code>https://localhost:7211</code>.
                    Run <code>start-stronghold.ps1</code> to start all services.
                </div>
            </div>
        </div>

        <div v-else-if="!costBook" class="no-book-msg">
            <i class="pi pi-exclamation-triangle text-yellow-400 text-2xl" />
            <div>
                <div class="font-semibold">No cost book found for this company.</div>
                <div class="text-sm text-slate-400 mt-1">Click "Reset Defaults" to seed the standard cost book.</div>
            </div>
        </div>

        <template v-else>
            <!-- NeedsReview banner -->
            <div v-if="reviewCount > 0" class="review-banner">
                <i class="pi pi-exclamation-triangle" />
                <span><strong>{{ reviewCount }} position{{ reviewCount > 1 ? 's' : '' }} need cost rates set.</strong>
                These were auto-added from a rate book at 100% of bill rate — you are currently making $0 margin on them.
                Click the pencil icon on each flagged row to set your actual pay rate.</span>
            </div>

            <!-- Stats bar -->
            <div class="stats-bar">
                <div class="stat-chip">
                    <span class="stat-label">Labor Positions</span>
                    <span class="stat-val">{{ costBook.laborRates.length }}</span>
                </div>
                <div class="stat-chip">
                    <span class="stat-label">Equipment Items</span>
                    <span class="stat-val">{{ costBook.equipmentRates.length }}</span>
                </div>
                <div class="stat-chip">
                    <span class="stat-label">Expense Types</span>
                    <span class="stat-val">{{ costBook.expenses.length }}</span>
                </div>
                <div class="stat-chip">
                    <span class="stat-label">Total Burden</span>
                    <span class="stat-val primary">{{ totalBurdenDisplay }}</span>
                </div>
                <div class="stat-chip">
                    <span class="stat-label">Last Updated</span>
                    <span class="stat-val">{{ fmtDate(costBook.updatedAt) }}</span>
                </div>
                <div v-if="dirty" class="stat-chip dirty-chip">
                    <i class="pi pi-circle-fill" style="font-size:0.5rem" />
                    Unsaved changes
                </div>
            </div>

            <!-- ── OVERHEAD & BURDEN RATES ─────────────────────────────────── -->
            <div class="cb-section">
                <div class="cb-section-header" @click="collapsed.burden = !collapsed.burden">
                    <i class="pi pi-chevron-down cb-toggle" :class="{ rotated: collapsed.burden }" />
                    <i class="pi pi-percentage cb-icon" />
                    <span class="cb-section-title">OVERHEAD &amp; BURDEN RATES</span>
                    <span class="cb-section-badge">{{ costBook.overheadItems.length }} items</span>
                </div>
                <div v-show="!collapsed.burden" class="cb-section-body">
                    <div class="burden-grid">
                        <div v-for="group in burdenGroups" :key="group.title" class="burden-col">
                            <div class="burden-col-title">{{ group.title }}</div>
                            <div v-for="item in group.items" :key="item.code" class="burden-row">
                                <span class="burden-name">{{ item.name }}</span>
                                <div class="burden-input-row">
                                    <InputNumber
                                        v-model="item.value"
                                        mode="decimal"
                                        :minFractionDigits="2"
                                        :maxFractionDigits="4"
                                        class="burden-input"
                                        @input="markDirty"
                                    />
                                    <button
                                        class="type-toggle-btn"
                                        :class="item.burdenType === 'dollar_per_hour' ? 'type-dollar' : 'type-pct'"
                                        type="button"
                                        @click="toggleBurdenType(item)"
                                    >
                                        {{ item.burdenType === 'dollar_per_hour' ? '$/hr' : '%' }}
                                    </button>
                                </div>
                            </div>
                            <div v-if="group.items.length === 0" class="burden-empty">No items</div>
                            <div class="burden-col-subtotal">{{ groupSubtotalLabel(group.items) }}</div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- ── LABOR COSTS ────────────────────────────────────────────── -->
            <div class="cb-section">
                <div class="cb-section-header" @click="collapsed.labor = !collapsed.labor">
                    <i class="pi pi-chevron-down cb-toggle" :class="{ rotated: collapsed.labor }" />
                    <i class="pi pi-users cb-icon" />
                    <span class="cb-section-title">LABOR COSTS (WHAT WE PAY)</span>
                    <span class="cb-section-badge">{{ costBook.laborRates.length }} positions</span>
                </div>
                <div v-show="!collapsed.labor" class="cb-section-body">
                    <div class="table-scroll">
                        <table class="cb-table">
                            <thead>
                                <tr>
                                    <th>NAV CODE</th>
                                    <th>CRAFT CODE</th>
                                    <th>POSITION</th>
                                    <th>TYPE</th>
                                    <th class="text-right">ST RATE</th>
                                    <th class="text-right">OT RATE (1.5x)</th>
                                    <th class="text-right">DT RATE (2x)</th>
                                    <th class="text-right">BURDENED ST</th>
                                    <th class="act-col"></th>
                                </tr>
                            </thead>
                            <tbody>
                                <template v-for="ltype in ['Direct', 'Indirect']" :key="ltype">
                                    <tr class="group-header-row">
                                        <td colspan="9">{{ ltype }} Labor</td>
                                    </tr>
                                    <tr
                                        v-for="(row, idx) in laborByType(ltype)"
                                        :key="idx"
                                        class="data-row"
                                        :class="{ 'needs-review-row': row.needsReview }"
                                    >
                                        <td class="code-cell">{{ row.navCode || '—' }}</td>
                                        <td class="code-cell">{{ row.craftCode || '—' }}</td>
                                        <td class="font-medium">
                                            {{ row.position }}
                                            <span v-if="row.needsReview" class="review-flag" title="Cost rate was auto-copied from rate book (100% of bill rate = 0% margin). Edit to set your actual pay rate.">⚠ SET COST RATE</span>
                                        </td>
                                        <td>
                                            <Tag
                                                :value="row.laborType"
                                                :severity="row.laborType === 'Direct' ? 'success' : 'info'"
                                                style="font-size:0.68rem"
                                            />
                                        </td>
                                        <td class="text-right rate-cell">{{ fmtRate(row.stRate) }}</td>
                                        <td class="text-right rate-cell">{{ fmtRate(row.otRate) }}</td>
                                        <td class="text-right rate-cell">{{ fmtRate(row.dtRate) }}</td>
                                        <td class="text-right rate-cell font-semibold" style="color:var(--primary-color)">
                                            {{ fmtRate(burdenedSt(row.stRate)) }}
                                        </td>
                                        <td class="act-col">
                                            <Button icon="pi pi-pencil" text size="small" @click="editLabor(row)" />
                                            <Button icon="pi pi-trash" text size="small" severity="danger" @click="deleteLabor(row)" />
                                        </td>
                                    </tr>
                                    <tr v-if="laborByType(ltype).length === 0" class="empty-group-row">
                                        <td colspan="9">No {{ ltype.toLowerCase() }} labor positions — click Add Position below</td>
                                    </tr>
                                </template>
                            </tbody>
                        </table>
                    </div>
                    <div class="add-row-bar">
                        <Button label="Add Position" icon="pi pi-plus" text size="small" @click="addLabor" />
                    </div>
                </div>
            </div>

            <!-- ── EQUIPMENT COSTS ───────────────────────────────────────── -->
            <div class="cb-section">
                <div class="cb-section-header" @click="collapsed.equipment = !collapsed.equipment">
                    <i class="pi pi-chevron-down cb-toggle" :class="{ rotated: collapsed.equipment }" />
                    <i class="pi pi-wrench cb-icon" />
                    <span class="cb-section-title">EQUIPMENT COSTS (WHAT WE PAY)</span>
                    <span class="cb-section-badge">{{ costBook.equipmentRates.length }} items</span>
                </div>
                <div v-show="!collapsed.equipment" class="cb-section-body">
                    <div class="table-scroll">
                        <table class="cb-table">
                            <thead>
                                <tr>
                                    <th>EQUIPMENT</th>
                                    <th class="text-right">HOURLY</th>
                                    <th class="text-right">DAILY</th>
                                    <th class="text-right">WEEKLY</th>
                                    <th class="text-right">MONTHLY</th>
                                    <th class="act-col"></th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr v-for="(row, idx) in costBook.equipmentRates" :key="idx" class="data-row">
                                    <td class="font-medium">{{ row.name }}</td>
                                    <td class="text-right rate-cell">{{ row.hourly != null ? fmtRate(row.hourly) : '—' }}</td>
                                    <td class="text-right rate-cell">{{ row.daily != null ? fmtRate(row.daily) : '—' }}</td>
                                    <td class="text-right rate-cell">{{ row.weekly != null ? fmtRate(row.weekly) : '—' }}</td>
                                    <td class="text-right rate-cell">{{ row.monthly != null ? fmtRate(row.monthly) : '—' }}</td>
                                    <td class="act-col">
                                        <Button icon="pi pi-pencil" text size="small" @click="editEquipment(row)" />
                                        <Button icon="pi pi-trash" text size="small" severity="danger" @click="deleteEquipment(row)" />
                                    </td>
                                </tr>
                                <tr v-if="costBook.equipmentRates.length === 0">
                                    <td colspan="6" class="empty-group-row">No equipment rates — click Add Equipment below</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                    <div class="add-row-bar">
                        <Button label="Add Equipment" icon="pi pi-plus" text size="small" @click="addEquipment" />
                    </div>
                </div>
            </div>

            <!-- ── EXPENSES (PER DIEM, TRAVEL, LODGING) ──────────────────── -->
            <div class="cb-section">
                <div class="cb-section-header" @click="collapsed.expenses = !collapsed.expenses">
                    <i class="pi pi-chevron-down cb-toggle" :class="{ rotated: collapsed.expenses }" />
                    <i class="pi pi-receipt cb-icon" />
                    <span class="cb-section-title">EXPENSES (PER DIEM, TRAVEL, LODGING)</span>
                    <span class="cb-section-badge">{{ costBook.expenses.length }} items</span>
                </div>
                <div v-show="!collapsed.expenses" class="cb-section-body">
                    <div class="expense-panels">

                        <!-- PER DIEM -->
                        <div class="expense-panel">
                            <div class="expense-panel-hdr">
                                <span class="expense-panel-title">PER DIEM</span>
                                <Button label="+ Add" size="small" class="expense-add-btn" @click="addExpense('PerDiem')" />
                            </div>
                            <table class="expense-table">
                                <thead>
                                    <tr>
                                        <th>DESCRIPTION</th>
                                        <th class="text-right">DAILY RATE</th>
                                        <th class="act-col"></th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr v-for="row in expensesByCategory('PerDiem')" :key="row.description" class="data-row">
                                        <td>{{ row.description }}</td>
                                        <td class="text-right rate-cell">{{ fmtRate(row.rate) }}/day</td>
                                        <td class="act-col">
                                            <Button icon="pi pi-pencil" text size="small" @click="editExpense(row)" />
                                            <Button icon="pi pi-trash" text size="small" severity="danger" @click="deleteExpense(row)" />
                                        </td>
                                    </tr>
                                    <tr v-if="expensesByCategory('PerDiem').length === 0">
                                        <td colspan="3" class="empty-group-row">None</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>

                        <!-- TRAVEL -->
                        <div class="expense-panel">
                            <div class="expense-panel-hdr">
                                <span class="expense-panel-title">TRAVEL</span>
                                <Button label="+ Add" size="small" class="expense-add-btn" @click="addExpense('Travel')" />
                            </div>
                            <table class="expense-table">
                                <thead>
                                    <tr>
                                        <th>DESCRIPTION</th>
                                        <th class="text-right">RATE</th>
                                        <th>PER</th>
                                        <th class="act-col"></th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr v-for="row in expensesByCategory('Travel')" :key="row.description" class="data-row">
                                        <td>{{ row.description }}</td>
                                        <td class="text-right rate-cell">{{ fmtRate(row.rate) }}</td>
                                        <td class="unit-cell">{{ row.unit }}</td>
                                        <td class="act-col">
                                            <Button icon="pi pi-pencil" text size="small" @click="editExpense(row)" />
                                            <Button icon="pi pi-trash" text size="small" severity="danger" @click="deleteExpense(row)" />
                                        </td>
                                    </tr>
                                    <tr v-if="expensesByCategory('Travel').length === 0">
                                        <td colspan="4" class="empty-group-row">None</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>

                        <!-- LODGING -->
                        <div class="expense-panel">
                            <div class="expense-panel-hdr">
                                <span class="expense-panel-title">LODGING</span>
                                <Button label="+ Add" size="small" class="expense-add-btn" @click="addExpense('Lodging')" />
                            </div>
                            <table class="expense-table">
                                <thead>
                                    <tr>
                                        <th>DESCRIPTION</th>
                                        <th class="text-right">NIGHTLY RATE</th>
                                        <th class="act-col"></th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr v-for="row in expensesByCategory('Lodging')" :key="row.description" class="data-row">
                                        <td>{{ row.description }}</td>
                                        <td class="text-right rate-cell">{{ fmtRate(row.rate) }}/night</td>
                                        <td class="act-col">
                                            <Button icon="pi pi-pencil" text size="small" @click="editExpense(row)" />
                                            <Button icon="pi pi-trash" text size="small" severity="danger" @click="deleteExpense(row)" />
                                        </td>
                                    </tr>
                                    <tr v-if="expensesByCategory('Lodging').length === 0">
                                        <td colspan="3" class="empty-group-row">None</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>

                    </div>
                </div>
            </div>
        </template>

        <!-- ── Labor Dialog ─────────────────────────────────────────────── -->
        <Dialog v-model:visible="laborDlg.visible" :header="laborDlg.isNew ? 'Add Position' : 'Edit Position'" modal style="width:500px">
            <div class="flex flex-col gap-3 pt-2">
                <div class="dlg-field">
                    <label class="field-label">Position *</label>
                    <InputText v-model="laborDlg.data.position" class="w-full" placeholder="e.g. Pipefitter Foreman" />
                </div>
                <div class="dlg-field">
                    <label class="field-label">Labor Type *</label>
                    <Dropdown v-model="laborDlg.data.laborType" :options="['Direct', 'Indirect']" class="w-full" />
                </div>
                <div class="flex gap-3">
                    <div class="dlg-field flex-1">
                        <label class="field-label">NAV Code</label>
                        <InputText v-model="laborDlg.data.navCode" class="w-full" />
                    </div>
                    <div class="dlg-field flex-1">
                        <label class="field-label">Craft Code</label>
                        <InputText v-model="laborDlg.data.craftCode" class="w-full" />
                    </div>
                </div>
                <div class="flex gap-3">
                    <div class="dlg-field flex-1">
                        <label class="field-label">ST Rate ($/hr)</label>
                        <InputNumber v-model="laborDlg.data.stRate" mode="decimal" :minFractionDigits="2" :maxFractionDigits="2" class="w-full" />
                    </div>
                    <div class="dlg-field flex-1">
                        <label class="field-label">OT Rate ($/hr)</label>
                        <InputNumber v-model="laborDlg.data.otRate" mode="decimal" :minFractionDigits="2" :maxFractionDigits="2" class="w-full" />
                    </div>
                    <div class="dlg-field flex-1">
                        <label class="field-label">DT Rate ($/hr)</label>
                        <InputNumber v-model="laborDlg.data.dtRate" mode="decimal" :minFractionDigits="2" :maxFractionDigits="2" class="w-full" />
                    </div>
                </div>
                <div v-if="laborDlg.data.stRate" class="dlg-burdened-preview">
                    Burdened ST preview: <strong>{{ fmtRate(burdenedSt(laborDlg.data.stRate)) }}</strong>
                </div>
            </div>
            <template #footer>
                <Button label="Cancel" text @click="laborDlg.visible = false" />
                <Button label="Apply" icon="pi pi-check" @click="applyLabor" :disabled="!laborDlg.data.position.trim()" />
            </template>
        </Dialog>

        <!-- ── Equipment Dialog ─────────────────────────────────────────── -->
        <Dialog v-model:visible="equipDlg.visible" :header="equipDlg.isNew ? 'Add Equipment' : 'Edit Equipment'" modal style="width:440px">
            <div class="flex flex-col gap-3 pt-2">
                <div class="dlg-field">
                    <label class="field-label">Equipment Name *</label>
                    <InputText v-model="equipDlg.data.name" class="w-full" placeholder="e.g. Boom Truck 50T" />
                </div>
                <div class="flex gap-3">
                    <div class="dlg-field flex-1">
                        <label class="field-label">Hourly</label>
                        <InputNumber v-model="equipDlg.data.hourly" mode="decimal" :minFractionDigits="2" :maxFractionDigits="2" class="w-full" />
                    </div>
                    <div class="dlg-field flex-1">
                        <label class="field-label">Daily</label>
                        <InputNumber v-model="equipDlg.data.daily" mode="decimal" :minFractionDigits="2" :maxFractionDigits="2" class="w-full" />
                    </div>
                </div>
                <div class="flex gap-3">
                    <div class="dlg-field flex-1">
                        <label class="field-label">Weekly</label>
                        <InputNumber v-model="equipDlg.data.weekly" mode="decimal" :minFractionDigits="2" :maxFractionDigits="2" class="w-full" />
                    </div>
                    <div class="dlg-field flex-1">
                        <label class="field-label">Monthly</label>
                        <InputNumber v-model="equipDlg.data.monthly" mode="decimal" :minFractionDigits="2" :maxFractionDigits="2" class="w-full" />
                    </div>
                </div>
            </div>
            <template #footer>
                <Button label="Cancel" text @click="equipDlg.visible = false" />
                <Button label="Apply" icon="pi pi-check" @click="applyEquipment" :disabled="!equipDlg.data.name.trim()" />
            </template>
        </Dialog>

        <!-- ── Expense Dialog ───────────────────────────────────────────── -->
        <Dialog v-model:visible="expenseDlg.visible" :header="expenseDlgTitle" modal style="width:400px">
            <div class="flex flex-col gap-3 pt-2">
                <div class="dlg-field">
                    <label class="field-label">DESCRIPTION *</label>
                    <InputText v-model="expenseDlg.data.description" class="w-full"
                        :placeholder="expenseDlg.data.category === 'PerDiem' ? 'e.g., Standard Per Diem'
                                    : expenseDlg.data.category === 'Travel'  ? 'e.g., Mileage Reimbursement'
                                    : 'e.g., Standard Hotel'" />
                </div>
                <div class="flex gap-3">
                    <div class="dlg-field flex-1">
                        <label class="field-label">RATE ($)</label>
                        <InputNumber v-model="expenseDlg.data.rate" mode="decimal" :minFractionDigits="2" :maxFractionDigits="2" class="w-full" />
                    </div>
                    <!-- Travel only: show PER unit dropdown -->
                    <div v-if="expenseDlg.data.category === 'Travel'" class="dlg-field flex-1">
                        <label class="field-label">PER</label>
                        <Dropdown v-model="expenseDlg.data.unit" :options="['mile','day','trip','hour']" class="w-full" />
                    </div>
                </div>
            </div>
            <template #footer>
                <Button label="Cancel" text @click="expenseDlg.visible = false" />
                <Button label="Save" icon="pi pi-check" @click="applyExpense" :disabled="!expenseDlg.data.description.trim()" />
            </template>
        </Dialog>

        <!-- Reset Defaults Confirmation Dialog -->
        <Dialog v-model:visible="resetConfirmVisible" header="Reset to Defaults" modal style="width: 460px;" data-testid="reset-confirm-dialog">
            <p style="margin: 0 0 8px; color: var(--text-color-secondary); font-size: 0.88rem;">
                Reset burden items and expenses to standard defaults? This will overwrite your current overhead configuration.
                Labor and equipment rates are not affected.
            </p>
            <template #footer>
                <Button label="Cancel" text @click="resetConfirmVisible = false" />
                <Button label="Reset" icon="pi pi-refresh" severity="warning" @click="onConfirmReset" />
            </template>
        </Dialog>

        <Toast />
        <ConfirmDialog />
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useApiStore } from '@/stores/apiStore';
import { useToast } from 'primevue/usetoast';
import { useConfirm } from 'primevue/useconfirm';
import ConfirmDialog from 'primevue/confirmdialog';
import BasePageHeader from '@/components/layout/BasePageHeader.vue';

// ── Types ─────────────────────────────────────────────────────────────────────

interface OverheadItem {
    category: string;
    code: string;
    name: string;
    burdenType: string; // 'percentage' | 'dollar_per_hour'
    value: number;
}

interface LaborRate {
    position: string;
    laborType: string;
    craftCode: string | null;
    navCode: string | null;
    stRate: number;
    otRate: number;
    dtRate: number;
    needsReview?: boolean;
}

interface EquipmentRate {
    name: string;
    hourly: number | null;
    daily: number | null;
    weekly: number | null;
    monthly: number | null;
}

interface ExpenseItem {
    category: string;
    description: string;
    rate: number;
    unit: string;
}

interface CostBook {
    costBookId: number;
    name: string;
    isDefault: boolean;
    updatedAt?: string;
    laborRates: LaborRate[];
    equipmentRates: EquipmentRate[];
    expenses: ExpenseItem[];
    overheadItems: OverheadItem[];
}

interface BookListItem {
    costBookId: number;
    name: string;
    isDefault: boolean;
    updatedAt?: string;
}

// ── State ─────────────────────────────────────────────────────────────────────

const apiStore = useApiStore();
const toast = useToast();
const confirm = useConfirm();
const resetConfirmVisible = ref(false);

const bookList = ref<BookListItem[]>([]);
const selectedBookId = ref<number | null>(null);
const costBook = ref<CostBook | null>(null);
const loading = ref(false);
const saving = ref(false);
const dirty = ref(false);
const apiError = ref<string | null>(null);

const collapsed = ref({ burden: false, labor: false, equipment: false, expenses: false });

// ── Burden grouping ───────────────────────────────────────────────────────────

const BURDEN_COL: Record<string, string> = {
    FICA: 'Labor Burden', FUTA: 'Labor Burden', SUTA: 'Labor Burden',
    HEALTH: 'Labor Burden', '401K': 'Labor Burden', TRAINING: 'Labor Burden',
    WC: 'Insurance & Bonds', GL: 'Insurance & Bonds', AUTO: 'Insurance & Bonds',
    UMBRELLA: 'Insurance & Bonds', BOND: 'Insurance & Bonds',
    GA: 'Other Overhead',
};

const burdenGroups = computed(() => {
    const items = costBook.value?.overheadItems ?? [];
    const groups: Record<string, OverheadItem[]> = {
        'Labor Burden': [],
        'Insurance & Bonds': [],
        'Other Overhead': [],
    };
    for (const item of items) {
        const col = BURDEN_COL[item.code.toUpperCase()] ?? 'Other Overhead';
        groups[col].push(item);
    }
    return Object.entries(groups).map(([title, items]) => ({ title, items }));
});

const burdenPctSum = computed(() =>
    (costBook.value?.overheadItems ?? [])
        .filter(i => i.burdenType === 'percentage')
        .reduce((s, i) => s + i.value, 0)
);

const burdenDollarSum = computed(() =>
    (costBook.value?.overheadItems ?? [])
        .filter(i => i.burdenType === 'dollar_per_hour')
        .reduce((s, i) => s + i.value, 0)
);

const totalBurdenDisplay = computed(() =>
    `${burdenPctSum.value.toFixed(1)}% + $${burdenDollarSum.value.toFixed(2)}/hr`
);

function burdenedSt(stRate: number): number {
    return stRate * (1 + burdenPctSum.value / 100) + burdenDollarSum.value;
}

function groupSubtotalLabel(items: OverheadItem[]): string {
    const pct = items.filter(i => i.burdenType === 'percentage').reduce((s, i) => s + i.value, 0);
    const dollar = items.filter(i => i.burdenType === 'dollar_per_hour').reduce((s, i) => s + i.value, 0);
    const parts: string[] = [];
    if (pct) parts.push(`${pct.toFixed(2)}%`);
    if (dollar) parts.push(`$${dollar.toFixed(2)}/hr`);
    return `Subtotal: ${parts.join(' + ') || '0'}`;
}

function laborByType(type: string): LaborRate[] {
    return (costBook.value?.laborRates ?? []).filter(r => r.laborType === type);
}

const reviewCount = computed(() =>
    (costBook.value?.laborRates ?? []).filter(r => r.needsReview).length
);

// ── Load ──────────────────────────────────────────────────────────────────────

onMounted(async () => {
    loading.value = true;
    apiError.value = null;
    try {
        const { data: list } = await apiStore.api.get('/api/v1/cost-books');
        bookList.value = list as BookListItem[];
        const def = bookList.value.find(b => b.isDefault) ?? bookList.value[0];
        if (def) {
            selectedBookId.value = def.costBookId;
            const { data: book } = await apiStore.api.get(`/api/v1/cost-books/${def.costBookId}`);
            costBook.value = book as CostBook;
        }
    } catch (err: any) {
        const isNetwork = err?.code === 'ERR_NETWORK' || err?.code === 'ECONNREFUSED' || !err?.response;
        if (isNetwork) {
            apiError.value = 'ERR_NETWORK — API not reachable';
        } else {
            toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load cost books', life: 4000 });
        }
    } finally {
        loading.value = false;
    }
});

async function loadBook() {
    if (!selectedBookId.value) return;
    loading.value = true;
    try {
        const { data } = await apiStore.api.get(`/api/v1/cost-books/${selectedBookId.value}`);
        costBook.value = data as CostBook;
        dirty.value = false;
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load cost book', life: 4000 });
    } finally {
        loading.value = false;
    }
}

// ── Save ──────────────────────────────────────────────────────────────────────

async function saveAll() {
    if (!costBook.value) return;
    saving.value = true;
    try {
        await apiStore.api.put(`/api/v1/cost-books/${costBook.value.costBookId}`, {
            name: costBook.value.name,
            isDefault: costBook.value.isDefault,
            laborRates: costBook.value.laborRates,
            equipmentRates: costBook.value.equipmentRates,
            expenses: costBook.value.expenses,
            overheadItems: costBook.value.overheadItems,
        });
        dirty.value = false;
        toast.add({ severity: 'success', summary: 'Saved', detail: 'Cost book saved successfully', life: 3000 });
        // Refresh updatedAt
        const { data: list } = await apiStore.api.get('/api/v1/cost-books');
        bookList.value = list as BookListItem[];
        const updated = bookList.value.find(b => b.costBookId === costBook.value!.costBookId);
        if (updated) costBook.value.updatedAt = updated.updatedAt;
    } catch {
        toast.add({ severity: 'error', summary: 'Save Failed', detail: 'Could not save cost book', life: 4000 });
    } finally {
        saving.value = false;
    }
}

// ── Reset Defaults ────────────────────────────────────────────────────────────

function confirmReset() {
    resetConfirmVisible.value = true;
}

async function onConfirmReset() {
    resetConfirmVisible.value = false;
    await doReset();
}

async function doReset() {
    saving.value = true;
    try {
        await apiStore.api.post('/api/v1/cost-books/reset-standard');
        // Reload the book list and selected book
        const { data: list } = await apiStore.api.get('/api/v1/cost-books');
        bookList.value = list as BookListItem[];
        const def = bookList.value.find(b => b.isDefault) ?? bookList.value[0];
        if (def) {
            selectedBookId.value = def.costBookId;
            await loadBook();
        }
        toast.add({ severity: 'success', summary: 'Reset', detail: 'Cost book reset to standard defaults', life: 3000 });
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Reset failed', life: 4000 });
    } finally {
        saving.value = false;
    }
}

// ── Burden toggle ─────────────────────────────────────────────────────────────

function toggleBurdenType(item: OverheadItem) {
    item.burdenType = item.burdenType === 'percentage' ? 'dollar_per_hour' : 'percentage';
    dirty.value = true;
}

function markDirty() {
    dirty.value = true;
}

// ── Labor dialog ──────────────────────────────────────────────────────────────

const emptyLabor = (): LaborRate => ({
    position: '', laborType: 'Direct', craftCode: null, navCode: null,
    stRate: 0, otRate: 0, dtRate: 0,
});

const laborDlg = ref({ visible: false, isNew: true, idx: -1, data: emptyLabor() });

function addLabor() {
    laborDlg.value = { visible: true, isNew: true, idx: -1, data: emptyLabor() };
}

function editLabor(row: LaborRate) {
    const idx = costBook.value!.laborRates.indexOf(row);
    laborDlg.value = { visible: true, isNew: false, idx, data: { ...row } };
}

function applyLabor() {
    const d = laborDlg.value.data;
    if (!d.position.trim()) return;
    // When user explicitly saves a rate, clear the NeedsReview flag
    const row = { ...d, needsReview: false };
    if (laborDlg.value.isNew) {
        costBook.value!.laborRates.push(row);
    } else {
        costBook.value!.laborRates[laborDlg.value.idx] = row;
    }
    laborDlg.value.visible = false;
    dirty.value = true;
}

function deleteLabor(row: LaborRate) {
    confirm.require({
        message: `Delete "${row.position}"?`,
        header: 'Confirm Delete',
        icon: 'pi pi-trash',
        acceptLabel: 'Delete',
        rejectLabel: 'Cancel',
        acceptClass: 'p-button-danger',
        accept: () => {
            costBook.value!.laborRates.splice(costBook.value!.laborRates.indexOf(row), 1);
            dirty.value = true;
        },
    });
}

// ── Equipment dialog ──────────────────────────────────────────────────────────

const emptyEquip = (): EquipmentRate => ({
    name: '', hourly: null, daily: null, weekly: null, monthly: null,
});

const equipDlg = ref({ visible: false, isNew: true, idx: -1, data: emptyEquip() });

function addEquipment() {
    equipDlg.value = { visible: true, isNew: true, idx: -1, data: emptyEquip() };
}

function editEquipment(row: EquipmentRate) {
    const idx = costBook.value!.equipmentRates.indexOf(row);
    equipDlg.value = { visible: true, isNew: false, idx, data: { ...row } };
}

function applyEquipment() {
    const d = equipDlg.value.data;
    if (!d.name.trim()) return;
    if (equipDlg.value.isNew) {
        costBook.value!.equipmentRates.push({ ...d });
    } else {
        costBook.value!.equipmentRates[equipDlg.value.idx] = { ...d };
    }
    equipDlg.value.visible = false;
    dirty.value = true;
}

function deleteEquipment(row: EquipmentRate) {
    confirm.require({
        message: `Delete "${row.name}"?`,
        header: 'Confirm Delete',
        icon: 'pi pi-trash',
        acceptLabel: 'Delete',
        rejectLabel: 'Cancel',
        acceptClass: 'p-button-danger',
        accept: () => {
            costBook.value!.equipmentRates.splice(costBook.value!.equipmentRates.indexOf(row), 1);
            dirty.value = true;
        },
    });
}

// ── Expense dialog ────────────────────────────────────────────────────────────

const emptyExpense = (): ExpenseItem => ({
    category: '', description: '', rate: 0, unit: 'Day',
});

const expenseDlg = ref({ visible: false, isNew: true, idx: -1, data: emptyExpense() });

const expenseDlgTitle = computed(() => {
    const cat = expenseDlg.value.data.category;
    const verb = expenseDlg.value.isNew ? 'Add' : 'Edit';
    if (cat === 'PerDiem') return `${verb} Per Diem`;
    if (cat === 'Travel')  return `${verb} Travel`;
    if (cat === 'Lodging') return `${verb} Lodging`;
    return `${verb} Expense`;
});

function expensesByCategory(cat: string) {
    return costBook.value?.expenses.filter(e => e.category === cat) ?? [];
}

function addExpense(category: string = '') {
    const defaultUnit = category === 'PerDiem' ? 'day' : category === 'Lodging' ? 'night' : 'day';
    expenseDlg.value = { visible: true, isNew: true, idx: -1, data: { ...emptyExpense(), category, unit: defaultUnit } };
}

function editExpense(row: ExpenseItem) {
    const idx = costBook.value!.expenses.indexOf(row);
    expenseDlg.value = { visible: true, isNew: false, idx, data: { ...row } };
}

function applyExpense() {
    const d = expenseDlg.value.data;
    if (!d.description.trim()) return;
    // Auto-set unit for non-Travel categories
    if (d.category === 'PerDiem') d.unit = 'day';
    if (d.category === 'Lodging') d.unit = 'night';
    if (expenseDlg.value.isNew) {
        costBook.value!.expenses.push({ ...d });
    } else {
        costBook.value!.expenses[expenseDlg.value.idx] = { ...d };
    }
    expenseDlg.value.visible = false;
    dirty.value = true;
}

function deleteExpense(row: ExpenseItem) {
    confirm.require({
        message: `Delete "${row.description}"?`,
        header: 'Confirm Delete',
        icon: 'pi pi-trash',
        acceptLabel: 'Delete',
        rejectLabel: 'Cancel',
        acceptClass: 'p-button-danger',
        accept: () => {
            costBook.value!.expenses.splice(costBook.value!.expenses.indexOf(row), 1);
            dirty.value = true;
        },
    });
}

// ── Formatting ────────────────────────────────────────────────────────────────

function fmtRate(n: number): string { return `$${n.toFixed(2)}`; }

function fmtDate(d?: string): string {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}
</script>

<style scoped>
.cost-book-view {
    display: flex;
    flex-direction: column;
    gap: 10px;
    padding-bottom: 32px;
}

/* ── Internal badge ── */
.internal-hdr-badge {
    display: inline-flex;
    align-items: center;
    gap: 5px;
    background: color-mix(in srgb, var(--red-500) 12%, transparent);
    border: 1px solid color-mix(in srgb, var(--red-500) 30%, transparent);
    color: var(--red-400);
    font-size: 0.68rem;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    padding: 4px 10px;
    border-radius: 6px;
}

/* ── No book state ── */
.no-book-msg {
    display: flex;
    align-items: center;
    gap: 16px;
    padding: 24px 20px;
    background: color-mix(in srgb, var(--yellow-500) 8%, transparent);
    border: 1px solid color-mix(in srgb, var(--yellow-500) 25%, transparent);
    border-radius: 6px;
}

.no-book-msg.error-msg {
    background: color-mix(in srgb, var(--red-500) 8%, transparent);
    border-color: color-mix(in srgb, var(--red-500) 25%, transparent);
}

.no-book-msg code {
    font-family: monospace;
    font-size: 0.85em;
    background: color-mix(in srgb, var(--surface-border) 60%, transparent);
    padding: 1px 4px;
    border-radius: 3px;
}

/* ── Stats bar ── */
.stats-bar {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 8px;
    padding: 10px 14px;
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 6px;
}

.stat-chip {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    background: var(--surface-ground);
    border: 1px solid var(--surface-border);
    border-radius: 10px;
    padding: 3px 10px;
    font-size: 0.75rem;
}

.stat-label {
    color: var(--text-color-secondary);
    font-weight: 500;
}

.stat-val {
    font-weight: 700;
    color: var(--text-color);
}

.stat-val.primary { color: var(--primary-color); }

.dirty-chip {
    color: var(--orange-400);
    border-color: color-mix(in srgb, var(--orange-400) 30%, transparent);
    background: color-mix(in srgb, var(--orange-400) 8%, transparent);
    gap: 5px;
    font-weight: 600;
}

/* ── Section chrome ── */
.cb-section {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 6px;
    overflow: hidden;
}

.cb-section-header {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 8px 14px;
    cursor: pointer;
    user-select: none;
    background: var(--surface-section);
    border-bottom: 1px solid var(--surface-border);
    transition: background 0.15s;
}

.cb-section-header:hover { background: var(--surface-hover); }

.cb-toggle {
    font-size: 0.7rem;
    color: var(--text-color-secondary);
    transition: transform 0.2s;
}
.cb-toggle.rotated { transform: rotate(-90deg); }

.cb-icon {
    font-size: 0.85rem;
    color: var(--primary-color);
}

.cb-section-title {
    font-size: 0.78rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: var(--text-color);
}

.cb-section-badge {
    font-size: 0.7rem;
    color: var(--text-color-secondary);
    background: var(--surface-ground);
    border: 1px solid var(--surface-border);
    border-radius: 10px;
    padding: 1px 8px;
}

.cb-section-body {
    padding: 0;
}

/* ── Burden grid ── */
.burden-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 0;
    border-bottom: 1px solid var(--surface-border);
}

.burden-col {
    padding: 14px 16px;
    border-right: 1px solid var(--surface-border);
}
.burden-col:last-child { border-right: none; }

.burden-col-title {
    font-size: 0.72rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: var(--text-color-secondary);
    padding-bottom: 10px;
    border-bottom: 1px solid var(--surface-border);
    margin-bottom: 10px;
}

.burden-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px;
    padding: 3px 0;
}

.burden-name {
    font-size: 0.8rem;
    color: var(--text-color);
    min-width: 0;
    flex: 1;
}

.burden-input-row {
    display: flex;
    align-items: center;
    gap: 4px;
}

.burden-input {
    width: 120px;
}

.burden-input :deep(.p-inputnumber-input) {
    font-size: 0.78rem;
    padding: 3px 8px;
    text-align: right;
    font-family: monospace;
    width: 100%;
}

.type-toggle-btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-width: 34px;
    height: 26px;
    font-size: 0.68rem;
    font-weight: 700;
    border-radius: 4px;
    border: 1px solid var(--surface-border);
    cursor: pointer;
    transition: background 0.15s, color 0.15s;
    background: var(--surface-ground);
    color: var(--text-color-secondary);
}
.type-toggle-btn:hover { background: var(--surface-hover); }
.type-toggle-btn.type-pct  { color: var(--primary-color); border-color: color-mix(in srgb, var(--primary-color) 40%, transparent); }
.type-toggle-btn.type-dollar { color: var(--green-500); border-color: color-mix(in srgb, var(--green-500) 40%, transparent); }

.burden-col-subtotal {
    margin-top: 10px;
    padding-top: 8px;
    border-top: 1px solid var(--surface-border);
    font-size: 0.72rem;
    font-weight: 600;
    color: var(--primary-color);
    text-align: right;
}

.burden-empty {
    font-size: 0.75rem;
    color: var(--text-color-secondary);
    padding: 6px 0;
    font-style: italic;
}

/* ── Tables ── */
.table-scroll { overflow-x: auto; }

.cb-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.8rem;
    min-width: 600px;
}

.cb-table th,
.cb-table td {
    padding: 5px 8px;
    border-bottom: 1px solid var(--surface-border);
    white-space: nowrap;
    vertical-align: middle;
}

.cb-table thead th {
    background: var(--surface-100);
    font-size: 0.7rem;
    font-weight: 600;
    color: var(--text-color-secondary);
    text-align: left;
    position: sticky;
    top: 0;
    z-index: 1;
}

.group-header-row td {
    background: var(--surface-ground);
    font-size: 0.72rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--text-color-secondary);
    padding: 5px 8px;
    border-bottom: 1px solid var(--surface-border);
}

.data-row:hover td { background: var(--surface-hover); }

.needs-review-row td { background: rgba(234, 179, 8, 0.08); }
.needs-review-row:hover td { background: rgba(234, 179, 8, 0.15); }
.review-flag {
    display: inline-block;
    margin-left: 8px;
    font-size: 0.68rem;
    font-weight: 700;
    color: #f59e0b;
    background: rgba(245, 158, 11, 0.15);
    border: 1px solid rgba(245, 158, 11, 0.4);
    border-radius: 3px;
    padding: 1px 5px;
    cursor: help;
}

.review-banner {
    display: flex;
    align-items: flex-start;
    gap: 10px;
    background: rgba(234, 179, 8, 0.12);
    border: 1px solid rgba(234, 179, 8, 0.5);
    border-radius: 6px;
    padding: 10px 14px;
    margin-bottom: 12px;
    font-size: 0.85rem;
    color: var(--text-color);
}
.review-banner .pi { color: #f59e0b; margin-top: 2px; flex-shrink: 0; }

.code-cell {
    font-family: monospace;
    font-size: 0.75rem;
    color: var(--text-color-secondary);
}

.rate-cell {
    font-family: monospace;
    font-size: 0.78rem;
}

.act-col {
    width: 76px;
    text-align: right;
}

.empty-group-row td {
    text-align: center;
    padding: 10px 8px;
    color: var(--text-color-secondary);
    font-size: 0.78rem;
    font-style: italic;
}

/* ── Add row bar ── */
.add-row-bar {
    padding: 6px 8px;
    border-top: 1px solid var(--surface-border);
    background: var(--surface-ground);
}

/* ── Expense panels ── */
.expense-panels {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 16px;
    padding: 16px;
}

.expense-panel {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 6px;
    overflow: hidden;
}

.expense-panel-hdr {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 10px 12px;
    background: var(--surface-section);
    border-bottom: 1px solid var(--surface-border);
}

.expense-panel-title {
    font-size: 0.7rem;
    font-weight: 700;
    letter-spacing: 0.08em;
    color: var(--text-color-secondary);
    text-transform: uppercase;
}

.expense-add-btn {
    font-size: 0.75rem !important;
    padding: 3px 10px !important;
}

.expense-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.82rem;
}

.expense-table th {
    padding: 5px 8px;
    font-size: 0.68rem;
    font-weight: 600;
    letter-spacing: 0.06em;
    color: var(--text-color-secondary);
    border-bottom: 1px solid var(--surface-border);
    white-space: nowrap;
}

.expense-table td {
    padding: 5px 8px;
    border-bottom: 1px solid var(--surface-border);
    color: var(--text-color);
    white-space: nowrap;
    vertical-align: middle;
}

.expense-table tbody tr:last-child td {
    border-bottom: none;
}

.expense-table .unit-cell {
    color: var(--text-color-secondary);
    font-size: 0.78rem;
}

.expense-table .act-col {
    width: 64px;
    text-align: right;
    white-space: nowrap;
}

.expense-table .act-col .p-button {
    display: inline-flex !important;
}

/* ── Dialog fields ── */
.dlg-field {
    display: flex;
    flex-direction: column;
    gap: 4px;
}

.field-label {
    font-size: 0.75rem;
    font-weight: 600;
    color: var(--text-color-secondary);
    text-transform: uppercase;
    letter-spacing: 0.04em;
}

.dlg-burdened-preview {
    font-size: 0.78rem;
    color: var(--text-color-secondary);
    background: var(--surface-ground);
    border-radius: 4px;
    padding: 6px 10px;
}
</style>
