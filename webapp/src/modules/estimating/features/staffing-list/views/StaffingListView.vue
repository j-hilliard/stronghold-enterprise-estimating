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

            <div class="sp-toolbar-right">
                <InputText
                    v-model="search"
                    placeholder="Search plans..."
                    class="sp-search"
                    data-testid="sp-search"
                    @input="onFilterChange"
                />
                <Tag :value="`Plans: ${total}`" severity="info" data-testid="sp-count" />
            </div>
        </div>

        <div class="sp-filters" data-testid="sp-filters">
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
            <InputText
                v-model="quickFilter"
                placeholder="Quick filter (client/location/plan)"
                class="flex-1"
                data-testid="sp-quick"
                @input="applyQuickFilter"
            />
        </div>

        <div v-if="loading" class="flex justify-content-center py-8" data-testid="sp-loading">
            <ProgressSpinner />
        </div>

        <div v-else class="card-grid" data-testid="sp-card-grid">
            <div
                v-for="item in filteredItems"
                :key="item.staffingPlanId"
                class="sp-card"
                :class="cardClass(item)"
                :data-testid="`sp-card-${item.staffingPlanId}`"
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

                <div class="card-actions">
                    <Button
                        label="Open"
                        size="small"
                        :data-testid="`sp-open-${item.staffingPlanId}`"
                        @click="router.push(`/estimating/staffing-plans/${item.staffingPlanId}`)"
                    />
                    <Button
                        label="Duplicate"
                        size="small"
                        outlined
                        :data-testid="`sp-duplicate-${item.staffingPlanId}`"
                        @click="duplicatePlan(item)"
                    />
                    <Button
                        v-if="!item.convertedEstimateId"
                        label="Delete"
                        size="small"
                        outlined
                        severity="danger"
                        :data-testid="`sp-delete-${item.staffingPlanId}`"
                        @click="confirmDelete(item)"
                    />
                    <Button
                        v-if="!item.convertedEstimateId"
                        label="Convert"
                        size="small"
                        severity="success"
                        :data-testid="`sp-convert-${item.staffingPlanId}`"
                        @click="convertPlan(item)"
                    />
                    <Button
                        v-else
                        label="Converted"
                        size="small"
                        disabled
                        :data-testid="`sp-converted-${item.staffingPlanId}`"
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
                <Button label="Cancel" text @click="deleteConfirmVisible = false" />
                <Button label="Delete" severity="danger" @click="doDelete" />
            </template>
        </Dialog>
    </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { useToast } from 'primevue/usetoast';
import { useApiStore } from '@/stores/apiStore';

const router = useRouter();
const toast = useToast();
const apiStore = useApiStore();

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
let debounceTimer: ReturnType<typeof setTimeout>;

const deleteConfirmVisible = ref(false);
const deleteTarget = ref<StaffingListItem | null>(null);

const statusOptions = [
    { label: 'All Status', value: '' },
    { label: 'Draft', value: 'Draft' },
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
.sp-toolbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 12px;
    flex-wrap: wrap;
    padding: 6px 0 2px;
}

.sp-toolbar-left,
.sp-toolbar-right {
    display: flex;
    gap: 8px;
    align-items: center;
    flex-wrap: wrap;
}

.sp-search {
    min-width: 260px;
}

.sp-filters {
    display: flex;
    gap: 8px;
    align-items: center;
    flex-wrap: wrap;
    padding: 8px 0 12px;
}

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
}

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
