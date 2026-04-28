<template>
    <div class="estimate-list-view">
        <BasePageHeader icon="pi pi-calculator" title="Quote Log" subtitle="Manage all estimates and bids">
            <BaseButtonCreate label="New Estimate" @click="router.push('/estimating/estimates/new')" />
        </BasePageHeader>

        <!-- ── Shared filter + view toggle bar ────────────────────────────── -->
        <div class="filter-bar">
            <span class="p-input-icon-left">
                <i class="pi pi-search" />
                <InputText v-model="search" placeholder="Search estimates..." @input="onFilterChange" class="filter-search" />
            </span>
            <Dropdown
                v-model="statusFilter"
                :options="statusOptions"
                optionLabel="label"
                optionValue="value"
                placeholder="All Stat..."
                showClear
                class="filter-dropdown"
                @change="onFilterChange"
            />
            <InputText v-model="clientFilter" placeholder="Client..." class="filter-client" @input="onFilterChange" />
            <Dropdown
                v-model="yearFilter"
                :options="yearOptions"
                optionLabel="label"
                optionValue="value"
                placeholder="All..."
                showClear
                class="filter-year"
                @change="onFilterChange"
            />
            <Button
                v-if="selectedItems.length > 0"
                label="Delete Selected"
                icon="pi pi-trash"
                severity="danger"
                outlined
                size="small"
                @click="confirmBulkDelete"
            />
            <div class="filter-spacer" />
            <div class="view-toggle">
                <Button
                    icon="pi pi-list"
                    size="small"
                    :severity="viewMode === 'table' ? undefined : 'secondary'"
                    :outlined="viewMode !== 'table'"
                    v-tooltip="'Table view'"
                    @click="setView('table')"
                />
                <Button
                    icon="pi pi-th-large"
                    size="small"
                    :severity="viewMode === 'card' ? undefined : 'secondary'"
                    :outlined="viewMode !== 'card'"
                    v-tooltip="'Card view'"
                    @click="setView('card')"
                />
            </div>
        </div>

        <!-- ── Table view ─────────────────────────────────────────────────── -->
        <BaseDataTable
            v-if="viewMode === 'table'"
            :value="items"
            :loading="loading"
            v-model:selection="selectedItems"
            selectionMode="multiple"
            dataKey="estimateId"
            :rows="pageSize"
            :totalRecords="total"
            :lazy="true"
            :paginator="true"
            paginatorTemplate="FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink CurrentPageReport RowsPerPageDropdown"
            :rowsPerPageOptions="[25, 50, 100]"
            currentPageReportTemplate="Showing {first} to {last} of {totalRecords}"
            emptyMessage="No estimates found."
            @page="onPage"
            @row-dblclick="onRowDblClick"
        >
            <Column selectionMode="multiple" headerStyle="width:3rem" />
            <Column field="estimateNumber" header="#" sortable style="min-width:130px">
                <template #body="{ data }">
                    <span class="font-mono text-sm font-semibold text-blue-400">{{ data.estimateNumber }}</span>
                </template>
            </Column>
            <Column field="name" header="Name" sortable style="min-width:200px">
                <template #body="{ data }">
                    <div class="flex align-items-center gap-2">
                        <span>{{ data.name }}</span>
                        <Tag v-if="data.isScenario" value="Scenario" severity="secondary" class="scenario-tag" />
                    </div>
                </template>
            </Column>
            <Column field="client" header="Client" sortable style="min-width:140px" />
            <Column field="status" header="Status" style="min-width:110px">
                <template #body="{ data }">
                    <Tag :value="data.status" :severity="statusSeverity(data.status)" />
                </template>
            </Column>
            <Column field="branch" header="Branch" style="min-width:100px" />
            <Column field="startDate" header="Start" style="min-width:110px">
                <template #body="{ data }">{{ fmtDate(data.startDate) }}</template>
            </Column>
            <Column field="endDate" header="End" style="min-width:110px">
                <template #body="{ data }">{{ fmtDate(data.endDate) }}</template>
            </Column>
            <Column field="grandTotal" header="Grand Total" style="min-width:130px; text-align:right" bodyStyle="text-align:right">
                <template #body="{ data }">
                    <span class="font-semibold tabular-nums">{{ fmtCurrency(data.grandTotal) }}</span>
                </template>
            </Column>
            <Column header="" style="width:110px; text-align:right" bodyStyle="text-align:right">
                <template #body="{ data }">
                    <div class="row-actions">
                        <Button icon="pi pi-pencil" text rounded size="small" v-tooltip="'Open'" @click.stop="editEstimate(data.estimateId)" />
                        <Button icon="pi pi-trash" text rounded size="small" severity="danger" v-tooltip="'Delete'" @click.stop="confirmDelete(data)" />
                        <Button
                            icon="pi pi-ellipsis-v"
                            text rounded size="small"
                            severity="secondary"
                            v-tooltip="'More actions'"
                            @click.stop="openRowMenu($event, data)"
                        />
                    </div>
                </template>
            </Column>
        </BaseDataTable>

        <!-- ── Card view ──────────────────────────────────────────────────── -->
        <div v-else class="card-view">
            <div v-if="loading" class="flex justify-center p-10">
                <ProgressSpinner />
            </div>
            <div v-else-if="items.length === 0" class="empty-state">
                <i class="pi pi-calculator text-4xl text-surface-400 mb-3" />
                <p class="text-surface-400">No estimates found.</p>
            </div>
            <div v-else class="estimate-card-grid">
                <Card
                    v-for="item in items"
                    :key="item.estimateId"
                    class="estimate-card"
                    @click="editEstimate(item.estimateId)"
                >
                    <template #title>
                        <div class="card-header">
                            <span class="card-number font-mono">{{ item.estimateNumber }}</span>
                            <Tag :value="item.status" :severity="statusSeverity(item.status)" class="card-status-tag" />
                        </div>
                        <div class="card-name">{{ item.name }}</div>
                    </template>
                    <template #content>
                        <div class="card-meta">
                            <div class="card-meta-row">
                                <i class="pi pi-building card-icon" />
                                <span>{{ item.client }}</span>
                            </div>
                            <div v-if="item.branch" class="card-meta-row">
                                <i class="pi pi-map-marker card-icon" />
                                <span>{{ item.branch }}</span>
                            </div>
                            <div v-if="item.startDate" class="card-meta-row">
                                <i class="pi pi-calendar card-icon" />
                                <span>{{ fmtDate(item.startDate) }} – {{ fmtDate(item.endDate) }}</span>
                            </div>
                            <div class="card-total">{{ fmtCurrency(item.grandTotal) }}</div>
                        </div>
                    </template>
                    <template #footer>
                        <div class="card-footer-actions">
                            <Button
                                label="Open"
                                icon="pi pi-folder-open"
                                size="small"
                                outlined
                                class="flex-1"
                                @click.stop="editEstimate(item.estimateId)"
                            />
                            <Button
                                icon="pi pi-ellipsis-v"
                                size="small"
                                text
                                severity="secondary"
                                v-tooltip="'More actions'"
                                @click.stop="openRowMenu($event, item)"
                            />
                        </div>
                    </template>
                </Card>
            </div>

            <Paginator
                v-if="total > pageSize"
                :rows="pageSize"
                :totalRecords="total"
                :rowsPerPageOptions="[25, 50, 100]"
                template="FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink CurrentPageReport RowsPerPageDropdown"
                currentPageReportTemplate="Showing {first} to {last} of {totalRecords}"
                class="card-paginator"
                @page="onPage"
            />
        </div>

        <Menu ref="rowMenu" :model="rowMenuItems" popup appendTo="body" />

        <!-- Lost Reason Dialog -->
        <Dialog v-model:visible="lostDialogVisible" header="Mark as Lost" modal style="width:420px" :closable="false">
            <div class="flex flex-col gap-3 pt-2">
                <p class="text-slate-400 text-sm">Select a reason before confirming.</p>
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
                <Button label="Cancel" text @click="lostDialogVisible = false" />
                <Button label="Confirm Lost" icon="pi pi-check" severity="danger" :disabled="!lostReasonDraft" @click="confirmLostFromList" />
            </template>
        </Dialog>

        <ConfirmDialog :closable="false" :dismissableMask="false" />
        <Toast />
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useConfirm } from 'primevue/useconfirm';
import { useToast } from 'primevue/usetoast';
import ConfirmDialog from 'primevue/confirmdialog';
import { useApiStore } from '@/stores/apiStore';
import BasePageHeader from '@/components/layout/BasePageHeader.vue';
import BaseButtonCreate from '@/components/buttons/BaseButtonCreate.vue';
import BaseDataTable from '@/components/tables/BaseDataTable.vue';

