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
                    v-if="!isNew && !store.isConverted"
                    label="Convert to Estimate"
                    icon="pi pi-arrow-right"
                    severity="success"
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
                        <Dropdown
                            v-model="store.header.status"
                            :options="statusOptions"
                            class="w-full"
                            :disabled="store.isConverted"
                            data-testid="sp-status"
                            @change="store.markDirty()"
                        />
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
            />
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
import { useRoute, useRouter } from 'vue-router';
import { useToast } from 'primevue/usetoast';
import { useStaffingStore } from '../../../stores/staffingStore';
import { useApiStore } from '@/stores/apiStore';
import BasePageHeader from '@/components/layout/BasePageHeader.vue';
import BaseButtonCancel from '@/components/buttons/BaseButtonCancel.vue';
import LaborGrid from '../../estimate-form/components/LaborGrid.vue';

const route = useRoute();
const router = useRouter();
const toast = useToast();
const store = useStaffingStore();
const apiStore = useApiStore();

const convertDialogVisible = ref(false);

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
const statusOptions = ['Draft', 'Active', 'Approved'];
const otMethodOptions = [
    { label: 'Daily 8', value: 'daily8' },
    { label: 'Weekly 40', value: 'weekly40' },
    { label: 'Daily 8 + Weekly 40', value: 'daily8_weekly40' },
    { label: 'California', value: 'california' },
    { label: 'Straight Time', value: 'straight' },
];

const dtWeekendsOptions = [
    { label: 'No', value: false },
    { label: 'Yes', value: true },
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
</style>
