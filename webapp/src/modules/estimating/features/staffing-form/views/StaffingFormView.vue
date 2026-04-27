<template>
    <div class="staffing-form-view" data-testid="sp-form-view">
        <BasePageHeader
            icon="pi pi-users"
            :title="isNew ? 'New Staffing Plan' : (store.header.staffingPlanNumber ?? 'Staffing Plan')"
            :subtitle="isNew ? 'Create a new staffing plan' : store.header.name"
        >
            <div class="flex gap-2 align-items-center flex-wrap">
                <Tag v-if="store.isConverted" value="Converted to Estimate" severity="success" icon="pi pi-check" data-testid="sp-converted-tag" />
                <Tag v-else-if="store.isDirty" value="Unsaved changes" severity="warning" data-testid="sp-dirty-tag" />
                <Button
                    v-if="!store.isConverted"
                    label="Save"
                    icon="pi pi-save"
                    :loading="store.isSaving"
                    data-testid="sp-save"
                    @click="savePlan"
                />
                <Button
                    v-if="!store.isConverted && store.header.status !== 'Submitted for Approval'"
                    label="Submit for Approval"
                    icon="pi pi-send"
                    severity="success"
                    :loading="submittingSpForApproval"
                    data-testid="sp-submit-approval"
                    @click="submitSpForApproval"
                />
                <Button
                    v-if="!isNew && !store.isConverted"
                    label="Duplicate"
                    icon="pi pi-copy"
                    severity="secondary"
                    outlined
                    :loading="duplicatingPlan"
                    data-testid="sp-duplicate"
                    @click="duplicateThisPlan"
                />
                <Button
                    v-if="!isNew && !store.isConverted"
                    label="Convert to Estimate"
                    icon="pi pi-arrow-right"
                    severity="secondary"
                    data-testid="sp-convert"
                    @click="confirmConvert"
                />
                <Button
                    v-if="store.isConverted"
                    :label="`View Estimate`"
                    icon="pi pi-external-link"
                    outlined
                    data-testid="sp-view-estimate"
                    @click="router.push(`/estimating/estimates/${store.header.convertedEstimateId}`)"
                />
                <BaseButtonCancel label="Back" icon="pi pi-arrow-left" @click="router.push('/estimating/staffing-plans')" />
            </div>
        </BasePageHeader>

        <div v-if="store.isLoading" class="flex justify-content-center py-8" data-testid="sp-form-loading">
            <ProgressSpinner />
        </div>

        <div v-else class="flex flex-col gap-4">
            <Message v-if="store.isConverted" severity="success" :closable="false" data-testid="sp-converted-message">
                This staffing plan has been converted to an estimate.
                <Button
                    :label="`Open Estimate`"
                    icon="pi pi-external-link"
                    text
                    size="small"
                    class="ml-2"
                    @click="router.push(`/estimating/estimates/${store.header.convertedEstimateId}`)"
                />
            </Message>

            <div class="sp-header">
                <div class="sp-title-row">
                    <i class="pi pi-users" />
                    <span class="sp-title">Plan Information</span>
                    <Tag v-if="store.header.status" :value="store.header.status" :severity="spStatusSeverity(store.header.status)" class="ml-2" />
                    <Tag v-if="store.header.staffingPlanNumber" :value="store.header.staffingPlanNumber" class="ml-1 font-mono" />
                </div>

                <div class="sp-hrow">
                    <div class="sp-hfield" style="flex:3">
                        <label>PLAN NAME *</label>
                        <InputText
                            v-model="store.header.name"
                            placeholder="Enter plan name..."
                            class="w-full"
                            :disabled="store.isConverted"
                            data-testid="sp-name"
                            @input="store.markDirty()"
                        />
                    </div>
                    <div class="sp-hfield" style="flex:2.5">
                        <label>CLIENT *</label>
                        <InputText
                            v-model="store.header.client"
                            placeholder="Client company..."
                            class="w-full"
                            :disabled="store.isConverted"
                            data-testid="sp-client"
                            @input="store.markDirty()"
                        />
                    </div>
                    <div class="sp-hfield" style="flex:1">
                        <label>CLIENT CODE</label>
                        <InputText
                            v-model="store.header.clientCode"
                            placeholder="BP"
                            maxlength="10"
                            class="w-full"
                            :disabled="store.isConverted"
                            data-testid="sp-client-code"
                            @input="store.markDirty()"
                        />
                    </div>
                    <div class="sp-hfield" style="flex:0.8">
                        <label>JOB LETTER</label>
                        <InputText
                            v-model="store.header.jobLetter"
                            placeholder="H"
                            maxlength="3"
                            class="w-full"
                            :disabled="store.isConverted"
                            data-testid="sp-job-letter"
                            @input="store.markDirty()"
                        />
                    </div>
                    <div class="sp-hfield" style="flex:1.2">
                        <label>STATUS</label>
                        <div class="sp-status-badge-wrap">
                            <Tag :value="store.header.status || 'Draft'" :severity="spStatusSeverity(store.header.status || 'Draft')" data-testid="sp-status" />
                        </div>
                    </div>
                </div>

                <div class="sp-hrow">
                    <div class="sp-hfield" style="flex:1.2">
                        <label>BRANCH</label>
                        <InputText
                            v-model="store.header.branch"
                            placeholder="Branch"
                            class="w-full"
                            :disabled="store.isConverted"
                            data-testid="sp-branch"
                            @input="store.markDirty()"
                        />
                    </div>
                    <div class="sp-hfield" style="flex:2">
                        <label>CITY</label>
                        <InputText
                            v-model="store.header.city"
                            placeholder="City..."
                            class="w-full"
                            :disabled="store.isConverted"
                            data-testid="sp-city"
                            @input="store.markDirty()"
                        />
                    </div>
                    <div class="sp-hfield" style="flex:0.7">
                        <label>STATE</label>
                        <InputText
                            v-model="store.header.state"
                            placeholder="--"
                            maxlength="2"
                            class="w-full"
                            :disabled="store.isConverted"
                            data-testid="sp-state"
                            @input="store.markDirty()"
                        />
                    </div>
                    <div class="sp-hfield" style="flex:1.3">
                        <label>START DATE</label>
                        <InputText
                            v-model="store.header.startDate"
                            type="date"
                            class="w-full"
                            :disabled="store.isConverted"
                            data-testid="sp-start-date"
                            @input="onDatesChange"
                        />
                    </div>
                    <div class="sp-hfield" style="flex:1.3">
                        <label>END DATE</label>
                        <InputText
                            v-model="store.header.endDate"
                            type="date"
                            class="w-full"
                            :disabled="store.isConverted"
                            data-testid="sp-end-date"
                            @input="onDatesChange"
                        />
                    </div>
                    <div class="sp-hfield" style="flex:0.6">
                        <label>DAYS</label>
                        <InputNumber
                            v-model="store.header.days"
                            :min="0"
                            :max="9999"
                            class="w-full"
                            inputClass="w-full"
                            :disabled="store.isConverted"
                            data-testid="sp-days"
                            @input="store.markDirty()"
                        />
                    </div>
                </div>

                <div class="sp-hrow">
                    <div class="sp-hfield" style="flex:1">
                        <label>SHIFT</label>
                        <Dropdown
                            v-model="store.header.shift"
                            :options="shiftOptions"
                            class="w-full"
                            :disabled="store.isConverted"
                            data-testid="sp-shift"
                            @change="store.markDirty()"
                        />
                    </div>
                    <div class="sp-hfield" style="flex:0.9">
                        <label>HRS/SHIFT</label>
                        <InputNumber
                            v-model="store.header.hoursPerShift"
                            :min="1"
                            :max="24"
                            :maxFractionDigits="1"
                            class="w-full"
                            inputClass="w-full"
                            :disabled="store.isConverted"
                            data-testid="sp-hours-per-shift"
                            @input="store.markDirty()"
                        />
                    </div>
                    <div class="sp-hfield" style="flex:2.5">
                        <label>OVERTIME RULES</label>
                        <Dropdown
                            v-model="store.header.otMethod"
                            :options="otMethodOptions"
                            optionLabel="label"
                            optionValue="value"
                            class="w-full"
                            :disabled="store.isConverted"
                            data-testid="sp-ot-method"
                            @change="store.markDirty()"
                        />
                    </div>
                    <div class="sp-hfield" style="flex:0.8">
                        <label>DT W/E</label>
                        <Dropdown
                            v-model="store.header.dtWeekends"
                            :options="dtWeekendsOptions"
                            optionLabel="label"
                            optionValue="value"
                            class="w-full"
                            :disabled="store.isConverted"
                            data-testid="sp-dt-weekends"
                            @change="store.markDirty()"
                        />
                    </div>
                </div>
            </div>

            <LaborGrid
                v-model:rows="store.laborRows"
                :startDate="store.header.startDate"
                :endDate="store.header.endDate"
                :hoursPerShift="store.header.hoursPerShift"
                :otMethod="store.header.otMethod"
                :dtWeekends="store.header.dtWeekends"
                :shift="store.header.shift"
                :rateBookRates="store.rateBookRates"
                :rateBookName="store.rateBookName"
                @change="store.markDirty()"
                @openRateBook="spRateBookDialogVisible = true"
                @clearRateBook="store.clearRateBook()"
                @applyRates="store.applyRateBookToRows()"
                @loadCrew="spCrewDialogVisible = true"
            />

            <!-- Job Cost Analysis + Summary -->
            <template v-if="store.laborRows.length > 0">
                <JobCostAnalysis
                    :laborRows="store.laborRows"
                    :hoursPerShift="store.header.hoursPerShift ?? 10"
                    @update:internalCost="spInternalCost = $event"
                />
                <div class="sp-summary">
                    <div class="sp-summary-header">
                        <i class="pi pi-calculator" />
                        <span>Plan Summary</span>
                    </div>
                    <div class="sp-summary-kpis">
                        <div class="sp-sum-kpi">
                            <span class="sp-sum-label">ROUGH REVENUE</span>
                            <span class="sp-sum-value">{{ fmtCurrency(roughRevenue) }}</span>
                        </div>
                        <div class="sp-sum-kpi">
                            <span class="sp-sum-label">INTERNAL COST</span>
                            <span class="sp-sum-value">{{ fmtCurrency(spInternalCost) }}</span>
                        </div>
                        <div class="sp-sum-kpi">
                            <span class="sp-sum-label">GROSS PROFIT</span>
                            <span class="sp-sum-value">{{ fmtCurrency(roughRevenue - spInternalCost) }}</span>
                        </div>
                        <div class="sp-sum-kpi">
                            <span class="sp-sum-label">GROSS MARGIN</span>
                            <Tag
                                :value="roughRevenue > 0 ? spMarginPct.toFixed(1) + '%' : '—'"
                                :severity="marginSeverity"
                                class="sp-margin-tag"
                            />
                        </div>
                    </div>
                </div>
            </template>
        </div>

        <Toast />

        <!-- Rate book picker -->
        <Dialog v-model:visible="spRateBookDialogVisible" header="Load Rate Book" modal style="width: 520px;">
            <div v-if="spRateBookListLoading" class="flex justify-content-center py-6">
                <ProgressSpinner />
            </div>
            <div v-else-if="spRateBookList.length === 0" class="text-center py-4 text-slate-400 text-sm">
                No rate books found. Create one in the Rate Books section.
            </div>
            <div v-else class="rate-book-picker-list">
                <div
                    v-for="rb in spRateBookList"
                    :key="rb.rateBookId"
                    class="rb-row"
                    @click="spPickRateBook(rb)"
                >
                    <div class="rb-name">{{ rb.name }}</div>
                    <div class="rb-meta">
                        <span v-if="rb.client">{{ rb.client }}</span>
                        <span v-if="rb.city">{{ rb.city }}<span v-if="rb.state">, {{ rb.state }}</span></span>
                        <Tag v-if="rb.isStandardBaseline" value="Baseline" severity="info" class="rb-tag" />
                        <span class="rb-count">{{ rb.laborCount }} positions</span>
                    </div>
                </div>
            </div>
            <template #footer>
                <Button label="Cancel" text @click="spRateBookDialogVisible = false" />
            </template>
        </Dialog>

        <!-- Crew template picker -->
        <Dialog v-model:visible="spCrewDialogVisible" header="Load Crew Template" modal style="width: 520px;">
            <div v-if="!store.rateBookName" class="flex align-items-center gap-2 p-2 mb-3 border-round" style="background: rgba(148,163,184,0.08); border: 1px solid var(--surface-border);">
                <i class="pi pi-info-circle" style="color: var(--text-color-secondary); font-size: 0.85rem;" />
                <span style="color: var(--text-color-secondary); font-size: 0.8rem;">No rate book loaded — positions will be added with $0 rates. Load a rate book after to fill rates.</span>
            </div>
            <div v-if="spCrewListLoading" class="flex justify-content-center py-6">
                <ProgressSpinner />
            </div>
            <div v-else-if="spCrewList.length === 0" class="flex flex-col align-items-center gap-3 py-6">
                <i class="pi pi-users text-4xl text-slate-500" />
                <p class="text-slate-400 text-sm m-0">No crew templates found.</p>
                <Button
                    label="Go to Crew Templates"
                    icon="pi pi-arrow-right"
                    severity="secondary"
                    outlined
                    size="small"
                    @click="spCrewDialogVisible = false; router.push('/estimating/crew-templates')"
                />
            </div>
            <div v-else class="rate-book-picker-list">
                <div
                    v-for="tmpl in spCrewList"
                    :key="tmpl.crewTemplateId"
                    class="rb-row"
                    :class="{ 'rb-active': spSelectedCrewId === tmpl.crewTemplateId }"
                    @click="spSelectedCrewId = tmpl.crewTemplateId"
                >
                    <div class="rb-name">{{ tmpl.name }}</div>
                    <div class="rb-meta">
                        <span v-if="tmpl.description" class="text-slate-400 text-sm">{{ tmpl.description }}</span>
                        <span class="rb-count">{{ tmpl.rowCount }} position{{ tmpl.rowCount !== 1 ? 's' : '' }}</span>
                        <span class="text-xs text-slate-500">{{ tmpl.positions?.map((p: any) => p.position).join(', ') }}</span>
                    </div>
                </div>
            </div>
            <template #footer>
                <Button label="Cancel" text @click="spCrewDialogVisible = false" />
                <Button label="Add to Labor" icon="pi pi-plus" :disabled="!spSelectedCrewId || spCrewApplying" :loading="spCrewApplying" @click="applySpCrewTemplate" />
            </template>
        </Dialog>

        <Dialog
            v-model:visible="convertDialogVisible"
            header="Convert to Estimate"
            modal
            style="width: 420px;"
            data-testid="sp-convert-dialog"
        >
            <p style="margin: 0 0 8px; color: var(--text-color-secondary); font-size: 0.88rem;">
                Convert this staffing plan to a full estimate? The plan will be locked and linked to the new estimate.
            </p>
            <template #footer>
                <Button label="Cancel" text @click="convertDialogVisible = false" />
                <Button label="Convert" icon="pi pi-arrow-right" severity="success" data-testid="sp-convert-confirm" @click="doConvert" />
            </template>
        </Dialog>

    </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import type { LaborRow } from '../../../stores/estimateStore';
