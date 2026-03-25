<template>
    <div class="space-y-6">
        <BasePageHeader
            icon="pi pi-clock"
            title="Investigation Workflow"
            subtitle="Coming soon"
        >
            <Button
                label="Back To Incident"
                text
                icon="pi pi-arrow-left"
                @click="goBackToIncident"
            />
        </BasePageHeader>

        <Card class="border border-slate-700/70 bg-slate-800/70 shadow-lg shadow-slate-950/20">
            <template #content>
                <div class="space-y-4 p-6">
                    <p class="m-0 text-sm text-slate-300">
                        Investigation form flow is being wired. This is a placeholder so you can validate navigation.
                    </p>
                    <div class="rounded-2xl border border-slate-700/70 bg-slate-900/50 p-4 space-y-2">
                        <p class="m-0 text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Incident</p>
                        <p class="m-0 text-lg font-semibold text-white">{{ incidentNumber || 'Not provided' }}</p>
                        <p class="m-0 text-sm text-slate-400">Incident Id: {{ incidentId || 'Not provided' }}</p>
                    </div>
                    <div class="flex flex-wrap gap-3">
                        <Button label="Back To Incidents" icon="pi pi-list" @click="router.push('/safety-application/incidents')" />
                        <Button label="Open Investigation Forms" outlined icon="pi pi-search" @click="router.push('/safety-application/investigations')" />
                    </div>
                </div>
            </template>
        </Card>
    </div>
</template>

<script setup lang="ts">
import BasePageHeader from '@/components/layout/BasePageHeader.vue';
import Button from 'primevue/button';
import Card from 'primevue/card';
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';

const route = useRoute();
const router = useRouter();

const incidentId = computed(() => String(route.query.incidentId || ''));
const incidentNumber = computed(() => String(route.query.incidentNumber || ''));

function goBackToIncident() {
    if (incidentId.value) {
        void router.push(`/safety-application/incidents/${incidentId.value}`);
        return;
    }

    void router.push('/safety-application/incidents');
}
</script>
