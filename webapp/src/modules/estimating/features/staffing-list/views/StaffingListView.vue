<template>
    <div class="staffing-list-view" data-testid="sp-list-view">
        <div class="sp-toolbar" data-testid="sp-toolbar">
            <div class="sp-toolbar-left">
                <Button
                    label="+ New Plan"
                    icon="pi pi-plus"
                    data-testid="sp-new"
                    @click="router.push('/estimating/staffing-plans/new')"
                />
                <Button
                    label="Refresh"
                    text
                    data-testid="sp-refresh"
                    :loading="loading"
                    @click="refresh"
                />
            </div>

            <div class="sp-filters-inline">
                <Dropdown
                    v-model="statusFilter"
                    :options="statusOptions"
                    optionLabel="label"
                    optionValue="value"
                    placeholder="All Status"
                    class="w-12rem"
                    data-testid="sp-status"
                    @change="onFilterChange"
                />
                <Dropdown
                    v-model="branchFilter"
                    :options="branchOptions"
                    optionLabel="label"
                    optionValue="value"
                    placeholder="All Branches"
                    class="w-12rem"
                    data-testid="sp-branch"
                    @change="onFilterChange"
                />
                <Dropdown
                    v-model="yearFilter"
                    :options="yearOptions"
                    optionLabel="label"
                    optionValue="value"
                    placeholder="All Years"
                    class="w-10rem"
                    showClear
                    data-testid="sp-year"
                    @change="onFilterChange"
                />
                <InputText
                    v-model="quickFilter"
                    placeholder="Quick filter (client/location/plan)"
                    class="flex-1 min-w-12rem"
                    data-testid="sp-quick"
                    @input="applyQuickFilter"
                />
                <InputText
                    v-model="search"
                    placeholder="Search plans..."
                    class="sp-search"
                    data-testid="sp-search"
                    @input="onFilterChange"
                />
            </div>

            <div class="sp-toolbar-right">
                <Button
                    v-if="selectedPlans.length > 0"
                    :label="`Delete ${selectedPlans.length} Selected`"
                    icon="pi pi-trash"
                    severity="danger"
                    outlined
                    size="small"
                    @click="confirmBulkDelete"
                />
                <Tag :value="`Plans: ${total}`" severity="info" data-testid="sp-count" />
                <div class="view-toggle">
                    <Button
                        icon="pi pi-list"
                        text
                        rounded
                        size="small"
                        v-tooltip="'Table view'"
                        :severity="viewMode === 'table' ? undefined : 'secondary'"
                        :outlined="viewMode !== 'table'"
                        @click="setView('table')"
                    />
                    <Button
                        icon="pi pi-th-large"
                        text
                        rounded
                        size="small"
                        v-tooltip="'Card view'"
                        :severity="viewMode === 'card' ? undefined : 'secondary'"
                        :outlined="viewMode !== 'card'"
                        @click="setView('card')"
                    />
                </div>
            </div>
        </div>

        <div v-if="loading" class="flex justify-content-center py-8" data-testid="sp-loading">
            <ProgressSpinner />
        </div>

        <!-- ── TABLE VIEW ── -->
        <DataTable
            v-else-if="viewMode === 'table'"
            :value="filteredItems"
            :rows="pageSize"
            rowHover
            class="sp-table"
            data-testid="sp-table"
            size="small"
            v-model:selection="selectedPlans"
            selectionMode="multiple"
            dataKey="staffingPlanId"
            @row-dblclick="(e: any) => router.push(`/estimating/staffing-plans/${e.data.staffingPlanId}`)"
        >
            <Column selectionMode="multiple" headerStyle="width:3rem" />
            <Column field="staffingPlanNumber" header="Plan #" style="width:150px">
                <template #body="{ data: row }">
                    <span class="sp-number-table">{{ row.staffingPlanNumber }}</span>
                </template>
            </Column>
            <Column header="Status" style="width:155px">
                <template #body="{ data: row }">
                    <Tag
                        :value="(row.status || 'Draft').toUpperCase()"
                        :severity="statusSeverity(row.status)"
                        class="text-xs"
                    />
                </template>
            </Column>
            <Column field="name" header="Plan Name" />
            <Column field="client" header="Client" style="width:170px" />
            <Column header="Location" style="width:140px">
                <template #body="{ data: row }">{{ locationLabel(row) }}</template>
            </Column>
            <Column header="Dates" style="width:175px">
                <template #body="{ data: row }">{{ datesLabel(row) }}</template>
            </Column>
            <Column header="Rough Labor" style="width:130px" bodyStyle="text-align:right" headerStyle="text-align:right">
                <template #body="{ data: row }">
                    <span class="font-semibold tabular-nums">{{ fmtCurrency(row.roughLaborTotal) }}</span>
                </template>
            </Column>
            <Column header="" style="width:210px">
                <template #body="{ data: row }">
                    <div class="sp-row-actions">
                        <template v-if="row.convertedEstimateId">
                            <Button label="Open Estimate" icon="pi pi-arrow-right" size="small" severity="success" text
                                @click.stop="router.push(`/estimating/estimates/${row.convertedEstimateId}`)" />
                            <Button label="Plan" size="small" text severity="secondary"
                                @click.stop="router.push(`/estimating/staffing-plans/${row.staffingPlanId}`)" />
                        </template>
                        <template v-else>
                            <Button label="Open" size="small" text
                                @click.stop="router.push(`/estimating/staffing-plans/${row.staffingPlanId}`)" />
                            <Button label="Convert" size="small" text severity="success" class="action-hover"
                                @click.stop="convertPlan(row)" />
                            <Button icon="pi pi-trash" size="small" text severity="danger" class="action-hover"
                                @click.stop="confirmDelete(row)" />
                        </template>
                    </div>
                </template>
            </Column>
        </DataTable>

        <!-- ── CARD VIEW ── -->
        <div v-else class="card-grid" data-testid="sp-card-grid">
            <div
                v-for="item in filteredItems"
                :key="item.staffingPlanId"
                class="sp-card"
                :class="cardClass(item)"
                :data-testid="`sp-card-${item.staffingPlanId}`"
                :style="item.convertedEstimateId ? 'cursor:pointer' : ''"
                @click.self="item.convertedEstimateId ? router.push(`/estimating/estimates/${item.convertedEstimateId}`) : null"
            >
                <div class="card-header">
                    <span class="sp-number">{{ item.staffingPlanNumber }}</span>
                    <Tag
                        :value="(item.status || 'Draft').toUpperCase()"
                        :severity="statusSeverity(item.status)"
                        class="text-xs"
                    />
                </div>

                <div class="sp-name">{{ item.name || '(Unnamed)' }}</div>

                <div class="sp-info">
                    <div><span class="info-label">Client:</span> {{ item.client || 'N/A' }}</div>
                    <div><span class="info-label">Location:</span> {{ locationLabel(item) }}</div>
                    <div><span class="info-label">Dates:</span> {{ datesLabel(item) }}</div>
                    <div><span class="info-label">Rough Labor:</span> <strong>{{ fmtCurrency(item.roughLaborTotal) }}</strong></div>
                </div>

                <div v-if="item.laborPreview?.length" class="sp-pill-row" :data-testid="`sp-pills-${item.staffingPlanId}`">
                    <Tag
                        v-for="pill in item.laborPreview"
                        :key="pill"
                        :value="pill"
                        rounded
                        severity="info"
                        class="sp-pill"
                    />
                    <Tag
                        v-if="(item.laborMoreCount ?? 0) > 0"
                        :value="`+${item.laborMoreCount} more`"
                        rounded
                        severity="secondary"
                        class="sp-pill"
                    />
                </div>

                <!-- Converted plans -->
                <div v-if="item.convertedEstimateId" class="card-actions">
                    <Button
                        label="Open Estimate"
                        icon="pi pi-arrow-right"
                        size="small"
                        severity="success"
                        :data-testid="`sp-converted-${item.staffingPlanId}`"
                        @click="router.push(`/estimating/estimates/${item.convertedEstimateId}`)"
                    />
                    <Button
                        label="View Plan"
                        size="small"
                        severity="secondary"
                        outlined
                        :data-testid="`sp-open-${item.staffingPlanId}`"
                        @click="router.push(`/estimating/staffing-plans/${item.staffingPlanId}`)"
                    />
                </div>
                <!-- Non-converted plans -->
                <div v-else class="card-actions">
                    <Button
                        label="Open"
                        size="small"
                        :data-testid="`sp-open-${item.staffingPlanId}`"
                        @click="router.push(`/estimating/staffing-plans/${item.staffingPlanId}`)"
                    />
                    <Button
                        label="Delete"
                        size="small"
                        outlined
                        severity="danger"
                        :data-testid="`sp-delete-${item.staffingPlanId}`"
                        @click="confirmDelete(item)"
                    />
                    <Button
                        label="Convert"
                        size="small"
                        severity="success"
                        :data-testid="`sp-convert-${item.staffingPlanId}`"
                        @click="convertPlan(item)"
                    />
                </div>
            </div>

            <div v-if="!loading && filteredItems.length === 0" class="empty-state" data-testid="sp-empty">
                No staffing plans found.
            </div>
        </div>

        <div v-if="total > pageSize" class="pagination-bar" data-testid="sp-pagination">
            <Button icon="pi pi-chevron-left" text :disabled="page <= 1" @click="prevPage" />
            <span class="text-sm text-color-secondary">{{ page }} / {{ Math.ceil(total / pageSize) }}</span>
            <Button icon="pi pi-chevron-right" text :disabled="page * pageSize >= total" @click="nextPage" />
        </div>

        <Toast />

        <Dialog
            v-model:visible="deleteConfirmVisible"
            header="Delete Staffing Plan"
            :modal="true"
            style="width: 420px;"
            data-testid="sp-delete-dialog"
        >
            <p>Delete "{{ deleteTarget?.name }}"? This cannot be undone.</p>
            <template #footer>
                <Button label="Cancel" text @click="cancelDelete" />
                <Button label="Delete" severity="danger" @click="doBulkOrSingleDelete" />
            </template>
        </Dialog>
    </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { useToast } from 'primevue/usetoast';
