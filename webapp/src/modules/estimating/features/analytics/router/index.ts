import { apps } from '@/apps.ts';

export const analyticsRoutes = [
    {
        path: 'analytics/revenue',
        name: `${apps.estimating.baseSlug}-revenue`,
        component: () => import('@/modules/estimating/features/analytics/views/RevenueForecastView.vue'),
        meta: { title: 'Revenue Forecast' },
    },
    {
        path: 'analytics/manpower',
        name: `${apps.estimating.baseSlug}-manpower`,
        component: () => import('@/modules/estimating/features/analytics/views/ManpowerForecastView.vue'),
        meta: { title: 'Manpower Forecast' },
    },
    {
        path: 'calendar',
        name: `${apps.estimating.baseSlug}-calendar`,
        component: () => import('@/modules/estimating/features/analytics/views/CalendarView.vue'),
        meta: { title: 'Calendar' },
    },
];