import { useRoute, useRouter } from 'vue-router';
import { useToast } from 'primevue/usetoast';
import { useStaffingStore } from '../../../stores/staffingStore';
import { useApiStore } from '@/stores/apiStore';
import BasePageHeader from '@/components/layout/BasePageHeader.vue';
import BaseButtonCancel from '@/components/buttons/BaseButtonCancel.vue';
import LaborGrid from '../../estimate-form/components/LaborGrid.vue';
import JobCostAnalysis from '../../estimate-form/components/JobCostAnalysis.vue';

const route = useRoute();
const router = useRouter();
const toast = useToast();
const store = useStaffingStore();
const apiStore = useApiStore();

const convertDialogVisible = ref(false);

// Crew template picker
const spCrewDialogVisible = ref(false);
const spCrewListLoading = ref(false);
const spCrewList = ref<any[]>([]);
const spSelectedCrewId = ref<number | null>(null);
const spCrewApplying = ref(false);

watch(spCrewDialogVisible, async (open) => {
    if (!open) { spSelectedCrewId.value = null; return; }
    if (spCrewList.value.length) return;
    spCrewListLoading.value = true;
    try {
        const { data } = await apiStore.api.get('/api/v1/crew-templates');
        spCrewList.value = data;
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load crew templates', life: 4000 });
    } finally {
        spCrewListLoading.value = false;
    }
});

