<template>
    <div>
        <BasePageHeader icon="pi pi-list" title="Estimates" subtitle="Manage all estimates and bids">
            <BaseButtonCreate label="New Estimate" @click="router.push('/estimating/estimates/new')" />
        </BasePageHeader>
        <BaseDataTable :value="[]" emptyMessage="No estimates yet. Click New Estimate to get started." />
    </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router';
import BasePageHeader from '@/components/layout/BasePageHeader.vue';
import BaseButtonCreate from '@/components/buttons/BaseButtonCreate.vue';
import BaseDataTable from '@/components/tables/BaseDataTable.vue';

const router = useRouter();
</script>
