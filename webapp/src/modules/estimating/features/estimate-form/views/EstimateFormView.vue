<template>
    <div class="estimate-form-view">
        <BasePageHeader
            icon="pi pi-file-edit"
            :title="isNew ? 'New Estimate' : (store.header.estimateNumber ?? 'Estimate')"
            :subtitle="isNew ? 'Create a new estimate' : store.header.name"
        >
            <div class="flex gap-2 align-items-center">
                <Tag v-if="store.isDirty" value="Unsaved changes" severity="warning" />
                <Button
                    label="Review with AI"
                    icon="pi pi-sparkles"
                    severity="secondary"
                    outlined
                    @click="reviewWithAi"
                />
                <Button
                    label="Save"
                    icon="pi pi-save"
                    :loading="store.isSaving"
                    @click="saveEstimate"
                />
                <Button
                    v-if="!isNew"
                    label="Export PDF"
                    icon="pi pi-file-pdf"
                    severity="secondary"
                    outlined
                    :loading="exportingPdf"
                    @click="exportProposal"
                />
                <Button
                    v-if="!isNew"
                    label="Save + Revision"
                    icon="pi pi-history"
                    outlined
                    @click="showRevisionDialog = true"
                />
                <Button
                    v-if="!isNew"
                    label="Revisions"
                    icon="pi pi-history"
                    severity="secondary"
                    outlined
                    @click="revisionDrawerVisible = true"
                />
                <BaseButtonCancel label="Back" icon="pi pi-arrow-left" @click="router.push('/estimating/estimates')" />
            </div>
        </BasePageHeader>

        <div v-if="store.isLoading" class="flex justify-content-center py-8">
            <ProgressSpinner />
        </div>

        <div v-else class="flex flex-col gap-2">
            <!-- 1. Header (always visible) -->
            <EstimateHeader
                v-model="store.header"
                @update:modelValue="store.markDirty()"
                @clientFilled="onClientFilled"
            />

            <!-- 2. Labor -->
            <div class="collapsible-section">
                <div class="section-toggle" @click="collapsed.labor = !collapsed.labor">
                    <i class="pi pi-chevron-down toggle-icon" :class="{ rotated: collapsed.labor }" />
                    <i class="pi pi-users section-icon" />
                    <span class="section-label">Labor</span>
                    <span class="section-badge">{{ store.laborRows.length }} rows</span>
                </div>
                <div v-show="!collapsed.labor" class="section-body">
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
                        @change="onLaborChange"
                        @loadCrew="crewDialogVisible = true"
                        @openRateBook="rateBookDialogVisible = true"
                        @clearRateBook="store.clearRateBook()"
                    />
                </div>
            </div>

            <!-- 3. Equipment -->
            <div class="collapsible-section">
                <div class="section-toggle" @click="collapsed.equipment = !collapsed.equipment">
                    <i class="pi pi-chevron-down toggle-icon" :class="{ rotated: collapsed.equipment }" />
                    <i class="pi pi-wrench section-icon" />
                    <span class="section-label">Equipment</span>
                    <span class="section-badge">{{ store.equipmentRows.length }} rows</span>
                </div>
                <div v-show="!collapsed.equipment" class="section-body">
                    <EquipmentSection
                        v-model:rows="store.equipmentRows"
                        :defaultDays="store.header.days"
                        @change="store.recalcSummary(); store.markDirty()"
                    />
                </div>
            </div>

            <!-- 4. Expenses -->
            <div class="collapsible-section">
                <div class="section-toggle" @click="collapsed.expenses = !collapsed.expenses">
                    <i class="pi pi-chevron-down toggle-icon" :class="{ rotated: collapsed.expenses }" />
                    <i class="pi pi-receipt section-icon" />
                    <span class="section-label">Expenses / Per Diem</span>
                    <span class="section-badge">{{ store.expenseRows.length }} rows</span>
                </div>
                <div v-show="!collapsed.expenses" class="section-body">
                    <ExpenseSection
                        v-model:rows="store.expenseRows"
                        :defaultDays="store.header.days"
                        @change="store.recalcSummary(); store.markDirty()"
                    />
                </div>
            </div>

            <!-- 5. Job Cost Analysis (Internal Only) -->
            <div class="collapsible-section">
                <div class="section-toggle" @click="collapsed.jobCost = !collapsed.jobCost">
                    <i class="pi pi-chevron-down toggle-icon" :class="{ rotated: collapsed.jobCost }" />
                    <i class="pi pi-chart-bar section-icon" />
                    <span class="section-label">Job Cost Analysis</span>
                    <span class="section-badge internal-badge">INTERNAL</span>
                </div>
                <div v-show="!collapsed.jobCost" class="section-body">
                    <JobCostAnalysis
                        :laborRows="store.laborRows"
                        :hoursPerShift="store.header.hoursPerShift"
                        @update:internalCost="onInternalCostUpdate"
                    />
                </div>
            </div>

            <!-- 6. Summary -->
            <div class="collapsible-section">
                <div class="section-toggle" @click="collapsed.summary = !collapsed.summary">
                    <i class="pi pi-chevron-down toggle-icon" :class="{ rotated: collapsed.summary }" />
                    <i class="pi pi-calculator section-icon" />
                    <span class="section-label">Summary</span>
                    <span v-if="collapsed.summary" class="section-badge grand-total">Grand Total {{ fmtCurrency(store.summary.grandTotal) }}</span>
                </div>
                <div v-show="!collapsed.summary" class="section-body">
                    <EstimateSummary
                        v-model="store.summary"
                        @update:modelValue="store.markDirty()"
                    />
                </div>
            </div>
        </div>

        <!-- Save + Revision dialog -->
        <Dialog v-model:visible="showRevisionDialog" header="Save with Revision" modal style="width:400px">
            <div class="flex flex-col gap-3 pt-2">
                <p class="text-slate-400 text-sm">Add an optional note for this revision snapshot.</p>
                <BaseFormField label="Revision Note (optional)">
                    <InputText v-model="revisionNote" placeholder="e.g. Revised labor rates" class="w-full" />
                </BaseFormField>
            </div>
            <template #footer>
                <Button label="Cancel" text @click="showRevisionDialog = false" />
                <Button label="Save + Snapshot" icon="pi pi-save" :loading="store.isSaving" @click="saveWithRevision" />
            </template>
        </Dialog>

        <!-- Revision drawer -->
        <RevisionDrawer
            v-model:visible="revisionDrawerVisible"
            :estimateId="store.header.estimateId"
            @restored="onRevisionRestored"
        />

        <!-- Rate book picker -->
        <Dialog v-model:visible="rateBookDialogVisible" header="Load Rate Book" modal style="width: 520px;">
            <div v-if="rateBookListLoading" class="flex justify-content-center py-6">
                <ProgressSpinner />
            </div>
            <div v-else-if="rateBookList.length === 0" class="text-center py-4 text-slate-400 text-sm">
                No rate books found. Create one in the Rate Books section.
            </div>
            <div v-else class="rate-book-picker-list">
                <div
                    v-for="rb in rateBookList"
                    :key="rb.rateBookId"
                    class="rb-row"
                    :class="{ 'rb-active': store.header.rateBookId === rb.rateBookId }"
                    @click="pickRateBook(rb)"
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
                <Button label="Cancel" text @click="rateBookDialogVisible = false" />
            </template>
        </Dialog>

        <!-- Load Crew Template dialog -->
        <Dialog v-model:visible="crewDialogVisible" header="Load Crew Template" modal style="width: 520px;">
            <div v-if="crewListLoading" class="flex justify-content-center py-6">
                <ProgressSpinner />
            </div>
            <div v-else-if="crewList.length === 0" class="text-center py-4 text-slate-400 text-sm">
                No crew templates found. Create one in the Crew Templates section.
            </div>
            <div v-else class="rate-book-picker-list">
                <div
                    v-for="tmpl in crewList"
                    :key="tmpl.crewTemplateId"
                    class="rb-row"
                    :class="{ 'rb-active': selectedCrewId === tmpl.crewTemplateId }"
                    @click="selectedCrewId = tmpl.crewTemplateId"
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
                <Button label="Cancel" text @click="crewDialogVisible = false" />
                <Button label="Add to Labor" icon="pi pi-plus" :disabled="!selectedCrewId || crewApplying" :loading="crewApplying" @click="applyCrewTemplate" />
            </template>
        </Dialog>

        <Toast />
        <ConfirmDialog />

    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useToast } from 'primevue/usetoast';
