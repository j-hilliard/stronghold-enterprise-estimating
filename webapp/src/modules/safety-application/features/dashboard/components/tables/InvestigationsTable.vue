<template>
    <BaseDataTable :value="rows" :emptyMessage="emptyMessage" responsiveLayout="scroll" stripedRows>
        <template #filters>
            <div class="flex w-full flex-col gap-3 rounded-2xl border border-slate-700/70 bg-slate-900/50 p-4 xl:flex-row xl:items-center">
                <div class="flex flex-1 flex-col gap-3 md:flex-row">
                    <span class="p-input-icon-left w-full md:w-72">
                        <i class="pi pi-search text-slate-500" />
                        <InputText
                            v-model="searchValue"
                            placeholder="Search investigations..."
                            class="w-full"
                            @update:modelValue="emit('update:search', searchValue)"
                        />
                    </span>
                    <Dropdown
                        v-model="statusValue"
                        :options="statusOptions"
                        optionLabel="label"
                        optionValue="value"
                        placeholder="All Statuses"
                        class="w-full md:w-56"
                        :showClear="!!statusValue"
                        @change="emit('update:status', statusValue)"
                    />
                </div>
                <div class="flex flex-wrap items-center gap-2 xl:ml-auto">
                    <Tag v-if="activeFilterLabel" :value="activeFilterLabel" severity="info" class="!rounded-full !px-3 !py-2" />
                    <Button v-if="showClearFilters" label="Clear Filters" text @click="emit('clear-filters')" />
                </div>
            </div>
        </template>

        <Column field="investigationNumber" header="Investigation #" />
        <Column field="incidentNumber" header="Related Incident" />
        <Column field="owner" header="Owner" />
        <Column field="openDate" header="Open Date">
            <template #body="{ data }">
                {{ formatDateMMDDYYYY(data.openDate) }}
            </template>
        </Column>
        <Column field="dueDate" header="Due Date">
            <template #body="{ data }">
                {{ formatDateMMDDYYYY(data.dueDate) }}
            </template>
        </Column>
        <Column field="status" header="Status">
            <template #body="{ data }">
                <Tag :value="data.status" :severity="statusTone(data.status)" />
            </template>
        </Column>
        <Column field="priority" header="Priority">
            <template #body="{ data }">
                <Tag :value="data.priority" :severity="priorityTone(data.priority)" />
            </template>
        </Column>
        <Column header="Actions" style="width: 120px;">
            <template #body="{ data }">
                <BaseButtonIconView @click="emit('view', data)" />
                <BaseButtonIconEdit @click="emit('edit', data)" />
            </template>
        </Column>
    </BaseDataTable>
</template>

<script setup lang="ts">
import BaseButtonIconEdit from '@/components/buttons/BaseButtonIconEdit.vue';
import BaseButtonIconView from '@/components/buttons/BaseButtonIconView.vue';
import BaseDataTable from '@/components/tables/BaseDataTable.vue';
import { formatDateMMDDYYYY } from '@/utils.ts';
import { computed, ref, watch } from 'vue';
import type { InvestigationStatus, SafetyInvestigationRecord } from '@/modules/safety-application/features/dashboard/services/investigationService';

const props = defineProps<{
    rows: SafetyInvestigationRecord[];
    search: string;
    selectedStatus: InvestigationStatus | '';
    emptyMessage: string;
    activeFilterLabel?: string;
    showClearFilters?: boolean;
}>();

const emit = defineEmits<{
    (e: 'update:search', value: string): void;
    (e: 'update:status', value: InvestigationStatus | '' | null): void;
    (e: 'clear-filters'): void;
    (e: 'view', value: SafetyInvestigationRecord): void;
    (e: 'edit', value: SafetyInvestigationRecord): void;
}>();

const searchValue = ref(props.search);
const statusValue = ref<InvestigationStatus | '' | null>(props.selectedStatus || '');

watch(() => props.search, value => {
    searchValue.value = value;
});

watch(() => props.selectedStatus, value => {
    statusValue.value = value || '';
});

const statusOptions = computed(() => [
    { label: 'Open (Assigned + In Progress)', value: 'Open' },
    { label: 'Assigned', value: 'Assigned' },
    { label: 'In Progress', value: 'In Progress' },
    { label: 'Completed', value: 'Completed' },
    { label: 'Closed', value: 'Closed' },
]);

function statusTone(value: InvestigationStatus) {
    if (value === 'Assigned') return 'warning';
    if (value === 'In Progress') return 'info';
    if (value === 'Completed') return 'success';
    if (value === 'Closed') return 'secondary';
    return 'contrast';
}

function priorityTone(value: SafetyInvestigationRecord['priority']) {
    if (value === 'High') return 'danger';
    if (value === 'Medium') return 'warning';
    return 'success';
}
</script>
