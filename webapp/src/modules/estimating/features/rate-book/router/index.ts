import { apps } from '@/apps.ts';

export const rateBookRoutes = [
    {
        path: 'rate-books',
        name: `${apps.estimating.baseSlug}-rate-books`,
        component: () => import('@/modules/estimating/features/rate-book/views/RateBookView.vue'),
        meta: { title: 'Rate Books' },
    },
];
