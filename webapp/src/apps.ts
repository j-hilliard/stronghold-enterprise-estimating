export const apps = {
    estimating: {
        baseSlug: 'estimating',
        name: 'Enterprise Estimating',
        description: 'Create and manage estimates, staffing plans, rate books, and cost analysis',
        icon: 'pi pi-calculator',
        menu: {
            user: [
                { label: 'Estimates', icon: 'pi pi-fw pi-list', to: '/estimating/estimates' },
                { label: 'New Estimate', icon: 'pi pi-fw pi-plus', to: '/estimating/estimates/new' },
                { label: 'Staffing Plans', icon: 'pi pi-fw pi-users', to: '/estimating/staffing-plans' },
                { label: 'New Staffing Plan', icon: 'pi pi-fw pi-user-plus', to: '/estimating/staffing-plans/new' },
                {
                    label: 'Library',
                    icon: 'pi pi-fw pi-book',
                    items: [
                        { label: 'Rate Books', icon: 'pi pi-fw pi-dollar', to: '/estimating/rate-books' },
                        { label: 'Cost Book', icon: 'pi pi-fw pi-chart-bar', to: '/estimating/cost-book' },
                        { label: 'Crew Templates', icon: 'pi pi-fw pi-sitemap', to: '/estimating/crew-templates' },
                    ],
                },
                {
                    label: 'Reports',
                    icon: 'pi pi-fw pi-chart-line',
                    items: [
                        { label: 'Revenue Forecast', icon: 'pi pi-fw pi-chart-bar', to: '/estimating/analytics/revenue' },
                        { label: 'Manpower Forecast', icon: 'pi pi-fw pi-users', to: '/estimating/analytics/manpower' },
                        { label: 'Calendar', icon: 'pi pi-fw pi-calendar', to: '/estimating/calendar' },
                    ],
                },
            ],
            admin: [],
        },
    } as App,
    safetyApplication: {
        baseSlug: 'safety-application',
        name: 'Safety Application',
        description: 'Manage incidents, investigations, and safety reference data',
        icon: 'pi pi-shield',
        menu: {
            user: [
                { label: 'Dashboard', icon: 'pi pi-fw pi-th-large', to: '/safety-application/dashboard' },
                { label: 'Incident Forms', icon: 'pi pi-fw pi-file-edit', to: '/safety-application/incident-forms' },
                { label: 'Investigation Forms', icon: 'pi pi-fw pi-search', to: '/safety-application/investigation-forms' },
                { label: 'Reference Lookups', icon: 'pi pi-fw pi-book', to: '/safety-application/reference-lookups' },
                {
                    label: 'Administration',
                    emoji: '\u2699\uFE0F',
                    items: [
                        { label: 'Users', icon: 'pi pi-fw pi-users', to: '/safety-application/administration/users' },
                        { label: 'Security Roles', icon: 'pi pi-fw pi-id-card', to: '/safety-application/administration/security-roles' },
                        { label: 'Profile', icon: 'pi pi-fw pi-user', to: '/safety-application/administration/profile' },
                    ],
                },
            ],
            admin: [],
        },
    } as App,
    billingPacketRequestSystem: {
        baseSlug: 'billing-packet-request-system',
        name: 'Billing Packet Request System',
        description: 'Create and manage billing packet requests',
        icon: 'pi pi-envelope',
        menu: {
            user: [{ label: 'Dashboard', icon: 'pi pi-fw pi-chart-line', to: '/billing-packet-request-system/dashboard' }],
            admin: [],
        },
    } as App,
    strongholdBizAppsSuite: {
        baseSlug: '',
        name: 'Stronghold BizApps Suite',
        description: 'Access your apps quickly and easily',
        icon: 'pi pi-th-large',
        menu: {
            user: [{ label: 'Dashboard', icon: 'pi pi-fw pi-th-large', to: '/dashboard' }],
            admin: [],
        },
    } as App,
    projectManagementSystem: {
        baseSlug: 'project-management-system',
        name: 'Project Management System',
        description: 'Manage and track your projects efficiently',
        icon: 'pi pi-briefcase',
        menu: {
            user: [{ label: 'Dashboard', icon: 'pi pi-fw pi-th-large', to: '/project-management-system/dashboard' }],
            admin: [],
        },
    } as App,
    incidentManagement: {
        baseSlug: 'incident-management',
        name: 'Incident Management',
        description: 'Create and manage incident reports',
        icon: 'pi pi-exclamation-triangle',
        menu: {
            user: [
                { label: 'Incidents', icon: 'pi pi-fw pi-list', to: '/incident-management/incidents' },
                { label: 'New Incident', icon: 'pi pi-fw pi-plus', to: '/incident-management/incidents/new' },
                { label: 'Investigations', icon: 'pi pi-fw pi-search', to: '/incident-management/investigations' },
            ],
            admin: [],
        },
    } as App,
};
