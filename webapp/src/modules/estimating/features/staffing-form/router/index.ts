import { apps } from '@/apps.ts';

export const staffingFormRoutes = [
    {
        path: 'staffing-plans/new',
        name: `${apps.estimating.baseSlug}-staffing-new`,
        component: () => import('@/modules/estimating/features/staffing-form/views/StaffingFormView.vue'),
        meta: { title: 'New Staffing Plan' },
    },
    {
        path: 'staffing-plans/:id',
        name: `${apps.estimating.baseSlug}-staffing-edit`,
        component: () => import('@/modules/estimating/features/staffing-form/views/StaffingFormView.vue'),
        meta: { title: 'Edit Staffing Plan' },
    },
];