const router = useRouter();
const confirm = useConfirm();
const toast = useToast();
const apiStore = useApiStore();

// ── View mode ─────────────────────────────────────────────────────────────────
type ViewMode = 'table' | 'card';
const viewMode = ref<ViewMode>((localStorage.getItem('ql-view-mode') as ViewMode) ?? 'table');
function setView(mode: ViewMode) {
    viewMode.value = mode;
    localStorage.setItem('ql-view-mode', mode);
}

const devMode = import.meta.env.DEV;
const resetting = ref(false);
const seeding = ref(false);

async function resetDevData() {
    resetting.value = true;
    try {
        const { data } = await apiStore.api.post('/api/v1/dev/reset');
        toast.add({ severity: 'warn', summary: 'Reset Complete', detail: data.message ?? 'All CSL data deleted', life: 4000 });
        loadEstimates();
    } catch {
        toast.add({ severity: 'error', summary: 'Reset Failed', detail: 'Could not reach dev/reset endpoint', life: 4000 });
    } finally {
        resetting.value = false;
    }
}

async function seedDevData() {
    seeding.value = true;
    try {
        const { data } = await apiStore.api.post('/api/v1/dev/seed');
        toast.add({ severity: 'success', summary: 'Seed Complete', detail: data.message ?? 'Dev data seeded', life: 4000 });
        loadEstimates();
    } catch (err: any) {
        const detail = err?.response?.status === 409
            ? 'Data already seeded — run Reset first'
            : 'Seed failed';
        toast.add({ severity: 'error', summary: 'Seed Failed', detail, life: 4000 });
    } finally {
        seeding.value = false;
    }
}

