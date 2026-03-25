import { apps } from '@/apps.ts';
import { useAppStore } from '@/stores/appStore.ts';
import { createRouter, createWebHistory } from 'vue-router';
import { billingPacketRoutes } from '@/modules/billing-packet-request-system/router';
import { strongholdBizAppsSuiteRoutes } from '@/modules/stronghold-biz-apps-suite/router';
import { projectManagementSystemRoutes } from '@/modules/project-management-system/router';
import { safetyApplicationRoutes } from '@/modules/safety-application/router';
import { incidentManagementRoutes } from '@/modules/incident-management/router';
import { estimatingRoutes } from '@/modules/estimating/router';
import { isAuthenticated } from '@/services/authService';
import NProgress from 'nprogress';
import 'nprogress/nprogress.css';

const PUBLIC_PATHS = ['/login'];

const routes = [
    // Public — no auth required
    {
        path: '/login',
        name: 'login',
        component: () => import('@/views/auth/LoginView.vue'),
        meta: { public: true, title: 'Sign In' },
    },

    // Default redirect
    {
        path: '/',
        redirect: `/${apps.estimating.baseSlug}/estimates`,
    },

    // Estimating (primary app)
    {
        path: `/${apps.estimating.baseSlug}`,
        component: () => import('@/layout/AppLayout.vue'),
        children: estimatingRoutes,
    },

    // Other modules
    {
        path: `/${apps.safetyApplication.baseSlug}`,
        component: () => import('@/layout/AppLayout.vue'),
        children: safetyApplicationRoutes,
    },
    {
        path: `/${apps.billingPacketRequestSystem.baseSlug}`,
        component: () => import('@/layout/AppLayout.vue'),
        children: billingPacketRoutes,
    },
    {
        path: `/${apps.strongholdBizAppsSuite.baseSlug}`,
        component: () => import('@/layout/AppLayout.vue'),
        children: strongholdBizAppsSuiteRoutes,
    },
    {
        path: `/${apps.projectManagementSystem.baseSlug}`,
        component: () => import('@/layout/AppLayout.vue'),
        children: projectManagementSystemRoutes,
    },
    {
        path: `/${apps.incidentManagement.baseSlug}`,
        component: () => import('@/layout/AppLayout.vue'),
        children: incidentManagementRoutes,
    },
];

const router = createRouter({
    routes,
    history: createWebHistory(),
    scrollBehavior() {
        return { left: 0, top: 0 };
    },
});

router.beforeEach((to, from, next) => {
    NProgress.start();

    // Trim trailing slash
    if (to.path !== '/' && to.path.endsWith('/')) {
        return next({ path: to.path.slice(0, -1), query: to.query, hash: to.hash });
    }

    // Allow public routes through
    const isPublic = to.meta.public === true || PUBLIC_PATHS.includes(to.path);
    if (isPublic) return next();

    // Require authentication for everything else
    if (!isAuthenticated()) {
        return next({ path: '/login', query: { redirect: to.fullPath } });
    }

    next();
});

router.afterEach(to => {
    NProgress.done();
    const appStore = useAppStore();
    const routeTitle = to.meta.title ? `${to.meta.title} - ` : '';
    const appName = appStore.currentApp.name || 'Stronghold Enterprise Estimating';
    document.title = routeTitle + appName;
});

router.onError(() => {
    NProgress.done();
});

export default router;
