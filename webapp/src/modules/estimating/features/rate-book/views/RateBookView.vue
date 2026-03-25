<template>
    <div>
        <BasePageHeader icon="pi pi-dollar" title="Rate Books" subtitle="Manage customer billing rate libraries" />
        <BaseDataTable :value="[]" emptyMessage="No rate books found." />
    </div>
</template>

<script setup lang="ts">
import BasePageHeader from '@/components/layout/BasePageHeader.vue';
import BaseDataTable from '@/components/tables/BaseDataTable.vue';
</script>
