<template>
    <div class="space-y-4">
        <Card class="mt-6">
            <template #content>
                <div class="space-y-4">
                    <div class="flex w-full flex-col gap-4 rounded-2xl border border-slate-700/70 bg-slate-900/50 p-4">
                        <div class="grid gap-3 md:grid-cols-2 xl:grid-cols-5">
                            <span class="flex flex-col gap-2">
                                <label class="text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Incident Date Start</label>
                                <InputText :modelValue="incidentDateStart" type="date" class="w-full" @update:modelValue="emit('update:incident-date-start', normalizeString($event))" />
                            </span>
                            <span class="flex flex-col gap-2">
                                <label class="text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Incident Date End</label>
                                <InputText :modelValue="incidentDateEnd" type="date" class="w-full" @update:modelValue="emit('update:incident-date-end', normalizeString($event))" />
                            </span>
                            <span class="flex flex-col gap-2">
                                <label class="text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Company</label>
                                <Dropdown
                                    :modelValue="selectedCompany || null"
                                    :options="companyOptions"
                                    optionLabel="label"
                                    optionValue="value"
                                    placeholder="All Companies"
                                    class="w-full"
                                    :showClear="!!selectedCompany"
                                    @change="emit('update:company', $event.value || null)"
                                />
                            </span>
                            <span class="flex flex-col gap-2">
                                <label class="text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Customer</label>
                                <Dropdown
                                    :modelValue="selectedCustomer || null"
                                    :options="customerOptions"
                                    optionLabel="label"
                                    optionValue="value"
                                    placeholder="All Customers"
                                    class="w-full"
                                    :showClear="!!selectedCustomer"
                                    @change="emit('update:customer', $event.value || null)"
                                />
                            </span>
                            <span class="flex flex-col gap-2">
                                <label class="text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Page Size</label>
                                <Dropdown
                                    :modelValue="pageSize"
                                    :options="pageSizeOptionItems"
                                    optionLabel="label"
                                    optionValue="value"
                                    class="w-full"
                                    @change="emit('update:page-size', $event.value || 20)"
                                />
                            </span>
                        </div>
                        <div class="flex flex-wrap items-center justify-between gap-3">
                            <div class="flex flex-wrap items-center gap-2">
                                <Tag v-if="activeFilterLabel" :value="activeFilterLabel" severity="info" class="!rounded-full !px-3 !py-2" />
                                <Tag v-if="selectedCompany" :value="selectedCompany" severity="contrast" class="!rounded-full !px-3 !py-2" />
                                <Tag v-if="selectedCustomer" :value="selectedCustomer" severity="contrast" class="!rounded-full !px-3 !py-2" />
                            </div>
                            <div class="flex items-center gap-3">
                                <span class="text-sm text-slate-400">{{ pageReport }}</span>
                                <Button v-if="showClearFilters" label="Clear Filters" text @click="emit('clear-filters')" />
                            </div>
                        </div>
                    </div>

                    <div class="overflow-x-auto rounded-3xl border border-slate-700/70 bg-slate-900/30">
                        <table class="min-w-full border-collapse">
                            <thead class="bg-slate-900/80">
                                <tr>
                                    <th v-for="column in columns" :key="column.field" class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">
                                        <button type="button" class="flex items-center gap-2 bg-transparent p-0 text-left text-inherit" @click="toggleSort(column.field)">
                                            <span>{{ column.label }}</span>
                                            <i :class="sortIcon(column.field)" />
                                        </button>
                                    </th>
                                    <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Actions</th>
                                </tr>
                            </thead>
                            <tbody v-if="rows.length">
                                <tr v-for="row in rows" :key="row.incidentId" class="border-t border-slate-800/80 text-sm text-slate-200">
                                    <td class="px-4 py-3 font-medium text-white">{{ row.incidentNumber }}</td>
                                    <td class="px-4 py-3">{{ formatDateMMDDYYYY(row.incidentDate) }}</td>
                                    <td class="px-4 py-3">{{ row.company }}</td>
                                    <td class="px-4 py-3">{{ row.region }}</td>
                                    <td class="px-4 py-3">{{ row.customer }}</td>
                                    <td class="px-4 py-3">{{ row.customerSite }}</td>
                                    <td class="px-4 py-3">
                                        <Tag :value="row.status" :severity="statusTone(row.status)" />
                                    </td>
                                    <td class="px-4 py-3 whitespace-nowrap">
                                        <BaseButtonIconView @click="emit('view', row)" />
                                        <BaseButtonIconEdit @click="emit('edit', row)" />
                                    </td>
                                </tr>
                            </tbody>
                            <tbody v-else>
                                <tr>
                                    <td colspan="8" class="px-4 py-8 text-center text-sm text-slate-400">
                                        {{ loading ? 'Loading incidents...' : emptyMessage }}
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </template>
        </Card>

        <div class="flex flex-col gap-3 rounded-2xl border border-slate-700/70 bg-slate-900/40 px-4 py-3 md:flex-row md:items-center md:justify-between">
            <p class="m-0 text-sm text-slate-400">{{ pageReport }}</p>
            <Paginator
                :rows="pageSize"
                :totalRecords="totalRecords"
                :first="firstRow"
                :rowsPerPageOptions="pageSizeOptions"
                template="PrevPageLink PageLinks NextPageLink"
                @page="onPage"
            />
        </div>
    </div>
