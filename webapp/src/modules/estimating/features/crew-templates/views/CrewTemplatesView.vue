<template>
    <div class="crew-templates-view">
        <BasePageHeader icon="pi pi-sitemap" title="Crew Templates" subtitle="Save and reuse crew configurations">
            <Button label="New Template" icon="pi pi-plus" @click="openCreate" />
        </BasePageHeader>

        <!-- Card grid -->
        <div v-if="loading" class="flex justify-center p-8">
            <ProgressSpinner />
        </div>

        <div v-else-if="templates.length === 0" class="empty-state">
            <i class="pi pi-sitemap text-4xl text-surface-400 mb-3" />
            <p class="text-surface-400">No crew templates yet. Create one to get started.</p>
            <Button label="New Template" icon="pi pi-plus" class="mt-3" @click="openCreate" />
        </div>

        <div v-else class="template-grid">
            <Card v-for="t in templates" :key="t.crewTemplateId" class="template-card">
                <template #title>
                    <div class="flex justify-between items-start gap-2">
                        <span class="font-semibold">{{ t.name }}</span>
                        <div class="flex gap-1">
                            <Button icon="pi pi-pencil" text rounded size="small" @click="openEdit(t)" v-tooltip="'Edit'" />
                            <Button icon="pi pi-trash" text rounded size="small" severity="danger" @click="confirmDelete(t)" v-tooltip="'Delete'" />
                        </div>
                    </div>
                </template>
                <template #subtitle>
                    <span class="text-surface-400 text-sm">{{ t.rowCount }} position{{ t.rowCount !== 1 ? 's' : '' }}</span>
                    <span v-if="t.description" class="ml-2 text-surface-400 text-sm">· {{ t.description }}</span>
                </template>
                <template #content>
                    <!-- Position pills -->
                    <div class="position-pills">
                        <Tag
                            v-for="pos in t.positions.slice(0, 8)"
                            :key="`${pos.position}-${pos.shift}`"
                            :value="`${pos.qty}× ${pos.position}`"
                            class="pill"
                        />
                        <Tag v-if="t.positions.length > 8" :value="`+${t.positions.length - 8} more`" severity="secondary" class="pill" />
                    </div>
                </template>
                <template #footer>
                    <Button
                        label="Apply to Estimate"
                        icon="pi pi-arrow-right"
                        outlined
                        size="small"
                        class="w-full"
                        @click="openApply(t)"
                    />
                </template>
            </Card>
        </div>

        <!-- Create/Edit Dialog -->
        <Dialog
            v-model:visible="showDialog"
            :header="editingId ? 'Edit Template' : 'New Crew Template'"
            :modal="true"
            :style="{ width: '640px' }"
            :dismissableMask="true"
        >
            <div class="flex flex-col gap-4">
                <div class="flex flex-col gap-1">
                    <label class="text-sm font-medium">Name *</label>
                    <InputText v-model="form.name" placeholder="e.g. Standard 8-Man Piping Crew" class="w-full" />
                </div>
                <div class="flex flex-col gap-1">
                    <label class="text-sm font-medium">Description</label>
                    <InputText v-model="form.description" placeholder="Optional description" class="w-full" />
                </div>

                <div>
                    <div class="flex justify-between items-center mb-2">
                        <label class="text-sm font-medium">Positions</label>
                        <Button label="Add Row" icon="pi pi-plus" text size="small" @click="addRow" />
                    </div>
                    <p v-if="costBookPositions.length === 0" class="text-orange-400 text-xs mb-2">
                        <i class="pi pi-exclamation-triangle mr-1" />No cost book positions found. Load a cost book first.
                    </p>
                    <DataTable :value="form.rows" size="small" class="template-row-table">
                        <Column header="Position" style="min-width:200px">
                            <template #body="{ data: row, index }">
                                <Dropdown
                                    v-model="form.rows[index].position"
                                    :options="costBookPositions"
                                    placeholder="Select position..."
                                    class="w-full"
                                    filter
                                    :disabled="costBookPositions.length === 0"
                                />
                            </template>
                        </Column>
                        <Column header="Qty" style="width:70px">
                            <template #body="{ data: row, index }">
                                <InputNumber v-model="form.rows[index].qty" :min="1" :max="99" class="w-full" inputStyle="text-align:center" size="small" />
                            </template>
                        </Column>
                        <Column header="Shift" style="width:110px">
                            <template #body="{ data: row, index }">
                                <Dropdown
                                    v-model="form.rows[index].shift"
                                    :options="shiftOptions"
                                    optionLabel="label"
                                    optionValue="value"
                                    class="w-full"
                                />
                            </template>
                        </Column>
                        <Column style="width:48px">
                            <template #body="{ index }">
                                <Button icon="pi pi-times" text rounded size="small" severity="danger" @click="removeRow(index)" />
                            </template>
                        </Column>
                    </DataTable>
                    <p v-if="form.rows.length === 0" class="text-surface-400 text-sm mt-2 text-center">No rows. Click Add Row to begin.</p>
                </div>
            </div>

            <template #footer>
                <Button label="Cancel" text @click="showDialog = false" />
                <Button label="Save" icon="pi pi-check" :loading="saving" @click="save" />
            </template>
        </Dialog>

        <!-- Apply Dialog -->
        <Dialog
            v-model:visible="showApplyDialog"
            header="Apply Template to Estimate"
            :modal="true"
            :style="{ width: '420px' }"
            :dismissableMask="true"
        >
            <div class="flex flex-col gap-3">
                <p class="text-sm text-surface-400">
                    Applying <strong class="text-surface-0">{{ applyTarget?.name }}</strong>.
                    Select the estimate and rate book to use for billing rates.
                </p>
                <div class="flex flex-col gap-1">
                    <label class="text-sm font-medium">Estimate *</label>
                    <Dropdown
                        v-model="applyEstimateId"
                        :options="estimateOptions"
                        optionLabel="label"
                        optionValue="value"
                        placeholder="Select estimate..."
                        class="w-full"
                        filter
                    />
                </div>
                <div class="flex flex-col gap-1">
                    <label class="text-sm font-medium">Rate Book (for billing rates)</label>
                    <Dropdown
                        v-model="applyRateBookId"
                        :options="rateBookOptions"
                        optionLabel="label"
                        optionValue="value"
                        placeholder="Select rate book..."
                        showClear
                        class="w-full"
                        filter
                    />
                </div>
            </div>
            <template #footer>
                <Button label="Cancel" text @click="showApplyDialog = false" />
                <Button
                    label="Apply"
                    icon="pi pi-check"
                    :disabled="!applyEstimateId"
                    :loading="applying"
                    @click="applyTemplate"
                />
            </template>
        </Dialog>

        <ConfirmDialog />
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useConfirm } from 'primevue/useconfirm';
import { useToast } from 'primevue/usetoast';
import ConfirmDialog from 'primevue/confirmdialog';
import { useApiStore } from '@/stores/apiStore';
import BasePageHeader from '@/components/layout/BasePageHeader.vue';