interface EstimateListItem {
    estimateId: number;
    estimateNumber: string;
    name: string;
    client: string;
    status: string;
    branch?: string;
    startDate?: string;
    endDate?: string;
    grandTotal: number;
    isScenario?: boolean;
    lostReason?: string;
}

const items = ref<EstimateListItem[]>([]);
const total = ref(0);
const page = ref(1);
const pageSize = ref(50);
const loading = ref(false);
const selectedItems = ref<EstimateListItem[]>([]);

const search = ref('');
const statusFilter = ref('');
const clientFilter = ref('');
const yearFilter = ref<number | ''>('');

const yearOptions = [
    { label: 'All Years', value: '' as number | '' },
    { label: '2025', value: 2025 },
    { label: '2026', value: 2026 },
    { label: '2027', value: 2027 },
];
const cloningId = ref<number | null>(null);
const revisionId = ref<number | null>(null);
const rowMenu = ref();
const rowMenuItems = ref<any[]>([]);
const lostDialogVisible = ref(false);
const lostTargetId = ref<number | null>(null);
const lostReasonDraft = ref('');
const lostNotesDraft = ref('');
const lostReasonOptions = ['Pricing', 'Scope', 'Competitor', 'No Decision', 'Other'];
let debounceTimer: ReturnType<typeof setTimeout>;

const statusOptions = [
    { label: 'Draft', value: 'Draft' },
    { label: 'Submitted for Approval', value: 'Submitted for Approval' },
    { label: 'Pending', value: 'Pending' },
    { label: 'Awarded', value: 'Awarded' },
    { label: 'Lost', value: 'Lost' },
    { label: 'Canceled', value: 'Canceled' },
];

async function loadEstimates() {
    loading.value = true;
    try {
        const params = new URLSearchParams({
            page: page.value.toString(),
            pageSize: pageSize.value.toString(),
        });
        if (search.value) params.set('search', search.value);
        if (statusFilter.value) params.set('status', statusFilter.value);
        if (clientFilter.value) params.set('client', clientFilter.value);
        if (yearFilter.value) params.set('year', String(yearFilter.value));

        const { data } = await apiStore.api.get(`/api/v1/estimates?${params}`);
        items.value = data.items;
        total.value = data.total;
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load estimates', life: 3000 });
    } finally {
        loading.value = false;
    }
}

function onPage(event: { page: number; rows: number }) {
    page.value = event.page + 1;
    pageSize.value = event.rows;
    loadEstimates();
}

function onFilterChange() {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(() => {
        page.value = 1;
        loadEstimates();
    }, 400);
}

function onRowDblClick(event: { data: EstimateListItem }) {
    editEstimate(event.data.estimateId);
}

function editEstimate(id: number) {
    router.push(`/estimating/estimates/${id}`);
}

function openRowMenu(event: Event, row: EstimateListItem) {
    rowMenuItems.value = buildMenuItems(row);
    rowMenu.value.toggle(event);
}