async function applySpCrewTemplate() {
    if (!spSelectedCrewId.value) return;
    spCrewApplying.value = true;
    try {
        const { data } = await apiStore.api.get(`/api/v1/crew-templates/${spSelectedCrewId.value}`);
        const rateMap = new Map<string, any>();
        for (const r of store.rateBookRates ?? []) {
            rateMap.set(r.position.toLowerCase(), r);
        }
        const startDate = store.header.startDate;
        const endDate = store.header.endDate;

        function buildScheduleWithQty(qty: number): string {
            if (!startDate || !endDate) return '{}';
            const sched: Record<string, number> = {};
            const cur = new Date(startDate + 'T12:00:00');
            const end = new Date(endDate + 'T12:00:00');
            while (cur <= end) {
                sched[cur.toISOString().slice(0, 10)] = qty;
                cur.setDate(cur.getDate() + 1);
            }
            return JSON.stringify(sched);
        }

        const merged: LaborRow[] = [...store.laborRows];
        for (const row of data.rows) {
            const qty = row.qty > 0 ? row.qty : 1;
            const rate = rateMap.get(row.position?.toLowerCase() ?? '');
            const rowShift = row.shift ?? (store.header.shift === 'Both' ? 'Day' : (store.header.shift ?? 'Day'));
            const existing = merged.find(r => r.position === row.position && r.shift === rowShift);
            if (existing) {
                let sched: Record<string, number> = {};
                try { sched = JSON.parse(existing.scheduleJson ?? '{}'); } catch { /* */ }
                if (startDate && endDate) {
                    const cur = new Date(startDate + 'T12:00:00');
                    const end = new Date(endDate + 'T12:00:00');
                    while (cur <= end) {
                        const iso = cur.toISOString().slice(0, 10);
                        sched[iso] = (sched[iso] ?? 0) + qty;
                        cur.setDate(cur.getDate() + 1);
                    }
                } else {
                    for (const iso of Object.keys(sched)) sched[iso] = (sched[iso] ?? 0) + qty;
                }
                existing.scheduleJson = JSON.stringify(sched);
            } else {
                merged.push({
                    position: row.position,
                    laborType: row.laborType ?? 'Direct',
                    shift: rowShift,
                    craftCode: row.craftCode ?? rate?.craftCode ?? null,
                    navCode: rate?.navCode ?? null,
                    billStRate: rate?.stRate ?? 0,
                    billOtRate: rate?.otRate ?? 0,
                    billDtRate: rate?.dtRate ?? 0,
                    scheduleJson: buildScheduleWithQty(qty),
                    stHours: 0,
                    otHours: 0,
                    dtHours: 0,
                    subtotal: 0,
                });
            }
        }
        store.laborRows = merged;
        for (const row of store.laborRows) store.recalcLaborRow(row);
        store.markDirty();
        spCrewDialogVisible.value = false;
        toast.add({ severity: 'success', summary: 'Crew Loaded', detail: `Added ${data.rows.length} position(s) from "${data.name}"`, life: 3000 });
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to apply crew template', life: 4000 });
    } finally {
        spCrewApplying.value = false;
    }
}