const confirm = useConfirm();
const toast = useToast();
const apiStore = useApiStore();

// ── Types ─────────────────────────────────────────────────────────────────────

interface TemplateListItem {
    crewTemplateId: number;
    name: string;
    description?: string;
    rowCount: number;
    positions: Array<{ position: string; qty: number; shift: string }>;
}

interface FormRow {
    position: string;
    qty: number;
    shift: string;
    laborType: string;
}

// ── State ─────────────────────────────────────────────────────────────────────

const templates = ref<TemplateListItem[]>([]);
const loading = ref(false);

const showDialog = ref(false);
const editingId = ref<number | null>(null);
const saving = ref(false);

const form = ref({
    name: '',
    description: '',
    rows: [] as FormRow[],
});

const showApplyDialog = ref(false);
const applyTarget = ref<TemplateListItem | null>(null);
const applyEstimateId = ref<number | null>(null);
const applyRateBookId = ref<number | null>(null);
const applying = ref(false);

const estimateOptions = ref<Array<{ label: string; value: number }>>([]);
const rateBookOptions = ref<Array<{ label: string; value: number }>>([]);
const costBookPositions = ref<string[]>([]);

const shiftOptions = [
    { label: 'Day', value: 'Day' },
    { label: 'Night', value: 'Night' },
    { label: 'Both', value: 'Both' },
];

const laborTypeOptions = [
    { label: 'Direct', value: 'Direct' },
    { label: 'Indirect', value: 'Indirect' },
];

// ── Load ──────────────────────────────────────────────────────────────────────

async function loadTemplates() {
    loading.value = true;
    try {
        const { data } = await apiStore.api.get('/api/v1/crew-templates');
        templates.value = data;
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load templates', life: 3000 });
    } finally {
        loading.value = false;
    }
}

async function loadEstimateOptions() {
    try {
        const { data } = await apiStore.api.get('/api/v1/estimates?pageSize=200');
        estimateOptions.value = (data.items ?? []).map((e: any) => ({
            label: `${e.estimateNumber} — ${e.name}`,
            value: e.estimateId,
        }));
    } catch { /* ignore */ }
}

async function loadRateBookOptions() {
    try {
        const { data } = await apiStore.api.get('/api/v1/rate-books');
        rateBookOptions.value = (data as any[]).map(rb => ({
            label: rb.name,
            value: rb.rateBookId,
        }));
    } catch { /* ignore */ }
}

onMounted(() => {
    loadTemplates();
    loadEstimateOptions();
    loadRateBookOptions();
});

