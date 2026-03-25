<template>
    <Card class="h-full border border-slate-700/70 bg-slate-800/70 transition-all duration-200 hover:-translate-y-1 hover:border-blue-400/60 hover:shadow-xl hover:shadow-slate-950/30">
        <template #content>
            <div class="flex h-full flex-col gap-5 p-5">
                <div class="flex items-start justify-between gap-4">
                    <div class="flex h-14 w-14 items-center justify-center rounded-2xl bg-blue-500/15 text-blue-300">
                        <i :class="[icon, 'text-2xl']" />
                    </div>
                    <Tag :value="badge" severity="info" class="!rounded-full !bg-blue-500/15 !px-3 !py-1 !text-xs !font-semibold !text-blue-200" />
                </div>
                <div class="space-y-2">
                    <h3 class="m-0 text-xl font-semibold text-white">{{ title }}</h3>
                    <p class="m-0 text-sm leading-6 text-slate-400">{{ description }}</p>
                </div>
                <div class="mt-auto">
                    <RouterLink :to="to" class="inline-flex">
                        <Button :label="buttonLabel" icon="pi pi-arrow-right" icon-pos="right" class="p-button-sm" />
                    </RouterLink>
                </div>
            </div>
        </template>
    </Card>
</template>

<script setup lang="ts">
import Button from 'primevue/button';
import Card from 'primevue/card';
import Tag from 'primevue/tag';
defineProps<{
    title: string;
    description: string;
    icon: string;
    badge: string;
    buttonLabel?: string;
    to: string | Record<string, unknown>;
}>();
</script>