// Job Cost Analysis — internal cost captured via component emit
const spInternalCost = ref(0);
const submittingSpForApproval = ref(false);
const duplicatingPlan = ref(false);

const roughRevenue = computed(() =>
    store.laborRows.reduce((sum, r) => sum + (r.subtotal ?? 0), 0),
);

const spMarginPct = computed(() => {
    const rev = roughRevenue.value;
    if (rev <= 0 || spInternalCost.value === 0) return 0;
    return ((rev - spInternalCost.value) / rev) * 100;
});

const marginSeverity = computed(() => {
    if (roughRevenue.value <= 0 || spInternalCost.value === 0) return 'secondary';
    const pct = spMarginPct.value;
    if (pct >= 25) return 'success';
    if (pct >= 15) return 'warning';
    return 'danger';
});

function fmtCurrency(val: number): string {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(val ?? 0);
}

async function submitSpForApproval() {
    submittingSpForApproval.value = true;
    try {
        await savePlan();
        if (!store.header.staffingPlanId) {
            toast.add({ severity: 'error', summary: 'Error', detail: 'Plan must be saved before submitting', life: 4000 });
            return;
        }
        if (!confirm('Submit this staffing plan for approval? Status will change to Submitted for Approval.')) return;
        await apiStore.api.patch(`/api/v1/staffing-plans/${store.header.staffingPlanId}/status`, { status: 'Submitted for Approval' });
        store.header.status = 'Submitted for Approval';
        store.markDirty();
        toast.add({ severity: 'success', summary: 'Submitted', detail: 'Staffing plan submitted for approval', life: 3000 });
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Could not submit for approval', life: 4000 });
    } finally {
        submittingSpForApproval.value = false;
    }
}

