<template>
    <div class="space-y-6">
        <BasePageHeader
            icon="pi pi-search"
            :title="investigation?.investigationNumber || 'Investigation'"
            :subtitle="headerSubtitle"
        >
            <Button label="Back" text icon="pi pi-arrow-left" @click="router.push('/safety-application/investigations')" />
            <template v-if="!loading && investigation">
                <template v-if="isEditMode">
                    <Button label="Cancel" text :disabled="saving" @click="cancelEdit" />
                    <Button label="Save Changes" icon="pi pi-check" :loading="saving" @click="saveChanges" />
                </template>
                <template v-else>
                    <Button
                        v-if="investigation.status === 'Assigned'"
                        label="Start Investigation"
                        icon="pi pi-play"
                        severity="info"
                        :loading="transitioning"
                        @click="doTransition('start')"
                    />
                    <Button
                        v-if="investigation.status === 'In Progress'"
                        label="Complete Investigation"
                        icon="pi pi-check-circle"
                        severity="success"
                        :loading="transitioning"
                        @click="doTransition('complete')"
                    />
                    <template v-if="investigation.incidentStatus?.toLowerCase() === 'in-review'">
                        <Button
                            label="Approve"
                            icon="pi pi-thumbs-up"
                            severity="success"
                            :loading="transitioning"
                            @click="doTransition('approve')"
                        />
                        <Button
                            label="Reject"
                            icon="pi pi-thumbs-down"
                            severity="danger"
                            :loading="transitioning"
                            @click="doTransition('reject')"
                        />
                    </template>
                    <Button label="Edit Investigation" icon="pi pi-pencil" @click="beginEdit" />
                </template>
            </template>
        </BasePageHeader>

        <div v-if="loading" class="flex justify-center py-12">
            <ProgressSpinner />
        </div>

        <template v-else-if="investigation">
            <!-- 1. Overview -->
            <Card class="border border-slate-700/70 bg-slate-800/70 shadow-lg shadow-slate-950/20">
                <template #title>Overview</template>
                <template #content>
                    <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                        <div>
                            <p class="mb-1 text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Related Incident</p>
                            <p class="text-base font-semibold text-white">{{ investigation.incidentNumber }}</p>
                            <p class="text-sm text-slate-400">{{ formatDateMMDDYYYY(investigation.incidentDate) }}</p>
                            <Button label="Open Incident" size="small" text icon="pi pi-external-link" class="!pl-0 !mt-1" @click="openIncident" />
                        </div>
                        <div>
                            <p class="mb-1 text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Owner</p>
                            <p class="text-sm text-white">{{ investigation.owner }}</p>
                            <p class="mt-2 text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Opened</p>
                            <p class="text-sm text-white">{{ formatDateMMDDYYYY(investigation.openDate) }}</p>
                        </div>
                        <div>
                            <p class="mb-1 text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Priority</p>
                            <Tag :value="investigation.priority" :severity="priorityTone(investigation.priority)" class="mb-3" />
                            <p class="mb-1 text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Due Date</p>
                            <p class="text-sm text-white">{{ formatDateMMDDYYYY(investigation.dueDate) }}</p>
                        </div>
                    </div>
                </template>
            </Card>

            <!-- 2. Involved Employees -->
            <Card class="border border-slate-700/70 bg-slate-800/70 shadow-lg shadow-slate-950/20">
                <template #title>
                    <div class="flex items-center justify-between">
                        <span>Involved Employees</span>
                        <Button
                            label="Add / Remove People"
                            icon="pi pi-users"
                            size="small"
                            severity="secondary"
                            :disabled="true"
                            v-tooltip.left="'Adding investigation-specific people requires a schema update — discuss with team first.'"
                        />
                    </div>
                </template>
                <template #content>
                    <div v-if="employees.length === 0" class="text-sm text-slate-400">
                        No employees recorded on this incident.
                    </div>
                    <div v-else class="overflow-x-auto">
                        <table class="w-full text-sm">
                            <thead>
                                <tr class="text-left text-xs uppercase tracking-[0.15em] text-slate-400 border-b border-slate-700">
                                    <th class="pb-2 pr-4 font-medium">Employee ID</th>
                                    <th class="pb-2 pr-4 font-medium">Employee Name</th>
                                    <th class="pb-2 pr-4 font-medium">Type / Role</th>
                                    <th class="pb-2 font-medium w-28">Hours Worked</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr
                                    v-for="emp in employees"
                                    :key="emp.id"
                                    class="border-b border-slate-800/60"
                                >
                                    <td class="py-2 pr-4 text-slate-300">{{ emp.employeeIdentifier || '—' }}</td>
                                    <td class="py-2 pr-4 text-slate-200 font-medium">{{ emp.employeeName || '—' }}</td>
                                    <td class="py-2 pr-4 text-slate-300">{{ emp.injuryTypeCode || '—' }}</td>
                                    <td class="py-2 text-slate-300">{{ emp.hoursWorked != null ? emp.hoursWorked : '—' }}</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </template>
            </Card>

            <!-- 3. Investigation Details -->
            <Card class="border border-slate-700/70 bg-slate-800/70 shadow-lg shadow-slate-950/20">
                <template #title>Investigation Details</template>
                <template #content>
                    <Textarea
                        v-if="isEditMode"
                        v-model="form.investigationDetails"
                        rows="6"
                        class="w-full"
                        placeholder="Describe the investigation findings..."
                    />
                    <p v-else class="text-sm text-slate-200 whitespace-pre-wrap">
                        {{ investigation.investigationDetails || 'No investigation details have been added yet.' }}
                    </p>
                </template>
            </Card>

            <!-- 4. Attachments Placeholder -->
            <Card class="border border-slate-700/50 bg-slate-800/40 shadow-lg shadow-slate-950/20 opacity-60">
                <template #title>
                    <div class="flex items-center gap-2">
                        <span>Attachments</span>
                        <span class="text-xs font-normal text-slate-500 tracking-widest uppercase">Coming Soon</span>
                    </div>
                </template>
                <template #content>
                    <p class="text-sm text-slate-500 italic">
                        Document attachments will be available here: Strongcard / FLHA, Third Party Data, Training Records.
                    </p>
                </template>
            </Card>

            <!-- 5. Incident Investigation Analysis (Unsafe Conditions, Unsafe Acts, Systemic Failures) -->
            <Card class="border border-slate-700/70 bg-slate-800/70 shadow-lg shadow-slate-950/20">
                <template #title>Incident Investigation Analysis</template>
                <template #content>
                    <div class="border border-slate-600 rounded-lg overflow-hidden text-sm">

                        <!-- Header row -->
                        <div class="grid grid-cols-[200px_1fr] bg-slate-700">
                            <div class="px-3 py-2 text-xs font-bold text-white uppercase tracking-wide border-r border-slate-600">
                                Analysis Type
                            </div>
                            <div class="px-3 py-2 text-xs font-bold text-white uppercase tracking-wide">
                                Choose Any That Apply
                            </div>
                        </div>

                        <!-- Unsafe Conditions -->
                        <div class="grid grid-cols-[200px_1fr] border-t border-slate-600">
                            <div class="px-3 py-3 font-semibold text-white border-r border-slate-600">
                                Unsafe Conditions
                                <p class="text-xs font-normal text-slate-400 mt-0.5">Environment</p>
                            </div>
                            <div class="px-3 py-3 flex flex-wrap gap-3">
                                <div
                                    v-for="opt in getOptionsByTypeCode('unsafeconditions')"
                                    :key="opt.id"
                                    class="flex items-center gap-2"
                                    :class="!isEditMode ? 'opacity-60' : ''"
                                >
                                    <Checkbox
                                        :inputId="`attr-${opt.id}`"
                                        :modelValue="selectedAttributeIds.includes(opt.id)"
                                        :binary="true"
                                        :disabled="!isEditMode"
                                        @update:modelValue="toggleAttribute(opt.id)"
                                    />
                                    <label :for="`attr-${opt.id}`" class="text-sm" :class="isEditMode ? 'cursor-pointer' : 'cursor-default'">{{ opt.name }}</label>
                                </div>
                            </div>
                        </div>

                        <!-- Unsafe Acts -->
                        <div class="grid grid-cols-[200px_1fr] border-t border-slate-600">
                            <div class="px-3 py-3 font-semibold text-white border-r border-slate-600">
                                Unsafe Acts
                                <p class="text-xs font-normal text-slate-400 mt-0.5">People</p>
                            </div>
                            <div class="px-3 py-3 flex flex-wrap gap-3">
                                <div
                                    v-for="opt in getOptionsByTypeCode('unsafeacts')"
                                    :key="opt.id"
                                    class="flex items-center gap-2"
                                    :class="!isEditMode ? 'opacity-60' : ''"
                                >
                                    <Checkbox
                                        :inputId="`attr-${opt.id}`"
                                        :modelValue="selectedAttributeIds.includes(opt.id)"
                                        :binary="true"
                                        :disabled="!isEditMode"
                                        @update:modelValue="toggleAttribute(opt.id)"
                                    />
                                    <label :for="`attr-${opt.id}`" class="text-sm" :class="isEditMode ? 'cursor-pointer' : 'cursor-default'">{{ opt.name }}</label>
                                </div>
                            </div>
                        </div>

                        <!-- Systemic Failures sub-header -->
                        <div class="border-t border-slate-600 bg-slate-700/50 px-3 py-2">
                            <p class="text-xs font-bold uppercase tracking-widest text-blue-300">Systemic Failures</p>
                            <p class="text-xs text-slate-400 mt-0.5">From the below list, choose all SHC Processes that contributed to this event.</p>
                        </div>

                        <!-- Job / System Factors -->
                        <div class="grid grid-cols-[200px_1fr] border-t border-slate-600">
                            <div class="px-3 py-3 font-semibold text-white border-r border-slate-600">
                                Job / System Factors
                            </div>
                            <div class="px-3 py-3 flex flex-wrap gap-3">
                                <div
                                    v-for="opt in getOptionsByTypeCode('jobfactors')"
                                    :key="opt.id"
                                    class="flex items-center gap-2"
                                    :class="!isEditMode ? 'opacity-60' : ''"
                                >
                                    <Checkbox
                                        :inputId="`attr-${opt.id}`"
                                        :modelValue="selectedAttributeIds.includes(opt.id)"
                                        :binary="true"
                                        :disabled="!isEditMode"
                                        @update:modelValue="toggleAttribute(opt.id)"
                                    />
                                    <label :for="`attr-${opt.id}`" class="text-sm" :class="isEditMode ? 'cursor-pointer' : 'cursor-default'">{{ opt.name }}</label>
                                </div>
                            </div>
                        </div>

                        <!-- Personnel Factors -->
                        <div class="grid grid-cols-[200px_1fr] border-t border-slate-600">
                            <div class="px-3 py-3 font-semibold text-white border-r border-slate-600">
                                Personnel Factors
                            </div>
                            <div class="px-3 py-3 flex flex-wrap gap-3">
                                <div
                                    v-for="opt in getOptionsByTypeCode('personnelfactors')"
                                    :key="opt.id"
                                    class="flex items-center gap-2"
                                    :class="!isEditMode ? 'opacity-60' : ''"
                                >
                                    <Checkbox
                                        :inputId="`attr-${opt.id}`"
                                        :modelValue="selectedAttributeIds.includes(opt.id)"
                                        :binary="true"
                                        :disabled="!isEditMode"
                                        @update:modelValue="toggleAttribute(opt.id)"
                                    />
                                    <label :for="`attr-${opt.id}`" class="text-sm" :class="isEditMode ? 'cursor-pointer' : 'cursor-default'">{{ opt.name }}</label>
                                </div>
                            </div>
                        </div>

                    </div>
                </template>
            </Card>

            <!-- 6. Root Cause -->
            <Card class="border border-slate-700/70 bg-slate-800/70 shadow-lg shadow-slate-950/20">
                <template #title>Root Cause</template>
                <template #content>
                    <div class="border border-slate-600 rounded-lg overflow-hidden text-sm">

                        <!-- Header row -->
                        <div class="grid grid-cols-[200px_1fr] bg-slate-700">
                            <div class="px-3 py-2 text-xs font-bold text-white uppercase tracking-wide border-r border-slate-600">
                                Root Cause
                            </div>
                            <div class="px-3 py-2 text-xs font-bold text-white uppercase tracking-wide">
                                Choose Any That Apply
                            </div>
                        </div>

                        <div class="grid grid-cols-[200px_1fr] border-t border-slate-600">
                            <div class="px-3 py-3 font-semibold text-white border-r border-slate-600">Root Cause</div>
                            <div class="px-3 py-3 flex flex-wrap gap-3">
                                <div
                                    v-for="opt in getOptionsByTypeCode('rootcause')"
                                    :key="opt.id"
                                    class="flex items-center gap-2"
                                    :class="!isEditMode ? 'opacity-60' : ''"
                                >
                                    <Checkbox
                                        :inputId="`attr-${opt.id}`"
                                        :modelValue="selectedAttributeIds.includes(opt.id)"
                                        :binary="true"
                                        :disabled="!isEditMode"
                                        @update:modelValue="toggleAttribute(opt.id)"
                                    />
                                    <label :for="`attr-${opt.id}`" class="text-sm" :class="isEditMode ? 'cursor-pointer' : 'cursor-default'">{{ opt.name }}</label>
                                </div>
                            </div>
                        </div>

                    </div>
                </template>
            </Card>

            <!-- 7. Corrective Actions -->
            <Card class="border border-slate-700/70 bg-slate-800/70 shadow-lg shadow-slate-950/20">
                <template #title>
                    <div class="flex items-center justify-between">
                        <span>Corrective Actions</span>
                        <Button label="Add" icon="pi pi-plus" size="small" @click="openAddAction('Corrective')" />
                    </div>
                </template>
                <template #content>
                    <ActionsTable :actions="correctiveActions" @delete="deleteAction" />
                </template>
            </Card>

            <!-- 8. Preventative Actions -->
            <Card class="border border-slate-700/70 bg-slate-800/70 shadow-lg shadow-slate-950/20">
                <template #title>
                    <div class="flex items-center justify-between">
                        <span>Preventative Actions</span>
                        <Button label="Add" icon="pi pi-plus" size="small" @click="openAddAction('Preventative')" />
                    </div>
                </template>
                <template #content>
                    <ActionsTable :actions="preventativeActions" @delete="deleteAction" />
                </template>
            </Card>

            <!-- 9. Status & Classification -->
            <Card class="border border-slate-700/70 bg-slate-800/70 shadow-lg shadow-slate-950/20">
                <template #title>Status &amp; Classification</template>
                <template #content>
                    <div class="grid gap-4 md:grid-cols-3">
                        <BaseFormField label="Investigation Status">
                            <Dropdown
                                v-if="isEditMode"
                                v-model="form.status"
                                :options="statusOptions"
                                optionLabel="label"
                                optionValue="value"
                                class="w-full"
                            />
                            <Tag v-else :value="investigation.status" :severity="statusTone(investigation.status)" />
                        </BaseFormField>
                        <BaseFormField label="Review Status">
                            <p class="text-sm text-white">{{ investigation.reviewStatus || '—' }}</p>
                        </BaseFormField>
                        <BaseFormField label="Classification Status">
                            <InputText v-if="isEditMode" v-model="form.classificationStatus" class="w-full" />
                            <p v-else class="text-sm text-white">{{ investigation.classificationStatus || '—' }}</p>
                        </BaseFormField>
                    </div>
                </template>
            </Card>
        </template>

        <div v-else class="py-8 text-sm text-slate-300">Investigation not found.</div>

        <!-- Add Action Dialog -->
        <Dialog
            v-model:visible="showAddActionDialog"
            :header="`Add ${pendingActionType} Action`"
            modal
            :style="{ width: '480px' }"
            @hide="resetActionForm"
        >
            <div class="space-y-3 pt-2">
                <BaseFormField label="Description *">
                    <Textarea v-model="actionForm.actionDescription" rows="3" class="w-full" placeholder="Describe the action to be taken..." />
                </BaseFormField>
                <BaseFormField label="Assigned To">
                    <InputText v-model="actionForm.assignedTo" class="w-full" placeholder="Name or role" />
                </BaseFormField>
                <BaseFormField label="Due Date">
                    <Calendar v-model="actionForm.dueDateObj" showIcon class="w-full" dateFormat="mm/dd/yy" />
                </BaseFormField>
            </div>
            <template #footer>
                <Button label="Cancel" text :disabled="addingAction" @click="showAddActionDialog = false" />
                <Button
                    label="Add Action"
                    icon="pi pi-check"
                    :loading="addingAction"
                    :disabled="!actionForm.actionDescription.trim()"
                    @click="confirmAddAction"
                />
            </template>
        </Dialog>
    </div>
