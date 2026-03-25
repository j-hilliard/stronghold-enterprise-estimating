export const apps = {
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