async function duplicateThisPlan() {
    if (!store.header.staffingPlanId) return;
    duplicatingPlan.value = true;
    try {
        const { data } = await apiStore.api.post(`/api/v1/staffing-plans/${store.header.staffingPlanId}/duplicate`);
        toast.add({ severity: 'success', summary: 'Duplicated', detail: `New plan created — opening now`, life: 4000 });
        router.push(`/estimating/staffing-plans/${data.staffingPlanId}`);
    } catch {
        toast.add({ severity: 'error', summary: 'Duplicate Failed', detail: 'Could not duplicate plan', life: 4000 });
    } finally {
        duplicatingPlan.value = false;
    }
}

// Rate book picker
const spRateBookDialogVisible = ref(false);
const spRateBookListLoading = ref(false);
const spRateBookList = ref<any[]>([]);

watch(spRateBookDialogVisible, async (open) => {
    if (!open || spRateBookList.value.length) return;
    spRateBookListLoading.value = true;
    try {
        const { data } = await apiStore.api.get('/api/v1/rate-books');
        spRateBookList.value = data;
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load rate books', life: 4000 });
    } finally {
        spRateBookListLoading.value = false;
    }
});

async function spPickRateBook(rb: any) {
    spRateBookDialogVisible.value = false;
    try {
        await store.loadRateBook(rb.rateBookId);
        toast.add({ severity: 'success', summary: 'Rate Book Loaded', detail: rb.name, life: 3000 });
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load rate book', life: 4000 });
    }
}

