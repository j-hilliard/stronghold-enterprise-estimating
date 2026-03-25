import { computed, ref } from 'vue';
import { defineStore } from 'pinia';
import { useApiStore } from '@/stores/apiStore';
import {
    type AuthUser,
    getCurrentUser,
    isAuthenticated as checkAuth,
    login as authLogin,
    logout as authLogout,
} from '@/services/authService';

export const useUserStore = defineStore('user', () => {
    const user = ref<AuthUser | null>(getCurrentUser());

    const apiStore = useApiStore();

    const isAuthenticated = computed(() => checkAuth());

    const isAdmin = computed(() => user.value?.roles?.includes('Administrator') ?? false);

    const companyCode = computed(() => user.value?.companyCode ?? '');

    const displayName = computed(() => user.value?.username ?? '');

    async function login(username: string, password: string, companyCode: string): Promise<void> {
        const authUser = await authLogin(username, password, companyCode);
        user.value = authUser;
        await apiStore.setToken();
    }

    function logoutUser(): void {
        authLogout();
        user.value = null;
        apiStore.clearToken();
    }

    function refreshFromStorage(): void {
        user.value = getCurrentUser();
    }

    return {
        user,
        isAdmin,
        companyCode,
        displayName,
        isAuthenticated,
        login,
        logoutUser,
        refreshFromStorage,
    };
});
