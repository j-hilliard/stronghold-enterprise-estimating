<template>
    <div class="space-y-6">
        <BasePageHeader
            title="Investigations"
            subtitle="Track ownership, due dates, and investigation progress across the safety program."
            icon="pi pi-search"
        >
            <Button label="Create Investigation" icon="pi pi-plus" @click="showCreateDialog = true" />
        </BasePageHeader>

        <!-- Create Investigation Dialog -->
        <Dialog
            v-model:visible="showCreateDialog"
            header="Create Investigation"
            modal
            :style="{ width: '500px' }"
            @hide="resetCreateDialog"
        >
            <div class="space-y-3 pt-2">
                <p class="m-0 text-sm text-slate-300">Search by incident number, job number, or client code.</p>

                <div class="flex gap-2">
                    <InputText
                        v-model="incidentSearchTerm"
                        placeholder="e.g. CCI-2026-0005"
                        class="flex-1"
                        :disabled="creating"
                        @keyup.enter="searchIncident"
                    />
                    <Button label="Find" icon="pi pi-search" :loading="searching" :disabled="!incidentSearchTerm.trim() || creating" @click="searchIncident" />
                </div>

                <p class="m-0 text-xs text-slate-400">
                    Don't see it?
                    <button type="button" class="text-blue-400 hover:underline" @click="goToIncidents">Browse all incidents</button>
                </p>

                <!-- Live search results -->
                <div v-if="showResults && searchResults.length" class="rounded-xl border border-slate-700 bg-slate-900 max-h-48 overflow-y-auto divide-y divide-slate-800">
                    <button
                        v-for="incident in searchResults"
                        :key="incident.id"
                        type="button"
                        class="w-full px-4 py-2 text-left text-sm hover:bg-slate-800 flex justify-between items-center gap-4"
                        @click="selectSearchResult(incident)"
                    >
                        <span class="font-medium text-white">{{ incident.incidentNumber }}</span>
                        <span class="text-slate-400 text-xs shrink-0">{{ incident.incidentDate }} · {{ incident.status }}</span>
                    </button>
                </div>
                <p v-else-if="showResults && !searchResults.length && incidentSearchTerm.length >= 2" class="m-0 text-sm text-slate-400">
                    No incidents found matching "{{ incidentSearchTerm }}".
                </p>

                <div v-if="incidentSearchError" class="rounded-xl border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-300">
                    {{ incidentSearchError }}
                </div>

                <div v-if="foundIncident" class="rounded-xl border border-green-500/30 bg-green-500/10 px-4 py-3 space-y-1">
                    <p class="m-0 text-xs font-semibold uppercase tracking-[0.2em] text-green-300">Incident Found</p>
                    <p class="m-0 text-base font-semibold text-white">{{ foundIncident.incidentNumber }}</p>
                    <p class="m-0 text-sm text-slate-300">Date: {{ foundIncident.incidentDate }}</p>
                    <p class="m-0 text-sm text-slate-300">Status: {{ foundIncident.status }}</p>
                </div>
            </div>

            <template #footer>
                <Button label="Cancel" text :disabled="creating" @click="showCreateDialog = false" />
                <Button
                    label="Create Investigation"
                    icon="pi pi-plus"
                    :loading="creating"
                    :disabled="!foundIncident"
                    @click="confirmCreateInvestigation"
                />
            </template>
        </Dialog>

        <div v-if="activeFilterLabel" class="rounded-2xl border border-blue-500/25 bg-blue-500/10 px-5 py-4 text-sm text-blue-100 shadow-lg shadow-slate-950/15">
            <div class="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
                <div class="flex items-center gap-3">
                    <i class="pi pi-filter-fill text-blue-300" />
                    <div>
                        <p class="m-0 text-xs font-semibold uppercase tracking-[0.24em] text-blue-200">KPI Filter Applied</p>
                        <p class="m-0 text-sm text-blue-100">Showing investigations filtered to {{ activeFilterLabel.toLowerCase() }}.</p>
                    </div>
                </div>
                <Button label="Clear KPI Filter" text class="!text-blue-100" @click="clearFilters" />
            </div>
        </div>

        <Card class="border border-slate-700/70 bg-slate-800/70 shadow-lg shadow-slate-950/20">
            <template #content>
                <div class="grid gap-4 p-6 md:grid-cols-3">
                    <div class="rounded-2xl border border-slate-700/70 bg-slate-900/50 p-4">
                        <p class="mb-2 text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">Visible Investigations</p>
                        <p class="m-0 text-3xl font-semibold text-white">{{ filteredInvestigations.length }}</p>
                        <p class="mt-2 text-sm text-slate-400">{{ activeFilterDescription }}</p>
                    </div>
                    <div class="rounded-2xl border border-slate-700/70 bg-slate-900/50 p-4">
                        <p class="mb-2 text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">Open Investigations</p>
                        <p class="m-0 text-3xl font-semibold text-white">{{ openOrPastDueCount }}</p>
                        <p class="mt-2 text-sm text-slate-400">Investigations with Open status across all data.</p>
                    </div>
                    <div class="rounded-2xl border border-slate-700/70 bg-slate-900/50 p-4">
                        <p class="mb-2 text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">High Priority</p>
                        <p class="m-0 text-3xl font-semibold text-white">{{ highPriorityCount }}</p>
                        <p class="mt-2 text-sm text-slate-400">High priority investigations in the current view.</p>
                    </div>
                </div>
            </template>
        </Card>

        <BasePageSubheader title="Investigation Tracker">
            <div class="flex items-center gap-2">
                <Tag :value="`${filteredInvestigations.length} visible`" severity="contrast" />
            </div>
        </BasePageSubheader>

        <InvestigationsTable
            :rows="filteredInvestigations"
            :search="search"
            :selected-status="selectedStatus"
            :active-filter-label="activeFilterLabel"
            :show-clear-filters="showClearFilters"
            :empty-message="emptyMessage"
            @update:search="search = $event"
            @update:status="updateStatusFilter"
            @clear-filters="clearFilters"
            @view="viewInvestigation"
            @edit="editInvestigation"
        />
    </div>