const isNew = computed(() => !route.params.id || route.params.id === 'new');

const shiftOptions = ['Day', 'Night', 'Both'];
const otMethodOptions = [
    { label: 'Daily 8', value: 'daily8' },
    { label: 'Weekly 40', value: 'weekly40' },
    { label: 'Daily 8 + Weekly 40', value: 'daily8_weekly40' },
    { label: 'California', value: 'california' },
    { label: 'Straight Time', value: 'straight' },
];

const dtWeekendsOptions = [
    { label: 'No DT Weekends', value: 'none' },
    { label: 'Sundays Only',   value: 'sun_only' },
    { label: 'Sat & Sun',      value: 'sat_sun' },
];

onMounted(async () => {
    store.reset();
    if (!isNew.value) {
        try {
            await store.fetchPlan(Number(route.params.id));
        } catch {
            toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load staffing plan', life: 4000 });
        }
    }
});

onBeforeUnmount(() => store.reset());

function toLocalDate(dateValue: string): Date {
    return new Date(`${dateValue}T12:00:00`);
}

function onDatesChange() {
    if (store.header.startDate && store.header.endDate) {
        const start = toLocalDate(store.header.startDate);
        const end = toLocalDate(store.header.endDate);
        const diff = Math.round((end.getTime() - start.getTime()) / 86400000) + 1;
        if (diff > 0) {
            store.header.days = diff;
        }
    }

    store.markDirty();
}

async function savePlan() {
    try {
        await store.savePlan();
        toast.add({ severity: 'success', summary: 'Saved', detail: 'Staffing plan saved', life: 3000 });
        if (isNew.value) {
            router.replace(`/estimating/staffing-plans/${store.header.staffingPlanId}`);
        }
    } catch (err: any) {
        toast.add({
            severity: 'error',
            summary: 'Save Failed',
            detail: err?.response?.data?.message ?? 'An error occurred',
            life: 5000,
        });
    }
}

function confirmConvert() {
    convertDialogVisible.value = true;
}

