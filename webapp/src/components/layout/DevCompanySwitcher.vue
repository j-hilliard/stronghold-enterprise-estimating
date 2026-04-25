<template>
    <div v-if="isDev" class="dev-switcher">
        <span class="dev-label">DEV</span>
        <select v-model="selected" @change="onSwitch" class="dev-select">
            <option v-for="opt in options" :key="opt.code" :value="opt.code">
                {{ opt.code }} — {{ opt.label }}
            </option>
        </select>
        <i class="pi pi-chevron-down dev-chevron" />
    </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';

const isDev = import.meta.env.DEV;

const options = [
    { code: 'CSL', label: 'Cat-Spec, Ltd.' },
    { code: 'ETS', label: 'Elite TA Specialists' },
    { code: 'STS', label: 'Specialty Tank Svc' },
    { code: 'STG', label: 'Stronghold Tower' },
    { code: 'GLOBAL', label: 'Global Analytics' },
];

const selected = ref(localStorage.getItem('ste_dev_company') ?? 'CSL');

function onSwitch() {
    localStorage.setItem('ste_dev_company', selected.value);
    window.location.reload();
}
</script>

<style scoped>
.dev-switcher {
    position: relative;
    display: flex;
    align-items: center;
    gap: 6px;
    background: #7c3aed22;
    border: 1px solid #7c3aed66;
    border-radius: 6px;
    padding: 3px 8px 3px 6px;
    margin-right: 10px;
    cursor: pointer;
}

.dev-label {
    font-size: 0.6rem;
    font-weight: 800;
    letter-spacing: 0.08em;
    color: #a78bfa;
    text-transform: uppercase;
    user-select: none;
}

.dev-select {
    appearance: none;
    background: transparent;
    border: none;
    color: #c4b5fd;
    font-size: 0.75rem;
    font-weight: 600;
    cursor: pointer;
    outline: none;
    padding-right: 2px;
}

.dev-select option {
    background: #1e1b4b;
    color: #e2e8f0;
}

.dev-chevron {
    font-size: 0.55rem;
    color: #7c3aed;
    pointer-events: none;
    margin-left: -4px;
}
</style>