</template>

<script setup lang="ts">
import BasePageHeader from '@/components/layout/BasePageHeader.vue';
import BaseFormField from '@/components/forms/BaseFormField.vue';
import {
    deleteInvestigationAction,
    fetchIncidentEmployees,
    fetchInvestigationActions,
    fetchInvestigationAttributeOptions,
    fetchInvestigationAttributes,
    fetchInvestigationById,
    saveInvestigationAction,
    saveInvestigationAttributes,
    transitionInvestigation,
    updateInvestigation,
    type IncidentEmployeeRecord,
    type InvestigationActionItem,
    type InvestigationAttributeOption,
    type SafetyInvestigationRecord,
    type TransitionAction,
} from '@/modules/safety-application/features/dashboard/services/investigationService';
import { useApiStore } from '@/stores/apiStore';
import { formatDateMMDDYYYY } from '@/utils';
import Button from 'primevue/button';
import Calendar from 'primevue/calendar';
import Card from 'primevue/card';
import Checkbox from 'primevue/checkbox';
import Dialog from 'primevue/dialog';
import Dropdown from 'primevue/dropdown';
import InputText from 'primevue/inputtext';
import ProgressSpinner from 'primevue/progressspinner';
import Tag from 'primevue/tag';
import Textarea from 'primevue/textarea';
import { useToast } from 'primevue/usetoast';
import { computed, defineComponent, h, onMounted, reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';

const route = useRoute();
const router = useRouter();
const toast = useToast();
const apiStore = useApiStore();

const loading = ref(true);
const saving = ref(false);
const transitioning = ref(false);
const isEditMode = ref(false);
const investigation = ref<SafetyInvestigationRecord | null>(null);
const attributeOptions = ref<InvestigationAttributeOption[]>([]);
const selectedAttributeIds = ref<string[]>([]);
const actions = ref<InvestigationActionItem[]>([]);
const employees = ref<IncidentEmployeeRecord[]>([]);

// Edit form state
const form = reactive({
    status: '',
    reviewStatus: '',
    classificationStatus: '',
    investigationDetails: '',
});

// Add action dialog state
const showAddActionDialog = ref(false);
const pendingActionType = ref<'Corrective' | 'Preventative'>('Corrective');
const addingAction = ref(false);
const actionForm = reactive({
    actionDescription: '',
    assignedTo: '',
    dueDateObj: null as Date | null,
});

const headerSubtitle = computed(() => {
    if (!investigation.value) return 'Investigation details';
    if (isEditMode.value) return `Editing investigation for ${investigation.value.incidentNumber}`;
    return `Reviewing investigation for ${investigation.value.incidentNumber}`;
});

const statusOptions = [
    { label: 'Assigned', value: 'Assigned' },
    { label: 'In Progress', value: 'In Progress' },
    { label: 'Completed', value: 'Completed' },
    { label: 'Closed', value: 'Closed' },
];

const correctiveActions = computed(() =>
    actions.value.filter(a => a.actionType === 'Corrective')
);
const preventativeActions = computed(() =>
    actions.value.filter(a => a.actionType === 'Preventative')
);

function getOptionsByTypeCode(typeCode: string): InvestigationAttributeOption[] {
    return attributeOptions.value.filter(o => o.typeCode === typeCode);
}

onMounted(async () => {
    const id = String(route.params.id || '').trim();
    if (!id) {
        loading.value = false;
        return;
    }

    try {
        // Fetch investigation first so we have incidentId for employee lookup
        const inv = await fetchInvestigationById(apiStore.api, id);
        investigation.value = inv;

        const [opts, attrs, acts, emps] = await Promise.all([
            fetchInvestigationAttributeOptions(apiStore.api),
            fetchInvestigationAttributes(apiStore.api, id),
            fetchInvestigationActions(apiStore.api, id),
            fetchIncidentEmployees(apiStore.api, inv.incidentId),
        ]);

        attributeOptions.value = opts;
        selectedAttributeIds.value = attrs;
        actions.value = acts;
        employees.value = emps;

        if (route.query.mode === 'edit') {
            beginEdit();
        }
    } catch (error) {
        investigation.value = null;
        toast.add({
            severity: 'error',
            summary: 'Load Failed',
            detail: 'Failed to load investigation details.',
            life: 5000,
        });
        console.error(error);
    } finally {
        loading.value = false;
    }
});

function beginEdit() {
    if (!investigation.value) return;
    form.status = investigation.value.status;
    form.reviewStatus = investigation.value.reviewStatus;
    form.classificationStatus = investigation.value.classificationStatus;
    form.investigationDetails = investigation.value.investigationDetails;
    isEditMode.value = true;
}

function cancelEdit() {
    isEditMode.value = false;
    if (investigation.value) {
        form.status = investigation.value.status;
        form.reviewStatus = investigation.value.reviewStatus;
        form.classificationStatus = investigation.value.classificationStatus;
        form.investigationDetails = investigation.value.investigationDetails;
        void reloadAttributes();
    }
}

async function reloadAttributes() {
    const id = String(route.params.id || '').trim();
    if (!id) return;
    try {
        selectedAttributeIds.value = await fetchInvestigationAttributes(apiStore.api, id);
    } catch {
        // silently ignore
    }
}

async function saveChanges() {
    const id = String(route.params.id || '').trim();
    if (!id || !investigation.value) return;

    saving.value = true;
    try {
        const [updated] = await Promise.all([
            updateInvestigation(apiStore.api, id, {
                classificationStatus: form.classificationStatus,
                investigationDetails: form.investigationDetails,
            }),
            saveInvestigationAttributes(apiStore.api, id, selectedAttributeIds.value),
        ]);

        investigation.value = updated;
        isEditMode.value = false;

        toast.add({
            severity: 'success',
            summary: 'Saved',
            detail: 'Investigation updated successfully.',
            life: 3000,
        });
    } catch (error) {
        toast.add({
            severity: 'error',
            summary: 'Save Failed',
            detail: 'Could not save investigation. Please try again.',
            life: 5000,
        });
        console.error(error);
    } finally {
        saving.value = false;
    }
}

function toggleAttribute(id: string) {
    if (!isEditMode.value) return;
    const idx = selectedAttributeIds.value.indexOf(id);
    if (idx >= 0) {
        selectedAttributeIds.value.splice(idx, 1);
    } else {
        selectedAttributeIds.value.push(id);
    }
}

function openAddAction(type: 'Corrective' | 'Preventative') {
    pendingActionType.value = type;
    showAddActionDialog.value = true;
}

function resetActionForm() {
    actionForm.actionDescription = '';
    actionForm.assignedTo = '';
    actionForm.dueDateObj = null;
}

async function confirmAddAction() {
    const id = String(route.params.id || '').trim();
    if (!id || !actionForm.actionDescription.trim()) return;

    addingAction.value = true;
    try {
        let dueDateStr: string | undefined;
        if (actionForm.dueDateObj) {
            const d = actionForm.dueDateObj;
            const mm = String(d.getMonth() + 1).padStart(2, '0');
            const dd = String(d.getDate()).padStart(2, '0');
            const yyyy = d.getFullYear();
            dueDateStr = `${yyyy}-${mm}-${dd}`;
        }

        const created = await saveInvestigationAction(apiStore.api, id, {
            actionType: pendingActionType.value,
            actionDescription: actionForm.actionDescription.trim(),
            assignedTo: actionForm.assignedTo.trim(),
            dueDate: dueDateStr ?? null,
        });

        actions.value.push(created);
        showAddActionDialog.value = false;
    } catch (error) {
        toast.add({
            severity: 'error',
            summary: 'Failed',
            detail: 'Could not add action. Please try again.',
            life: 5000,
        });
        console.error(error);
    } finally {
        addingAction.value = false;
    }
}

async function deleteAction(action: InvestigationActionItem) {
    const id = String(route.params.id || '').trim();
    if (!id) return;

    try {
        await deleteInvestigationAction(apiStore.api, id, action.actionId);
        const idx = actions.value.findIndex(a => a.actionId === action.actionId);
        if (idx >= 0) actions.value.splice(idx, 1);
    } catch (error) {
        toast.add({
            severity: 'error',
            summary: 'Failed',
            detail: 'Could not delete action. Please try again.',
            life: 5000,
        });
        console.error(error);
    }
}

async function doTransition(action: TransitionAction) {
    const id = String(route.params.id || '').trim();
    if (!id || transitioning.value) return;

    transitioning.value = true;
    try {
        const updated = await transitionInvestigation(apiStore.api, id, action);
        investigation.value = updated;
        toast.add({
            severity: 'success',
            summary: 'Status Updated',
            detail: `Investigation transitioned via "${action}".`,
            life: 3000,
        });
    } catch (error: unknown) {
        const detail =
            (error as { response?: { data?: { detail?: string } } })?.response?.data?.detail ??
            'Could not complete the transition. Please try again.';
        toast.add({
            severity: 'error',
            summary: 'Transition Failed',
            detail,
            life: 6000,
        });
    } finally {
        transitioning.value = false;
    }
}

function openIncident() {
    if (!investigation.value?.incidentId) return;
    void router.push(`/safety-application/incidents/${investigation.value.incidentId}`);
}

function statusTone(status: SafetyInvestigationRecord['status']) {
    if (status === 'Assigned') return 'warning';
    if (status === 'In Progress') return 'info';
    if (status === 'Completed') return 'success';
    if (status === 'Closed') return 'secondary';
    return 'contrast';
}

function priorityTone(priority: SafetyInvestigationRecord['priority']) {
    if (priority === 'High') return 'danger';
    if (priority === 'Medium') return 'warning';
    return 'success';
}

const ActionsTable = defineComponent({
    props: {
        actions: {
            type: Array as () => InvestigationActionItem[],
            default: () => [],
        },
    },
    emits: ['delete'],
    setup(props, { emit }) {
        return () => {
            if (props.actions.length === 0) {
                return h('p', { class: 'text-sm text-slate-400' }, 'No actions recorded.');
            }
            return h('div', { class: 'overflow-x-auto' },
                h('table', { class: 'w-full text-sm' }, [
                    h('thead', {},
                        h('tr', { class: 'text-left text-xs uppercase tracking-[0.15em] text-slate-400 border-b border-slate-700' }, [
                            h('th', { class: 'pb-2 pr-4 font-medium' }, 'Description'),
                            h('th', { class: 'pb-2 pr-4 font-medium w-40' }, 'Assigned To'),
                            h('th', { class: 'pb-2 pr-4 font-medium w-32' }, 'Due Date'),
                            h('th', { class: 'pb-2 font-medium w-16' }, ''),
                        ])
                    ),
                    h('tbody', {},
                        props.actions.map(action =>
                            h('tr', { key: action.actionId, class: 'border-b border-slate-800/60' }, [
                                h('td', { class: 'py-2 pr-4 text-slate-200' }, action.actionDescription),
                                h('td', { class: 'py-2 pr-4 text-slate-300' }, action.assignedTo || '—'),
                                h('td', { class: 'py-2 pr-4 text-slate-300' }, action.dueDate ? formatDateMMDDYYYY(action.dueDate) : '—'),
                                h('td', { class: 'py-2' },
                                    h('button', {
                                        type: 'button',
                                        class: 'text-slate-500 hover:text-red-400 transition-colors',
                                        onClick: () => emit('delete', action),
                                    },
                                        h('i', { class: 'pi pi-trash text-xs' })
                                    )
                                ),
                            ])
                        )
                    ),
                ])
            );
        };
    },
});
</script>