import { useApiStore } from '@/stores/apiStore';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';

const router = useRouter();
const toast = useToast();
const apiStore = useApiStore();

type ViewMode = 'table' | 'card';

interface StaffingListItem {
    staffingPlanId: number;
    staffingPlanNumber: string;
    name: string;
    client: string;
    status: string;
    branch?: string;
    startDate?: string;
    endDate?: string;
    city?: string;
    state?: string;
    laborPreview?: string[];
    laborMoreCount?: number;
    roughLaborTotal: number;
    convertedEstimateId?: number;
}

const items = ref<StaffingListItem[]>([]);
const filteredItems = ref<StaffingListItem[]>([]);
const total = ref(0);
const page = ref(1);
const pageSize = ref(50);
const loading = ref(false);
const search = ref('');
const quickFilter = ref('');
const statusFilter = ref('');
const branchFilter = ref('');
const yearFilter = ref<number | ''>('');

const viewMode = ref<ViewMode>((localStorage.getItem('sp-view-mode') as ViewMode) ?? 'card');
function setView(mode: ViewMode) {
    viewMode.value = mode;
    localStorage.setItem('sp-view-mode', mode);
}

const yearOptions = [
    { label: 'All Years', value: '' as number | '' },
    { label: '2025', value: 2025 },
    { label: '2026', value: 2026 },
    { label: '2027', value: 2027 },
];
let debounceTimer: ReturnType<typeof setTimeout>;