</template>

<script setup lang="ts">
import BaseButtonIconEdit from '@/components/buttons/BaseButtonIconEdit.vue';
import BaseButtonIconView from '@/components/buttons/BaseButtonIconView.vue';
import { formatDateMMDDYYYY } from '@/utils.ts';
import Card from 'primevue/card';
import Dropdown from 'primevue/dropdown';
import InputText from 'primevue/inputtext';
import Paginator from 'primevue/paginator';
import Tag from 'primevue/tag';
import Button from 'primevue/button';
import { computed } from 'vue';
import type {
    IncidentFilterType,
    IncidentRegisterRecord,
    IncidentRegisterSortField,
} from '@/modules/safety-application/features/dashboard/services/incidentRegisterService';

const props = defineProps<{
    rows: IncidentRegisterRecord[];
    loading?: boolean;
    emptyMessage: string;
    selectedType: IncidentFilterType | '';
    selectedCompany: string;
    selectedCustomer: string;
    incidentDateStart: string;
    incidentDateEnd: string;
    companyOptions: { label: string; value: string }[];
    customerOptions: { label: string; value: string }[];
    pageSize: number;
    pageSizeOptions: readonly number[];
    page: number;
    totalRecords: number;
    pageReport: string;
    sortField: IncidentRegisterSortField;
    sortOrder: 1 | -1;
    activeFilterLabel?: string;
    showClearFilters?: boolean;
}>();

const emit = defineEmits<{
    (e: 'update:type', value: IncidentFilterType | null): void;
    (e: 'update:company', value: string | null): void;
    (e: 'update:customer', value: string | null): void;
    (e: 'update:incident-date-start', value: string | null): void;
    (e: 'update:incident-date-end', value: string | null): void;
    (e: 'update:page-size', value: number | null): void;
    (e: 'update:page', value: number): void;
    (e: 'clear-filters'): void;
    (e: 'sort', value: { sortField: IncidentRegisterSortField; sortOrder: 1 | -1 }): void;
    (e: 'view', value: IncidentRegisterRecord): void;
    (e: 'edit', value: IncidentRegisterRecord): void;
}>();

const pageSizeOptionItems = computed(() => props.pageSizeOptions.map(value => ({ label: String(value), value })));
const firstRow = computed(() => (props.page - 1) * props.pageSize);
const columns: { field: IncidentRegisterSortField; label: string }[] = [
    { field: 'incidentNumber', label: 'Incident Number' },
    { field: 'incidentDate', label: 'Incident Date' },
    { field: 'company', label: 'Company' },
    { field: 'region', label: 'Region' },
    { field: 'customer', label: 'Customer' },
    { field: 'customerSite', label: 'Customer Site' },
    { field: 'status', label: 'Status' },
];

function onPage(event: { page: number }) {
    emit('update:page', event.page + 1);
}

function onSort(event: { sortField: IncidentRegisterSortField; sortOrder: 1 | -1 | 0 }) {
    emit('sort', {
        sortField: event.sortField || 'incidentDate',
        sortOrder: event.sortOrder === 1 ? 1 : -1,
    });
}

function toggleSort(field: IncidentRegisterSortField) {
    if (props.sortField === field) {
        emit('sort', {
            sortField: field,
            sortOrder: props.sortOrder === 1 ? -1 : 1,
        });
        return;
    }

    emit('sort', {
        sortField: field,
        sortOrder: 1,
    });
}

function normalizeString(value: string | null | undefined) {
    return value ? value : null;
}

function statusTone(value: IncidentRegisterRecord['status']) {
    if (value === 'Open') return 'warning';
    if (value === 'In Review') return 'info';
    return 'success';
}

function sortIcon(field: IncidentRegisterSortField) {
    if (props.sortField !== field) return 'pi pi-sort-alt text-slate-500';
    return props.sortOrder === 1 ? 'pi pi-sort-amount-up-alt text-blue-300' : 'pi pi-sort-amount-down text-blue-300';
}
</script>