// ── Create/Edit dialog ────────────────────────────────────────────────────────

async function loadCostBookPositions() {
    if (costBookPositions.value.length > 0) return;
    try {
        const { data } = await apiStore.api.get('/api/v1/cost-books/positions');
        costBookPositions.value = Array.isArray(data) ? data : [];
    } catch {
        costBookPositions.value = [];
    }
}

function openCreate() {
    editingId.value = null;
    form.value = { name: '', description: '', rows: [] };
    loadCostBookPositions();
    showDialog.value = true;
}

async function openEdit(t: TemplateListItem) {
    editingId.value = t.crewTemplateId;
    loadCostBookPositions();
    try {
        const { data } = await apiStore.api.get(`/api/v1/crew-templates/${t.crewTemplateId}`);
        form.value = {
            name: data.name,
            description: data.description ?? '',
            rows: (data.rows ?? []).map((r: any) => ({
                position: r.position,
                qty: r.qty,
                shift: r.shift,
                laborType: r.laborType ?? 'Direct',
            })),
        };
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load template', life: 3000 });
        return;
    }
    showDialog.value = true;
}

function addRow() {
    form.value.rows.push({ position: '', qty: 1, shift: 'Day', laborType: 'Direct' });
}

function removeRow(index: number) {
    form.value.rows.splice(index, 1);
}

async function save() {
    if (!form.value.name.trim()) {
        toast.add({ severity: 'warn', summary: 'Validation', detail: 'Name is required', life: 3000 });
        return;
    }
    saving.value = true;
    try {
        const payload = {
            name: form.value.name.trim(),
            description: form.value.description.trim() || null,
            rows: form.value.rows.filter(r => r.position.trim()).map((r, i) => ({
                position: r.position.trim(),
                laborType: r.laborType,
                craftCode: null,
                qty: r.qty,
                shift: r.shift,
            })),
        };

        if (editingId.value) {
            await apiStore.api.put(`/api/v1/crew-templates/${editingId.value}`, payload);
            toast.add({ severity: 'success', summary: 'Saved', detail: 'Template updated', life: 3000 });
        } else {
            await apiStore.api.post('/api/v1/crew-templates', payload);
            toast.add({ severity: 'success', summary: 'Created', detail: 'Template created', life: 3000 });
        }

        showDialog.value = false;
        loadTemplates();
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Save failed', life: 3000 });
    } finally {
        saving.value = false;
    }
}

// ── Delete ────────────────────────────────────────────────────────────────────

function confirmDelete(t: TemplateListItem) {
    confirm.require({
        message: `Delete template "${t.name}"?`,
        header: 'Confirm Delete',
        icon: 'pi pi-exclamation-triangle',
        rejectClass: 'p-button-secondary p-button-outlined',
        acceptClass: 'p-button-danger',
        accept: async () => {
            try {
                await apiStore.api.delete(`/api/v1/crew-templates/${t.crewTemplateId}`);
                toast.add({ severity: 'success', summary: 'Deleted', detail: `"${t.name}" deleted`, life: 3000 });
                loadTemplates();
            } catch {
                toast.add({ severity: 'error', summary: 'Error', detail: 'Delete failed', life: 3000 });
            }
        },
    });
}

// ── Apply ─────────────────────────────────────────────────────────────────────

function openApply(t: TemplateListItem) {
    applyTarget.value = t;
    applyEstimateId.value = null;
    applyRateBookId.value = null;
    showApplyDialog.value = true;
}

async function applyTemplate() {
    if (!applyEstimateId.value || !applyTarget.value) return;
    applying.value = true;
    try {
        const { data } = await apiStore.api.post(
            `/api/v1/crew-templates/${applyTarget.value.crewTemplateId}/apply`,
            { estimateId: applyEstimateId.value, rateBookId: applyRateBookId.value }
        );
        toast.add({
            severity: 'success',
            summary: 'Applied',
            detail: data.message ?? `Added ${data.addedCount} row(s)`,
            life: 4000,
        });
        showApplyDialog.value = false;
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Apply failed', life: 3000 });
    } finally {
        applying.value = false;
    }
}
</script>

<style scoped>
.crew-templates-view {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
}

.empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 4rem 2rem;
    border: 1px dashed var(--surface-border);
    border-radius: 8px;
}

.template-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
    gap: 1rem;
}

.template-card :deep(.p-card-footer) {
    padding-top: 0.5rem;
}

.position-pills {
    display: flex;
    flex-wrap: wrap;
    gap: 0.35rem;
    margin-top: 0.25rem;
    min-height: 2rem;
}

.pill {
    font-size: 0.72rem;
}

.template-row-table :deep(.p-datatable-tbody td) {
    padding: 0.3rem 0.5rem;
}
</style>