const deleteConfirmVisible = ref(false);
const deleteTarget = ref<StaffingListItem | null>(null);
const selectedPlans = ref<StaffingListItem[]>([]);

const statusOptions = [
    { label: 'All Status', value: '' },
    { label: 'Draft', value: 'Draft' },
    { label: 'Submitted for Approval', value: 'Submitted for Approval' },
    { label: 'Active', value: 'Active' },
    { label: 'Approved', value: 'Approved' },
    { label: 'Converted', value: 'Converted' },
    { label: 'Archived', value: 'Archived' },
];

const branchOptions = computed(() => {
    const map = new Set<string>();
    for (const item of items.value) {
        if (item.branch) map.add(item.branch);
    }

    return [
        { label: 'All Branches', value: '' },
        ...Array.from(map)
            .sort((a, b) => a.localeCompare(b))
            .map(branch => ({ label: branch, value: branch })),
    ];
});

async function load() {
    loading.value = true;
    try {
        const params = new URLSearchParams({ page: page.value.toString(), pageSize: pageSize.value.toString() });
        if (search.value) params.set('search', search.value);
        if (statusFilter.value) params.set('status', statusFilter.value);
        if (branchFilter.value) params.set('branch', branchFilter.value);
        if (yearFilter.value) params.set('year', String(yearFilter.value));

        const { data } = await apiStore.api.get(`/api/v1/staffing-plans?${params}`);
        items.value = data.items ?? [];
        total.value = data.total ?? 0;
        applyQuickFilter();
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load staffing plans', life: 3000 });
    } finally {
        loading.value = false;
    }
}

function onFilterChange() {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(() => {
        page.value = 1;
        load();
    }, 300);
}

