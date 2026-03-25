<template>
    <div>
        <BasePageHeader icon="pi pi-calculator" title="Estimate" subtitle="Coming soon — estimate editor">
            <BaseButtonCancel label="Back" @click="router.push('/estimating/estimates')" />
        </BasePageHeader>
    </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router';
import BasePageHeader from '@/components/layout/BasePageHeader.vue';
import BaseButtonCancel from '@/components/buttons/BaseButtonCancel.vue';

const router = useRouter();
</script>
