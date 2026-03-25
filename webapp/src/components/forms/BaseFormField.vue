<template>
    <div>
        <label class="block text-sm font-medium text-slate-300 mb-1">{{ label }}</label>
        <slot />
        <div v-if="normalizedErrors.length">
            <small class="text-red-600" v-for="(error, index) of normalizedErrors" :key="index">
                {{ error }}
            </small>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const props = withDefaults(defineProps<{
    label?: string;
    disabled?: boolean;
    errors?: Array<string | { $message?: string; [key: string]: unknown }>;
}>(), {
    label: '',
    disabled: false,
    errors: () => [],
});

const normalizedErrors = computed(() =>
    props.errors
        .map(error => {
            if (typeof error === 'string') {
                return error;
            }

            return error?.$message ?? '';
        })
        .filter(Boolean),
);
</script>