import { useEstimateStore } from '../../../stores/estimateStore';
import { useAiChatStore } from '../../../stores/aiChatStore';
import { useUserStore } from '@/stores/userStore';
import { useApiStore } from '@/stores/apiStore';
import BasePageHeader from '@/components/layout/BasePageHeader.vue';
import BaseButtonCancel from '@/components/buttons/BaseButtonCancel.vue';
import BaseFormField from '@/components/forms/BaseFormField.vue';
import EstimateHeader from '../components/EstimateHeader.vue';
import LaborGrid from '../components/LaborGrid.vue';
import EquipmentSection from '../components/EquipmentSection.vue';
import ExpenseSection from '../components/ExpenseSection.vue';
import EstimateSummary from '../components/EstimateSummary.vue';
import JobCostAnalysis from '../components/JobCostAnalysis.vue';
import RevisionDrawer from '../components/RevisionDrawer.vue';

const route = useRoute();
const router = useRouter();
const toast = useToast();
const store = useEstimateStore();
const apiStore = useApiStore();
const aiStore = useAiChatStore();
const userStore = useUserStore();

const hasTriggeredRateBookSuggestion = ref(false);

function buildAiContext() {
    const h = store.header;
    return {
        companyCode: userStore.companyCode,
        currentPage: 'estimate',
        currentEstimateId: typeof h.estimateId === 'number' ? h.estimateId : undefined,
        headerSnapshot: {
            name: h.name ?? undefined,
            client: h.client ?? undefined,
            city: h.city ?? undefined,
            state: h.state ?? undefined,
            startDate: h.startDate ?? undefined,
            endDate: h.endDate ?? undefined,
            days: h.days ?? undefined,
            shift: h.shift ?? undefined,
            jobType: h.jobType ?? undefined,
        },
        rateBookName: store.rateBookName ?? undefined,
        currentRateBookId: store.header.rateBookId ?? undefined,
        laborRows: store.laborRows.map(r => ({
            position: r.position,
            shift: r.shift,
            stRate: Number(r.billStRate),
            otRate: Number(r.billOtRate),
            dtRate: Number(r.billDtRate),
            stHours: Number(r.stHours),
            otHours: Number(r.otHours),
            subtotal: Number(r.subtotal),
        })),
    };
}

