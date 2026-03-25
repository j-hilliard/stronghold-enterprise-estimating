<template>
    <div>
        <BasePageHeader
            icon="pi pi-exclamation-triangle"
            :title="isEdit ? 'Edit Incident Report' : 'New Incident Report'"
            :subtitle="isEdit ? store.form.incidentNumber ?? '' : 'Complete the incident report form'"
        >
            <BaseButtonCancel label="Cancel" @click="router.push(incidentListPath)" />
            <Button
                v-if="showBeginInvestigationButton"
                label="Begin Investigation"
                icon="pi pi-search"
                :loading="startingInvestigation"
                @click="beginInvestigation"
            />
            <BaseButtonSave
                v-else
                :label="isEdit ? 'Save Changes' : 'Save Draft'"
                :loading="store.saving"
                @click="saveDraft"
            />
        </BasePageHeader>

        <div v-if="store.loading" class="flex justify-center py-12">
            <ProgressSpinner />
        </div>
        <div v-else>
            <IncidentFormPage1 />

            <div v-if="showSubmitActions" class="flex flex-wrap justify-end gap-3 mt-6">
                <Button
                    label="Submit"
                    icon="pi pi-check"
                    :loading="store.saving"
                    @click="submitForm"
                />
                <Button
                    v-if="requiresInvestigation"
                    label="Submit & Start Investigation"
                    icon="pi pi-search"
                    outlined
                    :loading="store.saving"
                    @click="submitAndStartInvestigation"
                />
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { apps } from '@/apps.ts';
import { createInvestigation, fetchInvestigationByIncident } from '@/modules/safety-application/features/dashboard/services/investigationService';
import { useIncidentStore } from '@/modules/incident-management/stores/incidentStore';
import { useReferenceDataStore } from '@/modules/incident-management/stores/referenceDataStore';
import { useApiStore } from '@/stores/apiStore';
import BasePageHeader from '@/components/layout/BasePageHeader.vue';
import BaseButtonCancel from '@/components/buttons/BaseButtonCancel.vue';
import BaseButtonSave from '@/components/buttons/BaseButtonSave.vue';
import IncidentFormPage1 from '@/modules/incident-management/features/incident-form/components/IncidentFormPage1.vue';
import { useToast } from 'primevue/usetoast';

const router = useRouter();
const route = useRoute();
const store = useIncidentStore();
const refStore = useReferenceDataStore();
const apiStore = useApiStore();
const toast = useToast();
const startingInvestigation = ref(false);

const FORMAL_INVESTIGATION_FALLBACK = 'a9f1c3d2-0011-4e88-b000-222200000002';
const FULL_CAUSE_MAP_FALLBACK = 'a9f1c3d2-0011-4e88-b000-222200000003';

const isEdit = computed(() => !!route.params.id);
const normalizedStatus = computed(() => normalizeStatus(store.form.status));
const requiresInvestigation = computed(() => {
    const opts = refStore.getOptionsByType('investigation_required');
    const fId = opts.find(o => o.name === 'Formal Investigation')?.id ?? FORMAL_INVESTIGATION_FALLBACK;
    const fcId = opts.find(o => o.name === 'Full Cause Map')?.id ?? FULL_CAUSE_MAP_FALLBACK;
    return !!(store.form.referenceIds?.includes(fId) || store.form.referenceIds?.includes(fcId));
});
const showBeginInvestigationButton = computed(
    () => isEdit.value && normalizedStatus.value === 'open' && requiresInvestigation.value,
);
const showSubmitActions = computed(
    () => !isEdit.value && (normalizedStatus.value === '' || normalizedStatus.value === 'draft'),
);
const incidentListPath = computed(() => {
    if (route.fullPath.startsWith(`/${apps.safetyApplication.baseSlug}/`)) {
        return `/${apps.safetyApplication.baseSlug}/incidents`;
    }

    return `/${apps.incidentManagement.baseSlug}/incidents`;
});

onMounted(async () => {
    store.resetForm();
    await refStore.loadAll();
    if (isEdit.value) {
        await store.loadIncident(route.params.id as string);
        if (!store.currentIncident) {
            router.replace(incidentListPath.value);
            return;
        }
        if (store.form.companyId) {
            await refStore.loadRegions(store.form.companyId);
        }
    }
});

async function saveDraft() {
    if (isEdit.value) {
        return await store.updateIncident(route.params.id as string);
    } else {
        const created = await store.createIncident();
        if (created?.id) {
            router.replace(`${incidentListPath.value}/${created.id}`);
            return created;
        }
        return null;
    }
}

async function submitForm() {
    const previousStatus = store.form.status;
    store.form.status = 'Open';
    const saved = await saveDraft();
    if (saved?.id) {
        router.push(incidentListPath.value);
    } else {
        store.form.status = previousStatus;
    }
}

async function submitAndStartInvestigation() {
    const previousStatus = store.form.status;
    store.form.status = 'Open';
    const saved = await saveDraft();

    if (!saved?.id) {
        store.form.status = previousStatus;
        return;
    }

    try {
        const createdInvestigation = await createInvestigation(apiStore.api, {
            incidentId: saved.id,
        });
        await router.push(`/safety-application/investigations/${createdInvestigation.investigationId}`);
    } catch (error) {
        toast.add({
            severity: 'warn',
            summary: 'Incident Saved',
            detail: 'Incident submitted, but investigation start failed. Open the incident and use Begin Investigation to retry.',
            life: 6000,
        });
        console.error(error);
        await router.push(incidentListPath.value);
    }
}

async function beginInvestigation() {
    if (!isEdit.value || !route.params.id || startingInvestigation.value) {
        return;
    }

    startingInvestigation.value = true;
    const incidentId = String(route.params.id);

    try {
        const existing = await fetchInvestigationByIncident(apiStore.api, incidentId);
        if (existing) {
            await router.push(`/safety-application/investigations/${existing.investigationId}`);
            return;
        }

        const created = await createInvestigation(apiStore.api, { incidentId });
        await router.push(`/safety-application/investigations/${created.investigationId}`);
    } catch (error) {
        toast.add({
            severity: 'error',
            summary: 'Investigation Failed',
            detail: 'Could not start investigation. Please try again.',
            life: 5000,
        });
        console.error(error);
    } finally {
        startingInvestigation.value = false;
    }
}

function normalizeStatus(value: string | undefined | null) {
    return (value || '').trim().toLowerCase();
}
</script>
