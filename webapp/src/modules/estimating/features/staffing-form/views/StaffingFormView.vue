<template>
    <div>
        <BasePageHeader icon="pi pi-users" title="Staffing Plan" subtitle="Coming soon — staffing plan editor">
            <BaseButtonCancel label="Back" @click="router.push('/estimating/staffing-plans')" />
        </BasePageHeader>
    </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router';
import BasePageHeader from '@/components/layout/BasePageHeader.vue';
import BaseButtonCancel from '@/components/buttons/BaseButtonCancel.vue';

const router = useRouter();
</script>
