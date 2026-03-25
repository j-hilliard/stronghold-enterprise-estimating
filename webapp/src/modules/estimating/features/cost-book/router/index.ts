import { apps } from '@/apps.ts';

export const costBookRoutes = [
    {
        path: 'cost-book',
        name: `${apps.estimating.baseSlug}-cost-book`,
        component: () => import('@/modules/estimating/features/cost-book/views/CostBookView.vue'),
        meta: { title: 'Cost Book' },
    },
];
