<template>
    <div class="estimate-header">
        <div class="header-title-row">
            <i class="pi pi-file-edit" />
            <span class="header-title">Job Information</span>
            <Tag v-if="modelValue.status" :value="modelValue.status" :severity="statusSeverity(modelValue.status)" class="ml-2" />
            <div class="estimate-number-block">
                <Tag v-if="modelValue.estimateNumber" :value="modelValue.estimateNumber" class="font-mono" />
                <Tag v-if="modelValue.estimateId" :value="modelValue.revisionNumber != null ? `Rev ${modelValue.revisionNumber}` : 'Rev 0'" :severity="modelValue.revisionNumber != null ? 'warning' : 'secondary'" class="rev-badge" />
            </div>
            <span v-if="modelValue.staffingPlanNumber" class="sp-ref">
                Staff Plan#: {{ modelValue.staffingPlanNumber }}
            </span>
        </div>

        <!-- Row 1: Job Name, Client, Client Code, Job Type, MSA #, Job Letter -->
        <div class="hrow">
            <div class="hfield" style="flex:3">
                <label>JOB NAME *</label>
                <InputText v-model="form.name" placeholder="Enter job name..." class="w-full" @input="emit" />
            </div>
            <div class="hfield" style="flex:2.5">
                <label>CLIENT *</label>
                <InputText v-model="form.client" placeholder="Client company..." class="w-full" @input="emit" @blur="onClientBlur" />
            </div>
            <div class="hfield" style="flex:1">
                <label>CLIENT CODE</label>
                <InputText v-model="form.clientCode" placeholder="BP" maxlength="10" class="w-full" @input="emit" />
            </div>
            <div class="hfield" style="flex:1.5">
                <label>JOB TYPE</label>
                <Dropdown v-model="form.jobType" :options="jobTypeOptions" placeholder="Select..." showClear class="w-full" @change="emit" />
            </div>
            <div class="hfield" style="flex:1.2">
                <label>MSA #</label>
                <InputText v-model="form.msaNumber" placeholder="MSA-XXXX" class="w-full" @input="emit" />
            </div>
            <div v-if="jobLetterVisible" class="hfield" style="flex:0.8">
                <label>JOB LETTER</label>
                <Dropdown
                    v-if="jobLetterOptions.length > 1"
                    v-model="form.jobLetter"
                    :options="jobLetterOptions"
                    placeholder="H"
                    class="w-full"
                    @change="emit"
                />
                <InputText
                    v-else
                    v-model="form.jobLetter"
                    :disabled="jobLetterOptions.length === 1"
                    class="w-full"
                    @input="emit"
                />
            </div>
        </div>

        <!-- Row 2: Branch, City, State, Site, Start, End, Days -->
        <div class="hrow">
            <div class="hfield" style="flex:1.2">
                <label>BRANCH</label>
                <InputText v-model="form.branch" placeholder="Branch" class="w-full" @input="emit" />
            </div>
            <div class="hfield" style="flex:2">
                <label>CITY</label>
                <InputText v-model="form.city" placeholder="City..." class="w-full" @input="emit" />
            </div>
            <div class="hfield" style="flex:0.7">
                <label>STATE</label>
                <InputText v-model="form.state" placeholder="--" maxlength="2" class="w-full" @input="emit" />
            </div>
            <div class="hfield" style="flex:2">
                <label>SITE / FACILITY</label>
                <InputText v-model="form.site" placeholder="Site/facility..." class="w-full" @input="emit" />
            </div>
            <div class="hfield" style="flex:1.3">
                <label>START DATE</label>
                <InputText v-model="form.startDate" type="date" class="w-full" @input="onDatesChange" />
            </div>
            <div class="hfield" style="flex:1.3">
                <label>END DATE</label>
                <InputText v-model="form.endDate" type="date" class="w-full" @input="onDatesChange" />
            </div>
            <div class="hfield" style="flex:0.6">
                <label>DAYS</label>
                <InputNumber v-model="form.days" :min="0" :max="9999" class="w-full" inputClass="w-full" @input="emit" />
            </div>
        </div>

        <!-- Row 3: VP, Director, Region -->
        <div class="hrow">
            <div class="hfield" style="flex:3">
                <label>VP</label>
                <Dropdown v-model="form.vp" :options="vpOptions" placeholder="Select VP..." showClear class="w-full" @change="emit" />
            </div>
            <div class="hfield" style="flex:3.5">
                <label>DIRECTOR</label>
                <InputText v-model="form.director" placeholder="Director name..." class="w-full" @input="emit" />
            </div>
            <div class="hfield" style="flex:2.5">
                <label>REGION</label>
                <Dropdown v-model="form.region" :options="regionOptions" placeholder="Select Region..." showClear class="w-full" @change="emit" />
            </div>
        </div>

        <!-- Row 4: Shift, Hrs/Shift, OT Method, DT W/E, Status, Confidence -->
        <div class="hrow">
            <div class="hfield" style="flex:1">
                <label>SHIFT</label>
                <Dropdown v-model="form.shift" :options="shiftOptions" class="w-full" @change="emit" />
            </div>
            <div class="hfield" style="flex:0.9">
                <label>HRS/SHIFT</label>
                <InputNumber v-model="form.hoursPerShift" :min="1" :max="24" :maxFractionDigits="1" class="w-full" inputClass="w-full" @input="emit" />
            </div>
            <div class="hfield" style="flex:2.5">
                <label>OVERTIME RULES</label>
                <Dropdown v-model="form.otMethod" :options="otMethodOptions" optionLabel="label" optionValue="value" class="w-full" @change="emit" />
            </div>
            <div class="hfield" style="flex:0.8">
                <label>DT W/E</label>
                <Dropdown v-model="form.dtWeekends" :options="dtWeekendsOptions" optionLabel="label" optionValue="value" class="w-full" @change="emit" />
            </div>
            <div class="hfield" style="flex:1.2">
                <label>STATUS</label>
                <Dropdown v-model="form.status" :options="statusOptions" class="w-full" @change="onStatusChange" />
            </div>
            <div class="hfield" style="flex:1">
                <label>CONFIDENCE (0-100)</label>
                <InputNumber v-model="form.confidencePct" :min="0" :max="100" class="w-full" inputClass="w-full" @input="emit" />
            </div>
            <div v-if="form.status === 'Lost' && form.lostReason" class="hfield" style="flex:2">
                <label>LOST REASON</label>
                <div class="lost-reason-display">
                    <Tag :value="form.lostReason" severity="danger" />
                    <span v-if="form.lostNotes" class="lost-notes-text">{{ form.lostNotes }}</span>
                    <Button icon="pi pi-pencil" text size="small" title="Edit lost reason" @click="showLostDialog = true" />
                </div>
            </div>
        </div>
    </div>

    <!-- Lost Reason Dialog -->
    <Dialog v-model:visible="showLostDialog" header="Mark as Lost" modal style="width: 420px" :closable="false">
        <div class="flex flex-col gap-3 pt-2">
            <p class="text-slate-400 text-sm">Please provide a reason for marking this estimate as lost.</p>
            <div class="flex flex-col gap-1">
                <label class="text-xs font-bold uppercase text-slate-400">Reason *</label>
                <Dropdown v-model="lostReasonDraft" :options="lostReasonOptions" placeholder="Select reason..." class="w-full" />
            </div>
            <div class="flex flex-col gap-1">
                <label class="text-xs font-bold uppercase text-slate-400">Notes (optional)</label>
                <Textarea v-model="lostNotesDraft" rows="3" placeholder="Additional context..." class="w-full" autoResize />
            </div>
        </div>
        <template #footer>
            <Button label="Cancel" text @click="cancelLostDialog" />
            <Button label="Confirm" icon="pi pi-check" severity="danger" :disabled="!lostReasonDraft" @click="confirmLostDialog" />
        </template>
    </Dialog>
