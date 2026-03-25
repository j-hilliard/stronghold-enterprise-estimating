<template>
    <div>
        <BasePageHeader icon="pi pi-users" title="Staffing Plans" subtitle="Manage field labor forecasting plans">
            <BaseButtonCreate label="New Staffing Plan" @click="router.push('/estimating/staffing-plans/new')" />
        </BasePageHeader>
        <BaseDataTable :value="[]" emptyMessage="No staffing plans yet." />
    </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router';
import BasePageHeader from '@/components/layout/BasePageHeader.vue';
import BaseButtonCreate from '@/components/buttons/BaseButtonCreate.vue';
import BaseDataTable from '@/components/tables/BaseDataTable.vue';

const router = useRouter();
</script>
