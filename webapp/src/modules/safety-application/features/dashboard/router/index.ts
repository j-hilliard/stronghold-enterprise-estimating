import { apps } from '@/apps.ts';

export const dashboardRoutes = [
    {
        path: 'dashboard',
        name: `${apps.safetyApplication.baseSlug}-dashboard`,
        component: () => import('@/modules/safety-application/features/dashboard/views/SafetyLandingView.vue'),
        meta: {
            title: 'Dashboard',
            breadcrumbItems: () => [
                { label: apps.safetyApplication.name },
                { label: 'Dashboard' },
            ],
        },
    },
    {
        path: 'incident-forms',
        name: `${apps.safetyApplication.baseSlug}-incident-forms`,
        component: () => import('@/modules/safety-application/features/dashboard/views/IncidentFormsView.vue'),
        meta: {
            title: 'Incident Forms',
            breadcrumbItems: () => [
                { label: apps.safetyApplication.name },
                { label: 'Incident Forms' },
            ],
        },
    },
    {
        path: 'incidents',
        name: `${apps.safetyApplication.baseSlug}-incidents`,
        component: () => import('@/modules/safety-application/features/dashboard/views/IncidentFormsView.vue'),
        meta: {
            title: 'Incidents',
            breadcrumbItems: () => [
                { label: apps.safetyApplication.name },
                { label: 'Incidents' },
            ],
        },
    },
    {
        path: 'incidents/new',
        name: `${apps.safetyApplication.baseSlug}-incident-create`,
        component: () => import('@/modules/incident-management/features/incident-form/views/IncidentFormView.vue'),
        meta: {
            title: 'New Incident',
            breadcrumbItems: () => [
                { label: apps.safetyApplication.name },
                { label: 'Incidents', to: `/${apps.safetyApplication.baseSlug}/incidents` },
                { label: 'New Incident' },
            ],
        },
    },
    {
        path: 'incidents/:id',
        name: `${apps.safetyApplication.baseSlug}-incident-edit`,
        component: () => import('@/modules/incident-management/features/incident-form/views/IncidentFormView.vue'),
        meta: {
            title: 'Incident Detail',
            breadcrumbItems: () => [
                { label: apps.safetyApplication.name },
                { label: 'Incidents', to: `/${apps.safetyApplication.baseSlug}/incidents` },
                { label: 'Incident Detail' },
            ],
        },
    },
    {
        path: 'investigation-forms',
        name: `${apps.safetyApplication.baseSlug}-investigation-forms`,
        component: () => import('@/modules/safety-application/features/dashboard/views/InvestigationFormsView.vue'),
        meta: {
            title: 'Investigation Forms',
            breadcrumbItems: () => [
                { label: apps.safetyApplication.name },
                { label: 'Investigation Forms' },
            ],
        },
    },
    {
        path: 'investigations',
        name: `${apps.safetyApplication.baseSlug}-investigations`,
        component: () => import('@/modules/safety-application/features/dashboard/views/InvestigationFormsView.vue'),
        meta: {
            title: 'Investigations',
            breadcrumbItems: () => [
                { label: apps.safetyApplication.name },
                { label: 'Investigations' },
            ],
        },
    },
    {
        path: 'investigations/coming-soon',
        name: `${apps.safetyApplication.baseSlug}-investigations-coming-soon`,
        component: () => import('@/modules/safety-application/features/dashboard/views/InvestigationComingSoonView.vue'),
        meta: {
            title: 'Investigation Workflow',
            breadcrumbItems: () => [
                { label: apps.safetyApplication.name },
                { label: 'Investigations', to: `/${apps.safetyApplication.baseSlug}/investigations` },
                { label: 'Coming Soon' },
            ],
        },
    },
    {
        path: 'investigations/:id([0-9a-fA-F-]{36})',
        name: `${apps.safetyApplication.baseSlug}-investigation-detail`,
        component: () => import('@/modules/safety-application/features/dashboard/views/InvestigationDetailView.vue'),
        meta: {
            title: 'Investigation Detail',
            breadcrumbItems: () => [
                { label: apps.safetyApplication.name },
                { label: 'Investigations', to: `/${apps.safetyApplication.baseSlug}/investigations` },
                { label: 'Investigation Detail' },
            ],
        },
    },
    {
        path: 'reference-lookups',
        name: `${apps.safetyApplication.baseSlug}-reference-lookups`,
        component: () => import('@/modules/safety-application/features/dashboard/views/ReferenceLookupsView.vue'),
        meta: {
            title: 'Reference Lookups',
            breadcrumbItems: () => [
                { label: apps.safetyApplication.name },
                { label: 'Reference Lookups' },
            ],
        },
    },
    {
        path: 'administration/users',
        name: `${apps.safetyApplication.baseSlug}-administration-users`,
        component: () => import('@/modules/safety-application/features/dashboard/views/AdministrationUsersView.vue'),
        meta: {
            title: 'Users',
            breadcrumbItems: () => [
                { label: apps.safetyApplication.name },
                { label: 'Administration' },
                { label: 'Users' },
            ],
        },
    },
    {
        path: 'administration/security-roles',
        name: `${apps.safetyApplication.baseSlug}-administration-security-roles`,
        component: () => import('@/modules/safety-application/features/dashboard/views/SecurityRolesView.vue'),
        meta: {
            title: 'Security Roles',
            breadcrumbItems: () => [
                { label: apps.safetyApplication.name },
                { label: 'Administration' },
                { label: 'Security Roles' },
            ],
        },
    },
    {
        path: 'administration/profile',
        name: `${apps.safetyApplication.baseSlug}-administration-profile`,
        component: () => import('@/modules/safety-application/features/dashboard/views/AdministrationProfileView.vue'),
        meta: {
            title: 'Profile',
            breadcrumbItems: () => [
                { label: apps.safetyApplication.name },
                { label: 'Administration' },
                { label: 'Profile' },
            ],
        },
    },
];