function buildMenuItems(row: EstimateListItem) {
    const items: any[] = [];
    items.push({ label: 'Open', icon: 'pi pi-folder-open', command: () => editEstimate(row.estimateId) });
    if (row.status === 'Draft') {
        items.push({ separator: true });
        items.push({
            label: 'Submit for Approval',
            icon: 'pi pi-send',
            command: () => confirmSubmitForApproval(row),
        });
    }
    if (!['Awarded', 'Lost', 'Canceled'].includes(row.status)) {
        items.push({ separator: true });
        items.push({ label: 'Mark as Won', icon: 'pi pi-trophy', command: () => confirmAward(row) });
        items.push({ label: 'Mark as Lost', icon: 'pi pi-times-circle', command: () => openLostDialog(row) });
    } else if (row.status === 'Awarded') {
        items.push({ separator: true });
        items.push({ label: 'Mark as Lost', icon: 'pi pi-times-circle', command: () => openLostDialog(row) });
    }
    if (row.status === 'Awarded' || row.status === 'Submitted for Approval') {
        items.push({ label: 'Create Revision', icon: 'pi pi-history', command: () => createRevision(row) });
    }
    return items;
}

function confirmSubmitForApproval(item: EstimateListItem) {
    confirm.require({
        message: `Submit ${item.estimateNumber} for approval?`,
        header: 'Submit for Approval',
        icon: 'pi pi-send',
        acceptClass: 'p-button-success',
        rejectClass: 'p-button-secondary p-button-outlined',
        accept: async () => {
            try {
                await apiStore.api.patch(`/api/v1/estimates/${item.estimateId}/status`, { status: 'Submitted for Approval' });
                toast.add({ severity: 'success', summary: 'Submitted', detail: `${item.estimateNumber} submitted for approval`, life: 3000 });
                loadEstimates();
            } catch {
                toast.add({ severity: 'error', summary: 'Error', detail: 'Could not update status', life: 3000 });
            }
        },
    });
}

async function cloneAsScenario(item: EstimateListItem) {
    cloningId.value = item.estimateId;
    try {
        const { data } = await apiStore.api.post(`/api/v1/estimates/${item.estimateId}/clone?asScenario=true`);
        toast.add({ severity: 'success', summary: 'Cloned', detail: `Scenario created: ${data.estimateNumber}`, life: 4000 });
        loadEstimates();
    } catch {
        toast.add({ severity: 'error', summary: 'Clone Failed', detail: 'Could not clone estimate', life: 4000 });
    } finally {
        cloningId.value = null;
    }
}

function confirmAward(item: EstimateListItem) {
    confirm.require({
        message: `Mark ${item.estimateNumber} as Awarded?`,
        header: 'Mark Awarded',
        icon: 'pi pi-trophy',
        acceptClass: 'p-button-success',
        rejectClass: 'p-button-secondary p-button-outlined',
        accept: async () => {
            try {
                await apiStore.api.patch(`/api/v1/estimates/${item.estimateId}/status`, { status: 'Awarded' });
                toast.add({ severity: 'success', summary: 'Awarded', detail: `${item.estimateNumber} marked as Awarded`, life: 3000 });
                loadEstimates();
            } catch {
                toast.add({ severity: 'error', summary: 'Error', detail: 'Could not update status', life: 3000 });
            }
        },
    });
}

function openLostDialog(item: EstimateListItem) {
    lostTargetId.value = item.estimateId;
    lostReasonDraft.value = '';
    lostNotesDraft.value = '';
    lostDialogVisible.value = true;
}

async function confirmLostFromList() {
    if (!lostTargetId.value || !lostReasonDraft.value) return;
    try {
        await apiStore.api.patch(`/api/v1/estimates/${lostTargetId.value}/status`, {
            status: 'Lost',
            lostReason: lostReasonDraft.value,
            lostNotes: lostNotesDraft.value || undefined,
        });
        toast.add({ severity: 'warn', summary: 'Marked Lost', detail: `Reason: ${lostReasonDraft.value}`, life: 3000 });
        lostDialogVisible.value = false;
        loadEstimates();
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Could not update status', life: 3000 });
    }
}

async function createRevision(item: EstimateListItem) {
    revisionId.value = item.estimateId;
    try {
        const { data } = await apiStore.api.post(`/api/v1/estimates/${item.estimateId}/clone`);
        toast.add({ severity: 'success', summary: 'Revision Created', detail: `New estimate: ${data.estimateNumber}`, life: 4000 });
        router.push(`/estimating/estimates/${data.estimateId}`);
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Could not create revision', life: 3000 });
    } finally {
        revisionId.value = null;
    }
}