function reviewWithAi() {
    aiStore.open();
    setTimeout(() => {
        aiStore.sendMessage(
            'Review this estimate: check all labor rows for rate anomalies vs historical benchmarks, flag missing standard positions for this job type, and note any concerns.',
            buildAiContext()
        );
    }, 150);
}

function onClientFilled() {
    if (hasTriggeredRateBookSuggestion.value) return;
    const h = store.header;
    if (!h.client?.trim()) return;
    hasTriggeredRateBookSuggestion.value = true;
    const loc = [h.city, h.state].filter(Boolean).join(', ');
    const query = loc
        ? `Suggest a rate book for ${h.client} in ${loc}`
        : `Do we have a rate book for ${h.client}?`;
    aiStore.open();
    setTimeout(() => {
        aiStore.sendMessage(query, {
            companyCode: userStore.companyCode,
            currentPage: 'estimate',
            currentEstimateId: typeof store.header.estimateId === 'number' ? store.header.estimateId : undefined,
            headerSnapshot: { client: h.client, city: h.city ?? undefined, state: h.state ?? undefined },
        });
    }, 150);
}

const isNew = computed(() => !route.params.id || route.params.id === 'new');
const showRevisionDialog = ref(false);
const revisionNote = ref('');
const revisionDrawerVisible = ref(false);
const exportingPdf = ref(false);

// Rate book picker
const rateBookDialogVisible = ref(false);
const rateBookListLoading = ref(false);
const rateBookList = ref<any[]>([]);

