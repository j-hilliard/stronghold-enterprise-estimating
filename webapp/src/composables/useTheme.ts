import { ref, readonly } from 'vue';

const STORAGE_KEY = 'stronghold-theme';
const isDark = ref(true);

function applyTheme(dark: boolean) {
    isDark.value = dark;
    document.documentElement.setAttribute('data-theme', dark ? 'dark' : 'light');
    localStorage.setItem(STORAGE_KEY, dark ? 'dark' : 'light');
}

function initTheme() {
    const saved = localStorage.getItem(STORAGE_KEY);
    applyTheme(saved !== 'light'); // default to dark
}

function toggleTheme() {
    applyTheme(!isDark.value);
}

export function useTheme() {
    return { isDark: readonly(isDark), toggleTheme, initTheme };
}
