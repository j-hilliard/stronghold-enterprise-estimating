import { computed, ref } from 'vue';
import { defineStore } from 'pinia';
import { useApiStore } from '@/stores/apiStore';
import {
    type AuthUser,
    type CompanyInfo,
    getCurrentUser,
    getStoredCompanies,
    getTempToken,
    isAuthenticated as checkAuth,
    loginStep1 as authLoginStep1,
    selectCompany as authSelectCompany,
    logout as authLogout,
} from '@/services/authService';

export const useUserStore = defineStore('user', () => {
    const user = ref<AuthUser | null>(getCurrentUser());
    const companies = ref<CompanyInfo[]>(getStoredCompanies());
    const tempToken = ref<string | null>(getTempToken());

    const apiStore = useApiStore();

    const isAuthenticated = computed(() => checkAuth());
    const isAdmin = computed(() => user.value?.roles?.includes('Administrator') ?? false);
    const isAnalytics = computed(() => user.value?.roles?.includes('Analytics') ?? false);
    const companyCode = computed(() => user.value?.companyCode ?? '');
    const displayName = computed(() => user.value?.username ?? '');

    async function loginStep1(username: string, password: string): Promise<{ username: string; companies: CompanyInfo[] }> {
        const result = await authLoginStep1(username, password);
        companies.value = result.companies;
        tempToken.value = getTempToken();
        return result;
    }

    async function selectCompany(companyCode: string): Promise<void> {
        const token = tempToken.value;
        if (!token) throw new Error('No pending login session.');
        const authUser = await authSelectCompany(token, companyCode);
        user.value = authUser;
        tempToken.value = null;
        companies.value = [];
        await apiStore.setToken();
    }

    function logoutUser(): void {
        authLogout();
        user.value = null;
        tempToken.value = null;
        companies.value = [];
        apiStore.clearToken();
    }

    function refreshFromStorage(): void {
        user.value = getCurrentUser();
    }

    return {
        user,
        companies,
        tempToken,
        isAdmin,
        isAnalytics,
        companyCode,
        displayName,
        isAuthenticated,
        loginStep1,
        selectCompany,
        logoutUser,
        refreshFromStorage,
    };
});
