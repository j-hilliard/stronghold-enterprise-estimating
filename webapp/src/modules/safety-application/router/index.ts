import { apps } from '@/apps.ts';
import { getErrorRoutes } from '@/router/errorRoutes.ts';
import { dashboardRoutes } from '@/modules/safety-application/features/dashboard/router';

export const safetyApplicationRoutes = [
    { path: '', redirect: `${apps.safetyApplication.baseSlug}/dashboard` },
    ...dashboardRoutes,
    ...getErrorRoutes(apps.safetyApplication),
];
