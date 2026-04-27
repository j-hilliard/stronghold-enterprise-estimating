import { ref } from 'vue';
import axios from 'axios';
import router from '@/router';
import { defineStore } from 'pinia';
import { DEV_BYPASS, getToken, logout, tryAutoLogin } from '@/services/authService';
import { useAppStore } from '@/stores/appStore.ts';

export const useApiStore = defineStore('api', () => {
    const api = ref(axios.create({ baseURL: import.meta.env.VITE_APP_API_BASE_URL, timeout: 30000 }));

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
        setApiRequestInterceptor();
        setApiResponseInterceptor();
    }

    let requestInterceptorId: number | null = null;

    function setApiRequestInterceptor() {
        if (requestInterceptorId !== null) {
            api.value.interceptors.request.eject(requestInterceptorId);
        }
        requestInterceptorId = api.value.interceptors.request.use(config => {
            if (import.meta.env.DEV) {
                const override = localStorage.getItem('ste_dev_company');
                if (override) {
                    config.headers['X-Company-Override'] = override;
                }
            }
            return config;
        });
    }

    let interceptorId: number | null = null;

    function setApiResponseInterceptor() {
        if (interceptorId !== null) {
            api.value.interceptors.response.eject(interceptorId);
        }
        interceptorId = api.value.interceptors.response.use(
            response => response,
            async error => {
                if (!error.response) return Promise.reject(error);

                const { currentApp } = appStore;
                const { status } = error.response;
                const baseSlug = currentApp?.baseSlug ? `/${currentApp.baseSlug}` : '';

                if (status === 401) {
                    if (DEV_BYPASS && !error.config._retry) {
                        error.config._retry = true;
                        try {
                            localStorage.removeItem('ste_auth_token');
                            await tryAutoLogin();
                            const token = getToken();
                            if (token && token !== 'bypass-pending') {
                                setApiAuthorizationHeader(token);
                                error.config.headers['Authorization'] = `Bearer ${token}`;
                                return api.value.request(error.config);
                            }
                        } catch { /* fall through */ }
                    }
                    logout();
                    await router.push('/login');
                }

                if (status === 403) {
                    await router.push(`${baseSlug}/forbidden`);
                }

                if (status >= 500 && status < 600 && !error.config?.url?.includes('/ai/')) {
                    await router.push(`${baseSlug}/internal-server-error`);
                }

                return Promise.reject(error);
            }
        );
    }

    return { api, setToken, clearToken };
});
