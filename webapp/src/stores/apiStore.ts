import { ref } from 'vue';
import axios from 'axios';
import router from '@/router';
import { defineStore } from 'pinia';
import { getToken, logout } from '@/services/authService';
import { useAppStore } from '@/stores/appStore.ts';

export const useApiStore = defineStore('api', () => {
    const api = ref(axios.create({ baseURL: import.meta.env.VITE_APP_API_BASE_URL }));

    const appStore = useAppStore();

    function setApiAuthorizationHeader(token: string) {
        if (token) {
            api.value.defaults.headers.common.Authorization = `Bearer ${token}`;
            return;
        }
        delete api.value.defaults.headers.common.Authorization;
    }

    function clearToken() {
        delete api.value.defaults.headers.common.Authorization;
    }

    function setToken() {
        const token = getToken() ?? '';
        setApiAuthorizationHeader(token);
        setApiResponseInterceptor();
    }

    function setApiResponseInterceptor() {
        api.value.interceptors.response.use(
            response => response,
            async error => {
                if (!error.response) return Promise.reject(error);

                const { currentApp } = appStore;
                const { status } = error.response;
                const baseSlug = currentApp?.baseSlug ? `/${currentApp.baseSlug}` : '';

                if (status === 401) {
                    logout();
                    await router.push('/login');
                }

                if (status === 403) {
                    await router.push(`${baseSlug}/forbidden`);
                }

                if (status >= 500 && status < 600) {
                    await router.push(`${baseSlug}/internal-server-error`);
                }

                return Promise.reject(error);
            }
        );
    }

    return { api, setToken, clearToken };
});
