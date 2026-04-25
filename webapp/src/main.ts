import App from '@/App.vue';
import router from '@/router/index.ts';
import { useApiStore } from '@/stores/apiStore.ts';
import { tryAutoLogin } from '@/services/authService';
import { useTheme } from '@/composables/useTheme';

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

// Restore saved theme preference before first paint.
useTheme().initTheme();

// Restore JWT token to axios headers if already logged in.
const apiStore = useApiStore();
apiStore.setToken();

// Mount immediately — bypass mode never shows the login page (isAuthenticated always true).
// tryAutoLogin runs in background to get a real token for API calls.
app.mount('#app');
tryAutoLogin().then(() => apiStore.setToken()).catch(() => {});