watch(() => route.params.id, () => { hasTriggeredRateBookSuggestion.value = false; });

watch(rateBookDialogVisible, async (open) => {
    if (!open || rateBookList.value.length) return;
    rateBookListLoading.value = true;
    try {
        const { data } = await apiStore.api.get('/api/v1/rate-books');
        rateBookList.value = data;
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load rate books', life: 4000 });
    } finally {
        rateBookListLoading.value = false;
    }
});

async function pickRateBook(rb: any) {
    rateBookDialogVisible.value = false;
    try {
        await store.loadRateBook(rb.rateBookId);
        toast.add({ severity: 'success', summary: 'Rate Book Loaded', detail: rb.name, life: 3000 });
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load rate book', life: 4000 });
    }
}

// Crew template picker
const crewDialogVisible = ref(false);
const crewListLoading = ref(false);
const crewList = ref<any[]>([]);
const selectedCrewId = ref<number | null>(null);
const crewApplying = ref(false);

watch(crewDialogVisible, async (open) => {
    if (!open) { selectedCrewId.value = null; return; }
    if (crewList.value.length) return;
    crewListLoading.value = true;
    try {
        const { data } = await apiStore.api.get('/api/v1/crew-templates');
        crewList.value = data;
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load crew templates', life: 4000 });
    } finally {
        crewListLoading.value = false;
    }
});

async function applyCrewTemplate() {
    if (!selectedCrewId.value) return;
    crewApplying.value = true;
    try {
        const { data } = await apiStore.api.get(`/api/v1/crew-templates/${selectedCrewId.value}`);
        const rateMap = new Map<string, any>();
        for (const r of store.rateBookRates ?? []) {
            rateMap.set(r.position.toLowerCase(), r);
        }

        const startDate = store.header.startDate;
        const endDate = store.header.endDate;

        function buildSchedule(): string {
            if (!startDate || !endDate) return '{}';
            const sched: Record<string, number> = {};
            const cur = new Date(startDate + 'T12:00:00');
            const end = new Date(endDate + 'T12:00:00');
            while (cur <= end) {
                sched[cur.toISOString().slice(0, 10)] = 1;
                cur.setDate(cur.getDate() + 1);
            }
            return JSON.stringify(sched);
        }

        const newRows: any[] = [];
        for (const row of data.rows) {
            const qty = row.qty > 0 ? row.qty : 1;
            const rate = rateMap.get(row.position?.toLowerCase() ?? '');
            const rowShift = store.header.shift === 'Both' ? 'Day' : (store.header.shift ?? 'Day');
            for (let i = 0; i < qty; i++) {
                newRows.push({
                    position: row.position,
                    laborType: row.laborType ?? 'Direct',
                    shift: row.shift ?? rowShift,
                    craftCode: row.craftCode ?? rate?.craftCode ?? null,
                    navCode: rate?.navCode ?? null,
                    billStRate: rate?.stRate ?? 0,
                    billOtRate: rate?.otRate ?? 0,
                    billDtRate: rate?.dtRate ?? 0,
                    scheduleJson: buildSchedule(),
                    stHours: 0,
                    otHours: 0,
                    dtHours: 0,
                    subtotal: 0,
                });
            }
        }

        store.laborRows = [...store.laborRows, ...newRows];
        store.recalcAllLaborRows();
        store.recalcSummary();
        store.markDirty();
        crewDialogVisible.value = false;
        toast.add({ severity: 'success', summary: 'Crew Loaded', detail: `Added ${newRows.length} row(s) from "${data.name}"`, life: 3000 });
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to apply crew template', life: 4000 });
    } finally {
        crewApplying.value = false;
    }
}

const collapsed = ref({ labor: false, equipment: false, expenses: false, jobCost: false, summary: false });

function onInternalCostUpdate(val: number) {
    store.summary.internalCostTotal = val;
    store.recalcSummary();
}

function fmtCurrency(val: number) {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(val ?? 0);
}

onMounted(async () => {
    store.reset();
    if (!isNew.value) {
        try {
            await store.fetchEstimate(Number(route.params.id));
        } catch {
            toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load estimate', life: 4000 });
        }
    }
});

