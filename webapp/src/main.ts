import App from '@/App.vue';
import router from '@/router/index.ts';
import { useApiStore } from '@/stores/apiStore.ts';
import { useUserStore } from '@/stores/userStore.ts';
import {
    handleRedirectResponse,
    isLocalAuthEnabled,
    localAccount,
    login,
    msalInstance,
} from '@/services/msalService.ts';

import Toast from 'primevue/toast';
import Ripple from 'primevue/ripple';
import PrimeVue from 'primevue/config';
import Tooltip from 'primevue/tooltip';
import InputMask from 'primevue/inputmask';
import ProgressBar from 'primevue/progressbar';
import ToastService from 'primevue/toastservice';
import ConfirmationService from 'primevue/confirmationservice';

import 'primeicons/primeicons.css';
import 'primevue/resources/primevue.min.css';
import 'primevue/resources/themes/lara-dark-blue/theme.css';
import '@/assets/css/theme.css';
import '@/assets/css/style.css';

import { createPinia } from 'pinia';
import { createApp, reactive } from 'vue';

const app = createApp(App);
const pinia = createPinia();

app.use(pinia);
app.use(router);
app.use(ToastService);
app.use(ConfirmationService);
app.use(PrimeVue, { ripple: true });

app.directive('ripple', Ripple);
app.directive('tooltip', Tooltip);

app.component('PrimeVueToast', Toast);
app.component('PrimeVueInputMask', InputMask);
app.component('PrimeVueInputProgressBar', ProgressBar);

app.config.globalProperties.$appState = reactive({
    isRTL: false,
    layoutMode: 'dark',
    isNewThemeLoaded: false,
});

const initializeAuth = async () => {
    if (isLocalAuthEnabled) {
        return localAccount;
    }

    await msalInstance.initialize();
    return handleRedirectResponse();
};

const bypassAuth = import.meta.env.VITE_BYPASS_AUTH === 'true';

if (bypassAuth) {
    const userStore = useUserStore();
    userStore.setMockUser();
    app.mount('#app');
} else {
    initializeAuth()
        .then(async account => {
            const apiStore = useApiStore();
            const userStore = useUserStore();

            await apiStore.setToken(account);
            await userStore.setUser(account);

            if (!userStore.isAuthenticated && !isLocalAuthEnabled) {
                await login();
            }

            return userStore.isAuthenticated;
        })
        .then(isAuthenticated => {
            if (isAuthenticated) {
                app.mount('#app');
            }
        })
        .catch(error => console.error(error));
}
