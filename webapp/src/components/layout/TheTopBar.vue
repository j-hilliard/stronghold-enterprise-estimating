<template>
    <div class="layout-topbar app-shell-topbar">
        <div class="layout-topbar-wrapper">
            <div class="layout-topbar-left">
                <router-link to="/">
                    <div class="layout-topbar-logo">
                        <img id="app-logo" :src="logo" alt="poseidon-layout" />
                    </div>
                </router-link>
            </div>
            <div class="layout-topbar-right">
                <a href="#" @click="$emit('menu-button-click', $event)" class="menu-button app-shell-menu-button">
                    <i class="pi pi-bars" />
                </a>
                <Breadcrumb :model="breadcrumbItems" class="ml-4" />
                <ul class="layout-topbar-actions">
                    <!-- Dev company switcher (DEV mode only) -->
                    <li class="topbar-item">
                        <DevCompanySwitcher />
                    </li>
                    <!-- Theme toggle -->
                    <li class="topbar-item">
                        <button
                            class="theme-toggle-btn"
                            :title="isDark ? 'Switch to light mode' : 'Switch to dark mode'"
                            @click="toggleTheme"
                        >
                            <!-- Sun icon (shown in dark mode to switch to light) -->
                            <svg v-if="isDark" xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <circle cx="12" cy="12" r="4"/>
                                <path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41"/>
                            </svg>
                            <!-- Moon icon (shown in light mode to switch to dark) -->
                            <svg v-else xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/>
                            </svg>
                        </button>
                    </li>
                    <li class="topbar-item user-profile"
                        :class="{ 'active-topmenuitem': activeTopbarItem === 'profile' }">
                        <a
                            href="#"
                            class="text-white flex items-center justify-center"
                            @click="onTopbarItemClick($event, 'profile')"
                        >
                            <div class="w-10 h-10 rounded-full app-shell-avatar flex items-center justify-center">
                                <i class="pi pi-user text-slate-300" />
                            </div>
                            <div class="profile-info app-shell-topbar-text">
                                <h6>{{ userStore.displayName }}</h6>
                                <span>{{ userStore.companyCode }}</span>
                            </div>
                        </a>
                        <ul class="fadeInDown">
                            <li class="layout-submenu-header">
                                <div class="w-10 h-10 rounded-full app-shell-avatar flex items-center justify-center">
                                    <i class="pi pi-user text-slate-300" />
                                </div>
                                <div class="profile-info">
                                    <h6>{{ userStore.displayName }}</h6>
                                    <span>{{ userStore.companyCode }}</span>
                                </div>
                            </li>
                            <li role="menuitem">
                                <a href="#" @click="userStore.logoutUser">
                                    <i class="pi pi-power-off" />
                                    <h6>Logout</h6>
                                </a>
                            </li>
                        </ul>
                    </li>
                </ul>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import logo from '@/assets/images/header-logo.svg';
import { useUserStore } from '@/stores/userStore.ts';
import { useTheme } from '@/composables/useTheme';
import DevCompanySwitcher from '@/components/layout/DevCompanySwitcher.vue';

defineProps<{ activeTopbarItem: string; }>();

const emit = defineEmits([
    'menu-button-click',
    'topbar-item-click',
]);

const route = useRoute();
const userStore = useUserStore();
const { isDark, toggleTheme } = useTheme();

const breadcrumbItems = computed(() => {
    const routeBreadcrumbItems = route.meta?.breadcrumbItems;

    if (routeBreadcrumbItems instanceof Function) {
        return routeBreadcrumbItems() || [];
    }

    return [];
});

function onTopbarItemClick(event: Event, item: string) {
    emit('topbar-item-click', { originalEvent: event, item });
    event.preventDefault();
}
</script>

<style scoped>
.round-image {
    width: 100px;
    height: 100px;
    border-radius: 50%;
}

.theme-toggle-btn {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 36px;
    height: 36px;
    border-radius: 8px;
    background: rgba(255, 255, 255, 0.08);
    border: 1px solid rgba(255, 255, 255, 0.14);
    color: rgba(255, 255, 255, 0.8);
    cursor: pointer;
    transition: background 0.2s, color 0.2s, border-color 0.2s;
    margin-right: 8px;
}

.theme-toggle-btn:hover {
    background: rgba(255, 255, 255, 0.16);
    color: #ffffff;
    border-color: rgba(255, 255, 255, 0.28);
}
</style>
