export const apps = {
    estimating: {
        baseSlug: 'estimating',
        name: 'Enterprise Estimating',
        description: 'Create and manage estimates, staffing plans, rate books, and cost analysis',
        icon: 'pi pi-calculator',
        menu: {
            user: [
                {
                    label: 'Estimates',
                    icon: 'pi pi-fw pi-list',
                    items: [
                        { label: 'Quote Log', icon: 'pi pi-fw pi-list', to: '/estimating/estimates' },
                        { label: 'New Estimate', icon: 'pi pi-fw pi-plus', to: '/estimating/estimates/new' },
                    ],
                },
                {
                    label: 'Staffing Plans',
                    icon: 'pi pi-fw pi-users',
                    items: [
                        { label: 'Staffing Plan Log', icon: 'pi pi-fw pi-users', to: '/estimating/staffing-plans' },
                        { label: 'New Staffing Plan', icon: 'pi pi-fw pi-user-plus', to: '/estimating/staffing-plans/new' },
                    ],
                },
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
};