function confirmDelete(item: EstimateListItem) {
    confirm.require({
        message: `Delete estimate ${item.estimateNumber}?`,
        header: 'Confirm Delete',
        icon: 'pi pi-exclamation-triangle',
        rejectClass: 'p-button-secondary p-button-outlined',
        acceptClass: 'p-button-danger',
        accept: async () => {
            try {
                await apiStore.api.delete(`/api/v1/estimates/${item.estimateId}`);
                toast.add({ severity: 'success', summary: 'Deleted', detail: `${item.estimateNumber} deleted`, life: 3000 });
                loadEstimates();
            } catch {
                toast.add({ severity: 'error', summary: 'Error', detail: 'Delete failed', life: 3000 });
            }
        },
    });
}

function confirmBulkDelete() {
    const count = selectedItems.value.length;
    confirm.require({
        message: `Delete ${count} estimate${count !== 1 ? 's' : ''}? This cannot be undone.`,
        header: 'Bulk Delete',
        icon: 'pi pi-exclamation-triangle',
        rejectClass: 'p-button-secondary p-button-outlined',
        acceptClass: 'p-button-danger',
        accept: async () => {
            try {
                await apiStore.api.delete('/api/v1/estimates', {
                    data: selectedItems.value.map(i => i.estimateId),
                });
                toast.add({ severity: 'success', summary: 'Deleted', detail: `${count} estimate(s) deleted`, life: 3000 });
                selectedItems.value = [];
                loadEstimates();
            } catch {
                toast.add({ severity: 'error', summary: 'Error', detail: 'Bulk delete failed', life: 3000 });
            }
        },
    });
}

function statusSeverity(status: string): string {
    const map: Record<string, string> = {
        Draft: '',
        'Submitted for Approval': 'info',
        Pending: 'warning',
        Awarded: 'success',
        Lost: 'danger',
        Canceled: 'warning',
    };
    return map[status] ?? '';
}

function fmtDate(val?: string): string {
    if (!val) return '';
    return new Date(val).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function fmtCurrency(val: number): string {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(val ?? 0);
}

onMounted(loadEstimates);
</script>

<style scoped>
/* ── Filter bar ──────────────────────────────────────────────────────────── */
.filter-bar {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 0.5rem;
    padding: 0.75rem 1rem;
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 8px;
}

.filter-search { width: 220px; }
.filter-dropdown { width: 160px; }
.filter-client { width: 160px; }
.filter-year { width: 120px; }
.filter-spacer { flex: 1; }

.view-toggle { display: flex; gap: 0.25rem; }

/* ── Table view ──────────────────────────────────────────────────────────── */
.estimate-list-view :deep(.p-datatable-tbody > tr) {
    cursor: pointer;
}

/* Compact row density */
.estimate-list-view :deep(.p-datatable-thead > tr > th) {
    padding: 0.4rem 0.65rem;
    font-size: 0.78rem;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    color: var(--text-color-secondary);
}
.estimate-list-view :deep(.p-datatable-tbody > tr > td) {
    padding: 0.3rem 0.65rem;
    line-height: 1.4;
    font-size: 0.85rem;
}

.scenario-tag {
    font-size: 0.6rem;
    padding: 1px 5px;
    opacity: 0.75;
}
.row-actions {
    display: flex;
    gap: 2px;
    align-items: center;
    justify-content: flex-end;
    opacity: 0;
    transition: opacity 0.15s;
}
.estimate-list-view :deep(.p-datatable-tbody > tr:hover) .row-actions {
    opacity: 1;
}

/* ── Card view ───────────────────────────────────────────────────────────── */
.card-view {
    display: flex;
    flex-direction: column;
    gap: 1rem;
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

.estimate-card-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    gap: 1rem;
}

.estimate-card {
    cursor: pointer;
    transition: box-shadow 0.15s, border-color 0.15s;
    border: 1px solid var(--surface-border);
}
.estimate-card:hover {
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3);
    border-color: var(--primary-color);
}

.card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.25rem;
}
.card-number {
    font-size: 0.75rem;
    color: var(--blue-400);
    font-weight: 600;
}
.card-status-tag { font-size: 0.7rem; }

.card-name {
    font-size: 1rem;
    font-weight: 600;
    line-height: 1.3;
    margin-top: 0.25rem;
}

.card-meta {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
    margin-top: 0.5rem;
}
.card-meta-row {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.82rem;
    color: var(--text-color-secondary);
}
.card-icon {
    font-size: 0.75rem;
    opacity: 0.6;
    width: 14px;
    flex-shrink: 0;
}
.card-total {
    font-size: 1rem;
    font-weight: 700;
    color: var(--primary-color);
    margin-top: 0.25rem;
}

.card-footer-actions {
    display: flex;
    gap: 0.5rem;
    align-items: center;
}

.card-paginator {
    border-top: 1px solid var(--surface-border);
    padding-top: 0.75rem;
}
</style>