</template>

<script setup lang="ts">
import { reactive, ref, computed, watch } from 'vue';
import type { EstimateHeader } from '../../../stores/estimateStore';
import { useUserStore } from '@/stores/userStore';

const props = defineProps<{ modelValue: EstimateHeader }>();
const emits = defineEmits<{
    (e: 'update:modelValue', v: EstimateHeader): void;
    (e: 'clientFilled'): void;
}>();

const userStore = useUserStore();
const form = reactive<EstimateHeader>({ ...props.modelValue });

// ── Job letter config by company ─────────────────────────────────────────────

const JOB_LETTER_MAP: Record<string, string[]> = {
    ETS: ['H', 'B'],    // dropdown — H or B
    STS: ['S'],          // fixed
    STG: ['G'],          // fixed
    // CSL and others: no letter prefix → field hidden
};

const jobLetterOptions = computed(() => JOB_LETTER_MAP[userStore.companyCode] ?? []);
const jobLetterVisible = computed(() => jobLetterOptions.value.length > 0);

// Auto-set default job letter when company changes or on new estimate
watch(
    [() => userStore.companyCode, () => props.modelValue.estimateId],
    ([code]) => {
        const opts = JOB_LETTER_MAP[code];
        if (opts?.length && !form.jobLetter) {
            form.jobLetter = opts[0];
            emit();
        } else if (!opts) {
            form.jobLetter = undefined;
            emit();
        }
    },
    { immediate: true },
);