async function doConvert() {
    convertDialogVisible.value = false;
    try {
        const result = await store.convertToEstimate();
        toast.add({ severity: 'success', summary: 'Converted', detail: `Estimate ${result.estimateNumber} created`, life: 5000 });
    } catch (err: any) {
        toast.add({ severity: 'error', summary: 'Error', detail: err?.response?.data?.message ?? 'Conversion failed', life: 5000 });
    }
}

function spStatusSeverity(status: string) {
    const map: Record<string, string> = {
        Draft: '',
        Active: 'info',
        'Submitted for Approval': 'info',
        Approved: 'success',
        Converted: 'success',
        Archived: 'secondary',
    };
    return map[status] ?? '';
}
</script>

<style scoped>
.sp-header {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 6px;
    padding: 10px 14px 8px;
    margin-bottom: 8px;
}

.sp-title-row {
    display: flex;
    align-items: center;
    gap: 6px;
    margin-bottom: 8px;
    font-size: 0.75rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--text-color-secondary);
}

.sp-title {
    color: var(--text-color);
}

.sp-hrow {
    display: flex;
    gap: 8px;
    margin-bottom: 5px;
}

.sp-hrow:last-child {
    margin-bottom: 0;
}

.sp-hfield {
    display: flex;
    flex-direction: column;
    gap: 2px;
    min-width: 0;
}

.sp-hfield label {
    font-size: 0.62rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.07em;
    color: var(--text-color-secondary);
    white-space: nowrap;
    padding-left: 1px;
}

.sp-hfield :deep(.p-inputtext),
.sp-hfield :deep(.p-dropdown .p-dropdown-label),
.sp-hfield :deep(.p-inputnumber-input) {
    padding: 0.28rem 0.5rem;
    font-size: 0.82rem;
}

.sp-hfield :deep(.p-dropdown),
.sp-hfield :deep(.p-inputnumber) {
    width: 100%;
}

/* ── Rate book picker (shared with EstimateFormView) ─────────────────────── */
.rate-book-picker-list {
    max-height: 400px;
    overflow-y: auto;
    border: 1px solid var(--surface-border);
    border-radius: 5px;
}

.rb-row {
    display: flex;
    flex-direction: column;
    gap: 3px;
    padding: 10px 14px;
    cursor: pointer;
    border-bottom: 1px solid var(--surface-border);
    transition: background 0.12s;
}

.rb-row:last-child { border-bottom: none; }
.rb-row:hover { background: var(--surface-hover); }

.rb-name {
    font-size: 0.88rem;
    font-weight: 600;
    color: var(--text-color);
}

.rb-meta {
    display: flex;
    align-items: center;
    gap: 10px;
    font-size: 0.75rem;
    color: var(--text-color-secondary);
}

.rb-tag { font-size: 0.62rem; padding: 1px 5px; }
.rb-count { margin-left: auto; font-family: monospace; }

/* ── AI FAB ──────────────────────────────────────────────────────────────── */
.ai-fab {
    position: fixed;
    bottom: 24px;
    right: 24px;
    width: 52px;
    height: 52px;
    border-radius: 50%;
    border: none;
    background: linear-gradient(135deg, #6366f1, #8b5cf6);
    color: #fff;
    font-size: 1.3rem;
    cursor: pointer;
    box-shadow: 0 4px 16px rgba(99, 102, 241, 0.45);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 1100;
    transition: transform 0.15s, box-shadow 0.15s;
}
.ai-fab:hover {
    transform: scale(1.08);
    box-shadow: 0 6px 20px rgba(99, 102, 241, 0.6);
}

/* ── Plan Summary ────────────────────────────────────────────────────────── */
.sp-summary {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 6px;
    padding: 12px 16px;
}

.sp-summary-header {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 12px;
    font-size: 0.75rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--text-color);
}

.sp-summary-kpis {
    display: flex;
    gap: 40px;
    flex-wrap: wrap;
    align-items: flex-end;
}

.sp-sum-kpi {
    display: flex;
    flex-direction: column;
    gap: 3px;
}

.sp-sum-label {
    font-size: 0.6rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.07em;
    color: var(--text-color-secondary);
}

.sp-sum-value {
    font-size: 1.3rem;
    font-weight: 700;
    color: var(--text-color);
}

.sp-margin-tag { font-size: 1rem; font-weight: 700; }

.rb-active { background: var(--primary-color-text-focus, rgba(99,102,241,0.1)) !important; }
</style>