function applyQuickFilter() {
    const token = quickFilter.value.trim().toLowerCase();
    if (!token) {
        filteredItems.value = items.value;
        return;
    }

    filteredItems.value = items.value.filter(item => {
        const blob = [
            item.staffingPlanNumber,
            item.name,
            item.client,
            item.branch,
            item.city,
            item.state,
            ...(item.laborPreview ?? []),
        ]
            .filter(Boolean)
            .join(' ')
            .toLowerCase();
        return blob.includes(token);
    });
}

function refresh() {
    load();
}

function prevPage() {
    if (page.value > 1) {
        page.value -= 1;
        load();
    }
}

function nextPage() {
    if (page.value * pageSize.value < total.value) {
        page.value += 1;
        load();
    }
}

async function convertPlan(item: StaffingListItem) {
    try {
        const { data } = await apiStore.api.post(`/api/v1/staffing-plans/${item.staffingPlanId}/convert`);
        toast.add({ severity: 'success', summary: 'Converted', detail: `Estimate ${data.estimateNumber} created`, life: 4000 });
        load();
    } catch (err: any) {
        toast.add({ severity: 'error', summary: 'Error', detail: err?.response?.data?.message ?? 'Conversion failed', life: 4000 });
    }
}

async function duplicatePlan(item: StaffingListItem) {
    try {
        await apiStore.api.post(`/api/v1/staffing-plans/${item.staffingPlanId}/duplicate`);
        toast.add({ severity: 'success', summary: 'Duplicated', detail: 'Staffing plan duplicated', life: 3000 });
        load();
    } catch {
        toast.add({ severity: 'info', summary: 'Coming Soon', detail: 'Duplicate coming soon', life: 3000 });
    }
}

function cancelDelete() {
    deleteConfirmVisible.value = false;
    bulkDeleteIds.value = [];
    deleteTarget.value = null;
}

function confirmDelete(item: StaffingListItem) {
    deleteTarget.value = item;
    deleteConfirmVisible.value = true;
}

async function doDelete() {
    if (!deleteTarget.value) return;
    try {
        await apiStore.api.delete(`/api/v1/staffing-plans/${deleteTarget.value.staffingPlanId}`);
        toast.add({ severity: 'success', summary: 'Deleted', detail: 'Staffing plan deleted', life: 3000 });
        deleteConfirmVisible.value = false;
        deleteTarget.value = null;
        load();
    } catch (err: any) {
        toast.add({ severity: 'error', summary: 'Error', detail: err?.response?.data?.message ?? 'Delete failed', life: 4000 });
    }
}

function confirmBulkDelete() {
    const count = selectedPlans.value.length;
    deleteConfirmVisible.value = false;
    // reuse the existing dialog but with a bulk message
    const ids = selectedPlans.value.map(p => p.staffingPlanId);
    // Show a confirm via toast-style confirm — reuse Dialog by setting a synthetic target
    deleteTarget.value = { staffingPlanId: -1, staffingPlanNumber: '', name: `${count} staffing plan${count !== 1 ? 's' : ''}`, client: '', status: '', roughLaborTotal: 0 };
    bulkDeleteIds.value = ids;
    deleteConfirmVisible.value = true;
}

const bulkDeleteIds = ref<number[]>([]);

async function doBulkOrSingleDelete() {
    if (bulkDeleteIds.value.length > 0) {
        const ids = bulkDeleteIds.value;
        bulkDeleteIds.value = [];
        deleteConfirmVisible.value = false;
        deleteTarget.value = null;
        let failed = 0;
        for (const id of ids) {
            try {
                await apiStore.api.delete(`/api/v1/staffing-plans/${id}`);
            } catch {
                failed++;
            }
        }
        const succeeded = ids.length - failed;
        toast.add({ severity: succeeded > 0 ? 'success' : 'error', summary: 'Bulk Delete', detail: `${succeeded} deleted${failed > 0 ? `, ${failed} failed` : ''}`, life: 3000 });
        selectedPlans.value = [];
        load();
    } else {
        await doDelete();
    }
}

function cardClass(item: StaffingListItem): string {
    const status = item.status?.toLowerCase();
    if (status === 'converted') return 'converted';
    if (status === 'approved' || status === 'active') return 'approved';
    return '';
}

function locationLabel(item: StaffingListItem): string {
    const parts = [item.city, item.state].filter(Boolean);
    return parts.length > 0 ? parts.join(', ') : 'N/A';
}

