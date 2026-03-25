<template>
    <div class="space-y-6">
        <BasePageHeader
            title="Reference Lookups"
            subtitle="Manage the controlled values that support safety forms and dashboards."
            icon="pi pi-book"
        />

        <Card class="border border-slate-700/70 bg-slate-800/70 shadow-lg shadow-slate-950/20">
            <template #content>
                <div class="space-y-4 p-6">
                    <h2 class="m-0 text-xl font-semibold text-white">Reference lookup placeholder</h2>
                    <p class="m-0 max-w-3xl text-sm leading-7 text-slate-300">
                        This route is ready for the lookup management experience that will sit behind the Safety Application landing page.
                    </p>
                </div>
            </template>
        </Card>
    </div>
</template>

<script setup lang="ts">
import BasePageHeader from '@/components/layout/BasePageHeader.vue';
import Card from 'primevue/card';
</script>