watch(() => props.modelValue, (v) => Object.assign(form, v), { deep: true });

function emit() { emits('update:modelValue', { ...form }); }

function onClientBlur() {
    if (form.client?.trim()) emits('clientFilled');
}

// ── Lost reason dialog ────────────────────────────────────────────────────────
const showLostDialog = ref(false);
const lostReasonDraft = ref('');
const lostNotesDraft = ref('');
let prevStatus = '';

function onStatusChange() {
    if (form.status === 'Lost' && !form.lostReason) {
        prevStatus = form.status;
        lostReasonDraft.value = form.lostReason ?? '';
        lostNotesDraft.value = (form as any).lostNotes ?? '';
        showLostDialog.value = true;
    } else {
        emit();
    }
}

function confirmLostDialog() {
    form.lostReason = lostReasonDraft.value;
    (form as any).lostNotes = lostNotesDraft.value;
    showLostDialog.value = false;
    lostReasonDraft.value = '';
    lostNotesDraft.value = '';
    emit();
}

function cancelLostDialog() {
    form.status = prevStatus || 'Draft';
    showLostDialog.value = false;
    lostReasonDraft.value = '';
    lostNotesDraft.value = '';
    emit();
}

const lostReasonOptions = ['Pricing', 'Scope', 'Competitor', 'No Decision', 'Other'];

function onDatesChange() {
    if (form.startDate && form.endDate) {
        const start = new Date(form.startDate);
        const end = new Date(form.endDate);
        const diff = Math.round((end.getTime() - start.getTime()) / 86400000) + 1;
        if (diff > 0) form.days = diff;
    }
    emit();
}

function statusSeverity(status: string) {
    const map: Record<string, string> = { Draft: '', Pending: 'warning', Awarded: 'success', Lost: 'danger', Canceled: 'warning' };
    return map[status] ?? '';
}

const vpOptions     = ['David Torres', 'Mark Ellis', 'Mike Rodriguez', 'Carlos Vega'];
const regionOptions = ['Gulf', 'West', 'South TX', 'Mid-Continent', 'Southeast', 'Northeast'];
const jobTypeOptions = ['Lump Sum', 'T&M', 'Maintenance', 'Installation', 'Consulting', 'Repair', 'Other'];
const shiftOptions = ['Day', 'Night', 'Both'];
const statusOptions = ['Draft', 'Pending', 'Awarded', 'Lost', 'Canceled'];
const dtWeekendsOptions = [
    { label: 'No', value: false },
    { label: 'Yes', value: true },
];
const otMethodOptions = [
    { label: 'Daily 8 + Weekly 40', value: 'daily8_weekly40' },
    { label: 'Daily 8 (OT > 8 hrs/day)', value: 'daily8' },
    { label: 'Weekly 40 (OT > 40 hrs/wk)', value: 'weekly40' },
    { label: 'California (ST≤8, OT 8-12, DT>12)', value: 'california' },
    { label: 'Straight Time Only', value: 'straight' },
];
</script>

<style scoped>
.estimate-header {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 6px;
    padding: 10px 14px 8px;
    margin-bottom: 8px;
}

.header-title-row {
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

.header-title { color: var(--text-color); }

.estimate-number-block {
    display: flex;
    align-items: center;
    gap: 4px;
}

.rev-badge {
    font-size: 0.65rem;
}

.sp-ref {
    font-size: 0.68rem;
    color: #e74c3c;
    font-weight: 600;
    font-family: monospace;
    letter-spacing: 0.02em;
    margin-left: 2px;
}

.hrow {
    display: flex;
    gap: 8px;
    margin-bottom: 5px;
}
.hrow:last-child { margin-bottom: 0; }

.hfield {
    display: flex;
    flex-direction: column;
    gap: 2px;
    min-width: 0;
}

.hfield label {
    font-size: 0.62rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.07em;
    color: var(--text-color-secondary);
    white-space: nowrap;
    padding-left: 1px;
}

/* Make all inputs compact */
.hfield :deep(.p-inputtext),
.hfield :deep(.p-dropdown .p-dropdown-label),
.hfield :deep(.p-inputnumber-input) {
    padding: 0.28rem 0.5rem;
    font-size: 0.8rem;
    height: 30px;
}

.hfield :deep(.p-dropdown) {
    height: 30px;
}

.hfield :deep(.p-inputnumber) {
    width: 100%;
}

.lost-reason-display {
    display: flex; align-items: center; gap: 6px; flex-wrap: wrap;
}
.lost-notes-text {
    font-size: 0.75rem; color: var(--text-color-secondary); font-style: italic;
}
</style>