function datesLabel(item: StaffingListItem): string {
    const start = item.startDate ? fmtDateShort(item.startDate) : 'N/A';
    const end = item.endDate ? fmtDateShort(item.endDate) : 'N/A';
    if (!item.startDate && !item.endDate) return 'N/A';
    return `${start} - ${end}`;
}

function statusSeverity(status: string): string {
    const map: Record<string, string> = {
        Converted: 'success',
        Approved: 'info',
        Active: 'info',
        'Submitted for Approval': 'info',
        Draft: 'secondary',
        Archived: 'warning',
    };
    return map[status] ?? 'secondary';
}

function fmtDateShort(val: string): string {
    return val.slice(0, 10);
}

function fmtCurrency(n: number): string {
    return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD',
        maximumFractionDigits: 0,
    }).format(n ?? 0);
}

onMounted(load);
</script>

<style scoped>
.staffing-list-view {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.sp-toolbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 8px;
    flex-wrap: wrap;
    padding: 6px 0 2px;
}

.sp-toolbar-left {
    display: flex;
    gap: 8px;
    align-items: center;
    flex-shrink: 0;
}

.sp-filters-inline {
    display: flex;
    gap: 8px;
    align-items: center;
    flex-wrap: wrap;
    flex: 1;
}

.sp-toolbar-right {
    display: flex;
    gap: 8px;
    align-items: center;
    flex-shrink: 0;
}

.sp-search {
    min-width: 200px;
}

.view-toggle {
    display: flex;
    gap: 0.25rem;
}

/* ── Card grid ── */
.card-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(330px, 1fr));
    gap: 12px;
}

.sp-card {
    background: var(--surface-card);
    border: 1px solid color-mix(in srgb, var(--surface-border) 85%, transparent);
    border-radius: 8px;
    padding: 12px;
    display: flex;
    flex-direction: column;
    gap: 8px;
}

.sp-card.converted {
    border-color: color-mix(in srgb, #22c55e 45%, var(--surface-border));
}

.sp-card.approved {
    border-color: color-mix(in srgb, #3b82f6 45%, var(--surface-border));
}

.card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.sp-number {
    font-family: monospace;
    font-size: 0.78rem;
    font-weight: 700;
    color: #60a5fa;
}

.sp-name {
    font-size: 1rem;
    font-weight: 700;
    color: var(--text-color);
    line-height: 1.3;
}

.sp-info {
    display: flex;
    flex-direction: column;
    gap: 2px;
    font-size: 0.78rem;
    color: var(--text-color-secondary);
}

.info-label {
    font-weight: 600;
    color: var(--text-color);
}

.sp-pill-row {
    display: flex;
    flex-wrap: wrap;
    gap: 4px;
}

.sp-pill {
    font-size: 0.72rem;
}

.card-actions {
    display: flex;
    gap: 6px;
    flex-wrap: wrap;
    margin-top: auto;
    padding-top: 8px;
    border-top: 1px solid var(--surface-border);
    opacity: 0;
    transition: opacity 0.15s;
}
.sp-card:hover .card-actions {
    opacity: 1;
}

/* ── Table view ── */
.sp-number-table {
    font-family: monospace;
    font-size: 0.78rem;
    font-weight: 700;
    color: #60a5fa;
}

/* Compact row density */
.sp-table :deep(.p-datatable-thead > tr > th) {
    padding: 0.4rem 0.65rem;
    font-size: 0.78rem;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    color: var(--text-color-secondary);
}
.sp-table :deep(.p-datatable-tbody > tr > td) {
    padding: 0.3rem 0.65rem;
    line-height: 1.4;
    font-size: 0.85rem;
}
.sp-table :deep(.p-datatable-tbody > tr) {
    cursor: pointer;
}

/* Row actions — Open always visible; Convert + Delete hidden until hover */
.sp-row-actions {
    display: flex;
    gap: 2px;
    align-items: center;
    min-width: 200px;
}
.sp-table :deep(.p-datatable-tbody tr .action-hover) {
    opacity: 0;
    transition: opacity 0.15s;
    pointer-events: none;
}
.sp-table :deep(.p-datatable-tbody tr:hover .action-hover) {
    opacity: 1;
    pointer-events: auto;
}

/* ── Pagination ── */
.pagination-bar {
    display: flex;
    align-items: center;
    gap: 8px;
    justify-content: center;
    padding: 8px 0;
}

.empty-state {
    grid-column: 1 / -1;
    text-align: center;
    padding: 40px;
    color: var(--text-color-secondary);
}
</style>
