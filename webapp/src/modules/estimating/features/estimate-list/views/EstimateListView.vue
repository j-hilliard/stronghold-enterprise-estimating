<template>
    <div class="estimate-list-view">
        <BasePageHeader icon="pi pi-calculator" title="Estimates" subtitle="Manage all estimates and bids">
            <template v-if="devMode">
                <Button
                    label="Reset"
                    icon="pi pi-trash"
                    severity="danger"
                    outlined
                    size="small"
                    :loading="resetting"
                    @click="resetDevData"
                    v-tooltip="'Delete ALL CSL data (dev only)'"
                />
                <Button
                    label="Seed Dev Data"
                    icon="pi pi-database"
                    severity="secondary"
                    outlined
                    size="small"
                    :loading="seeding"
                    @click="seedDevData"
                    v-tooltip="'Seed rate books, cost book, estimates (dev only)'"
                />
            </template>
            <BaseButtonCreate label="New Estimate" @click="router.push('/estimating/estimates/new')" />
        </BasePageHeader>

        <BaseDataTable
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
            <template #filters>
                <span class="p-input-icon-left w-full md:w-72">
                    <i class="pi pi-search" />
                    <InputText v-model="search" placeholder="Search estimates..." @input="onFilterChange" class="w-full" />
                </span>
                <Dropdown
                    v-model="statusFilter"
                    :options="statusOptions"
                    optionLabel="label"
                    optionValue="value"
                    placeholder="All Statuses"
                    showClear
                    class="w-full md:w-40"
                    @change="onFilterChange"
                />
                <InputText v-model="clientFilter" placeholder="Client..." class="w-full md:w-44" @input="onFilterChange" />
                <Button
                    v-if="selectedItems.length > 0"
                    label="Delete Selected"
                    icon="pi pi-trash"
                    severity="danger"
                    outlined
                    @click="confirmBulkDelete"
                />
            </template>

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
            <Column field="grandTotal" header="Grand Total" style="min-width:130px">
                <template #body="{ data }">
                    <span class="font-semibold">{{ fmtCurrency(data.grandTotal) }}</span>
                </template>
            </Column>
            <Column header="Actions" style="min-width:140px">
                <template #body="{ data }">
                    <div class="flex gap-1">
                        <Button icon="pi pi-pencil" text rounded size="small" @click="editEstimate(data.estimateId)" v-tooltip="'Edit'" />
                        <Button icon="pi pi-copy" text rounded size="small" severity="secondary" @click="cloneAsScenario(data)" v-tooltip="'Clone as Scenario'" :loading="cloningId === data.estimateId" />
                        <Button icon="pi pi-trash" text rounded size="small" severity="danger" @click="confirmDelete(data)" v-tooltip="'Delete'" />
                    </div>
                </template>
            </Column>
        </BaseDataTable>

        <ConfirmDialog />
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useConfirm } from 'primevue/useconfirm';
import { useToast } from 'primevue/usetoast';
import { useApiStore } from '@/stores/apiStore';
import BasePageHeader from '@/components/layout/BasePageHeader.vue';
import BaseButtonCreate from '@/components/buttons/BaseButtonCreate.vue';
import BaseDataTable from '@/components/tables/BaseDataTable.vue';

const router = useRouter();
const confirm = useConfirm();
const toast = useToast();
const apiStore = useApiStore();

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
const cloningId = ref<number | null>(null);
let debounceTimer: ReturnType<typeof setTimeout>;

const statusOptions = [
    { label: 'Draft', value: 'Draft' },
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
.estimate-list-view :deep(.p-datatable-tbody > tr) {
    cursor: pointer;
}
.scenario-tag {
    font-size: 0.6rem;
    padding: 1px 5px;
    opacity: 0.75;
}
</style>