</template>

<script setup lang="ts">
import BasePageHeader from '@/components/layout/BasePageHeader.vue';
import BasePageSubheader from '@/components/layout/BasePageSubheader.vue';
import InvestigationsTable from '@/modules/safety-application/features/dashboard/components/tables/InvestigationsTable.vue';
import {
    createInvestigation,
    fetchInvestigationByIncident,
    fetchInvestigationList,
    findIncidentByNumber,
    searchIncidents,
    type IncidentLookupItem,
    type InvestigationStatus,
    type SafetyInvestigationRecord,
} from '@/modules/safety-application/features/dashboard/services/investigationService';
import { fetchMyProfile } from '@/modules/safety-application/features/dashboard/services/userProfileService';
import { useApiStore } from '@/stores/apiStore';
import Card from 'primevue/card';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import Tag from 'primevue/tag';
import { useToast } from 'primevue/usetoast';
import { computed, onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';

const route = useRoute();
const router = useRouter();
const apiStore = useApiStore();
const toast = useToast();

const search = ref('');
const investigations = ref<SafetyInvestigationRecord[]>([]);
const loadError = ref(false);

// Create dialog state
const showCreateDialog = ref(false);
const incidentSearchTerm = ref('');
const foundIncident = ref<IncidentLookupItem | null>(null);
const incidentSearchError = ref('');
const searching = ref(false);
const creating = ref(false);
const searchResults = ref<IncidentLookupItem[]>([]);
const showResults = ref(false);

// Debounced live search
let debounceTimer: ReturnType<typeof setTimeout> | null = null;
watch(incidentSearchTerm, (term) => {
    foundIncident.value = null;
    incidentSearchError.value = '';
    if (debounceTimer) clearTimeout(debounceTimer);
    if (term.trim().length < 2) {
        searchResults.value = [];
        showResults.value = false;
        return;
    }
    debounceTimer = setTimeout(async () => {
        searching.value = true;
        try {
            searchResults.value = await searchIncidents(apiStore.api, term.trim());
            showResults.value = true;
        } catch {
            searchResults.value = [];
        } finally {
            searching.value = false;
        }
    }, 300);
});

onMounted(async () => {
    try {
        const hasStatusFilter = !!route.query.status;
        const [investigationList, profile] = await Promise.all([
            fetchInvestigationList(apiStore.api),
            !hasStatusFilter ? fetchMyProfile(apiStore.api).catch(() => null) : Promise.resolve(null),
        ]);
        investigations.value = investigationList;
        if (!hasStatusFilter && profile?.defaultIncidentStatuses?.length) {
            await router.replace({
                path: '/safety-application/investigations',
                query: { status: profile.defaultIncidentStatuses[0] },
            });
        }
    } catch {
        loadError.value = true;
    }
});

const selectedStatus = computed<InvestigationStatus | 'Open' | ''>(() => {
    const normalized = String(route.query.status || '').trim().toLowerCase();
    if (normalized === 'open') return 'Open';
    if (normalized === 'assigned') return 'Assigned';
    if (normalized === 'in progress' || normalized === 'inprogress') return 'In Progress';
    if (normalized === 'completed' || normalized === 'complete') return 'Completed';
    if (normalized === 'closed') return 'Closed';
    return '';
});

const filteredInvestigations = computed(() => {
    const searchValue = search.value.trim().toLowerCase();

    return investigations.value.filter(investigation => {
        const matchesStatus =
            !selectedStatus.value ||
            (selectedStatus.value === 'Open'
                ? investigation.status === 'Assigned' || investigation.status === 'In Progress'
                : investigation.status === selectedStatus.value);
        const matchesSearch =
            !searchValue ||
            investigation.investigationNumber.toLowerCase().includes(searchValue) ||
            investigation.incidentNumber.toLowerCase().includes(searchValue) ||
            investigation.owner.toLowerCase().includes(searchValue) ||
            investigation.priority.toLowerCase().includes(searchValue);

        return matchesStatus && matchesSearch;
    });
});

const openOrPastDueCount = computed(() =>
    investigations.value.filter(item => item.status === 'Assigned' || item.status === 'In Progress').length
);
const highPriorityCount = computed(() => filteredInvestigations.value.filter(item => item.priority === 'High').length);

const activeFilterLabel = computed(() => {
    if (!selectedStatus.value) return '';
    if (selectedStatus.value === 'Open') return 'Open (Assigned + In Progress)';
    return `${selectedStatus.value} Investigations`;
});

const activeFilterDescription = computed(() => {
    if (activeFilterLabel.value) {
        return `Focused on investigations with ${selectedStatus.value.toLowerCase()} status.`;
    }

    return 'Showing all investigations currently in the dashboard dataset.';
});

const showClearFilters = computed(() => !!selectedStatus.value || !!search.value.trim());

const emptyMessage = computed(() => {
    if (selectedStatus.value || search.value.trim()) {
        return 'No investigations match the active filters.';
    }

    return 'No investigations found.';
});

watch(
    () => route.fullPath,
    () => {
        search.value = '';
    },
    { once: true }
);

function selectSearchResult(incident: IncidentLookupItem) {
    incidentSearchTerm.value = incident.incidentNumber;
    searchResults.value = [];
    showResults.value = false;
    void searchIncident();
}

function goToIncidents() {
    showCreateDialog.value = false;
    void router.push('/safety-application/incidents');
}

async function searchIncident() {
    const term = incidentSearchTerm.value.trim();
    if (!term) return;

    incidentSearchError.value = '';
    foundIncident.value = null;
    searching.value = true;

    try {
        const incident = await findIncidentByNumber(apiStore.api, term);
        if (!incident) {
            incidentSearchError.value = `No incident found with number "${term.toUpperCase()}".`;
            return;
        }

        // Check if investigation already exists for this incident
        const existing = await fetchInvestigationByIncident(apiStore.api, incident.id);
        if (existing) {
            incidentSearchError.value = `An investigation already exists for ${incident.incidentNumber} (${existing.investigationNumber ?? existing.investigationId}).`;
            return;
        }

        foundIncident.value = incident;
    } catch {
        incidentSearchError.value = 'Error searching for incident. Please try again.';
    } finally {
        searching.value = false;
    }
}

async function confirmCreateInvestigation() {
    if (!foundIncident.value) return;

    creating.value = true;
    try {
        const created = await createInvestigation(apiStore.api, { incidentId: foundIncident.value.id });
        showCreateDialog.value = false;
        toast.add({
            severity: 'success',
            summary: 'Investigation Created',
            detail: `${created.investigationNumber} linked to ${created.incidentNumber}.`,
            life: 3000,
        });
        await router.push(`/safety-application/investigations/${created.investigationId}`);
    } catch {
        incidentSearchError.value = 'Failed to create investigation. Please try again.';
    } finally {
        creating.value = false;
    }
}

function resetCreateDialog() {
    incidentSearchTerm.value = '';
    foundIncident.value = null;
    incidentSearchError.value = '';
    searching.value = false;
    creating.value = false;
    searchResults.value = [];
    showResults.value = false;
    if (debounceTimer) clearTimeout(debounceTimer);
}

async function updateStatusFilter(value: InvestigationStatus | '' | null) {
    await router.replace({
        path: '/safety-application/investigations',
        query: value ? { ...route.query, status: value } : omitQueryKey(route.query, 'status'),
    });
}

async function clearFilters() {
    search.value = '';
    await router.replace({ path: '/safety-application/investigations' });
}

function viewInvestigation(investigation: SafetyInvestigationRecord) {
    void router.push(`/safety-application/investigations/${investigation.investigationId}`);
}

function editInvestigation(investigation: SafetyInvestigationRecord) {
    void router.push(`/safety-application/investigations/${investigation.investigationId}?mode=edit`);
}

function omitQueryKey(query: Record<string, unknown>, key: string) {
    const next = { ...query };
    delete next[key];
    return next;
}
</script>
