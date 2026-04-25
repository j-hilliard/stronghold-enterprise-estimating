export const crewTemplateRoutes = [
    {
        path: 'crew-templates',
        name: 'estimating-crew-templates',
        component: () => import('@/modules/estimating/features/crew-templates/views/CrewTemplatesView.vue'),
        meta: { title: 'Crew Templates' },
    },
];
