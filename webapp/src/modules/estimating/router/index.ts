import { apps } from '@/apps.ts';
import { getErrorRoutes } from '@/router/errorRoutes.ts';
import { estimateListRoutes } from '@/modules/estimating/features/estimate-list/router';
import { estimateFormRoutes } from '@/modules/estimating/features/estimate-form/router';
import { staffingListRoutes } from '@/modules/estimating/features/staffing-list/router';
import { staffingFormRoutes } from '@/modules/estimating/features/staffing-form/router';
import { rateBookRoutes } from '@/modules/estimating/features/rate-book/router';
import { costBookRoutes } from '@/modules/estimating/features/cost-book/router';
import { analyticsRoutes } from '@/modules/estimating/features/analytics/router';

export const estimatingRoutes = [
    { path: '', redirect: `/${apps.estimating.baseSlug}/estimates` },
    ...estimateListRoutes,
    ...estimateFormRoutes,
    ...staffingListRoutes,
    ...staffingFormRoutes,
    ...rateBookRoutes,
    ...costBookRoutes,
    ...analyticsRoutes,
    ...getErrorRoutes(apps.estimating),
];