onBeforeUnmount(() => {
    store.reset();
});

function onLaborChange() {
    store.recalcSummary();
    store.markDirty();
}

async function onRevisionRestored() {
    if (store.header.estimateId) {
        try {
            await store.fetchEstimate(store.header.estimateId);
            toast.add({ severity: 'success', summary: 'Restored', detail: 'Estimate reloaded from revision', life: 3000 });
        } catch {
            toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to reload estimate after restore', life: 4000 });
        }
    }
}

async function saveEstimate() {
    try {
        await store.saveEstimate();
        toast.add({ severity: 'success', summary: 'Saved', detail: 'Estimate saved successfully', life: 3000 });
        if (isNew.value) {
            router.replace(`/estimating/estimates/${store.header.estimateId}`);
        }
    } catch (err: any) {
        toast.add({ severity: 'error', summary: 'Save Failed', detail: err?.response?.data?.message ?? 'An error occurred', life: 5000 });
    }
}

async function exportProposal() {
    if (!store.header.estimateId) return;
    exportingPdf.value = true;
    try {
        const response = await apiStore.api.get(
            `/api/v1/estimates/${store.header.estimateId}/proposal.pdf`,
            { responseType: 'blob' },
        );
        const url = URL.createObjectURL(new Blob([response.data], { type: 'application/pdf' }));
        const a = document.createElement('a');
        a.href = url;
        a.download = `${store.header.estimateNumber ?? 'proposal'}.pdf`;
        a.click();
        URL.revokeObjectURL(url);
    } catch {
        toast.add({ severity: 'error', summary: 'Export Failed', detail: 'Could not generate proposal PDF', life: 4000 });
    } finally {
        exportingPdf.value = false;
    }
}

async function saveWithRevision() {
    showRevisionDialog.value = false;
    try {
        await store.saveEstimate(revisionNote.value || undefined);
        revisionNote.value = '';
        toast.add({ severity: 'success', summary: 'Saved', detail: 'Estimate saved with revision snapshot', life: 3000 });
        if (isNew.value) {
            router.replace(`/estimating/estimates/${store.header.estimateId}`);
        }
    } catch (err: any) {
        toast.add({ severity: 'error', summary: 'Save Failed', detail: err?.response?.data?.message ?? 'An error occurred', life: 5000 });
    }
}
</script>

<style scoped>
.collapsible-section {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 6px;
    overflow: hidden;
}

.section-toggle {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 7px 12px;
    cursor: pointer;
    user-select: none;
    border-bottom: 1px solid var(--surface-border);
    background: var(--surface-section);
    transition: background 0.15s;
}

.section-toggle:hover {
    background: var(--surface-hover);
}

.toggle-icon {
    font-size: 0.7rem;
    color: var(--text-color-secondary);
    transition: transform 0.2s;
}

.toggle-icon.rotated {
    transform: rotate(-90deg);
}

.section-icon {
    font-size: 0.85rem;
    color: var(--primary-color);
}

.section-label {
    font-size: 0.78rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: var(--text-color);
}

.section-badge {
    font-size: 0.7rem;
    color: var(--text-color-secondary);
    background: var(--surface-ground);
    border: 1px solid var(--surface-border);
    border-radius: 10px;
    padding: 1px 8px;
}

.section-badge.internal-badge {
    color: var(--red-400);
    border-color: color-mix(in srgb, var(--red-400) 40%, transparent);
    background: color-mix(in srgb, var(--red-400) 8%, transparent);
    font-weight: 700;
}

.section-badge.grand-total {
    color: var(--primary-color);
    font-weight: 700;
    border-color: var(--primary-color);
}

.section-body {
    padding: 0;
}

.section-body :deep(.labor-grid),
.section-body :deep(.equipment-section),
.section-body :deep(.expense-section),
.section-body :deep(.estimate-summary) {
    border: none;
    border-radius: 0;
}

/* ── Rate book picker ─────────────────────────────────────────────────────── */
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
.rb-row.rb-active { background: color-mix(in srgb, var(--primary-color) 8%, transparent); }

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

</style>
