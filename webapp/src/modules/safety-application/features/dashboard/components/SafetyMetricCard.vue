<template>
    <component :is="wrapperTag" v-bind="wrapperProps" class="block h-full no-underline">
        <Card
            v-tooltip.top="tooltip || null"
            :class="[
                'h-full border border-slate-700/70 bg-slate-800/70 shadow-lg shadow-slate-950/20 transition-all duration-200',
                isClickable ? 'cursor-pointer hover:-translate-y-0.5 hover:border-blue-400/50 hover:shadow-xl hover:shadow-slate-950/35' : '',
            ]"
        >
            <template #content>
                <div class="flex h-full flex-col gap-4 p-5">
                    <div class="flex items-start justify-between gap-3">
                        <div>
                            <p class="mb-2 text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">{{ label }}</p>
                            <p :class="[showTrend ? 'mb-1' : 'mb-0', 'text-3xl font-semibold text-white']">{{ value }}</p>
                            <p v-if="detail" class="text-sm text-slate-400">{{ detail }}</p>
                        </div>
                        <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-blue-500/15 text-blue-300">
                            <i :class="[icon, 'text-xl']" />
                        </div>
                    </div>
                    <div v-if="showTrend" class="mt-auto flex items-center gap-2 text-sm">
                        <i :class="[trendIcon, trendTone]" />
                        <span :class="trendTone">{{ trend }}</span>
                    </div>
                </div>
            </template>
        </Card>
    </component>
</template>

<script setup lang="ts">
import Card from 'primevue/card';
import { computed } from 'vue';
import type { RouteLocationRaw } from 'vue-router';

const props = defineProps<{
    label: string;
    value: string | number;
    detail?: string;
    icon: string;
    trend?: string;
    trendDirection?: 'up' | 'down' | 'neutral';
    tooltip?: string;
    to?: RouteLocationRaw;
}>();

const isClickable = computed(() => !!props.to);

const wrapperTag = computed(() => (props.to ? 'RouterLink' : 'div'));

const wrapperProps = computed(() => (props.to ? { to: props.to } : {}));

const showTrend = computed(() => !!props.trend);

const trendIcon = computed(() => {
    if (props.trendDirection === 'down') return 'pi pi-arrow-down-right';
    if (props.trendDirection === 'neutral') return 'pi pi-minus';
    return 'pi pi-arrow-up-right';
});

const trendTone = computed(() => {
    if (props.trendDirection === 'down') return 'text-emerald-300';
    if (props.trendDirection === 'neutral') return 'text-slate-300';
    return 'text-amber-300';
});
</script>
