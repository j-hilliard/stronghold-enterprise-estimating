<template>
    <div class="min-h-screen company-select-bg flex flex-col items-center justify-center px-4 py-12">

        <!-- Header -->
        <div class="flex flex-col items-center mb-10">
            <img src="@/assets/images/header-logo.svg" alt="Stronghold" class="select-logo mb-5" />
            <p class="text-xs font-semibold tracking-widest text-slate-400 uppercase mb-1">Stronghold Companies</p>
            <h1 class="text-3xl font-bold text-white tracking-wide">Enterprise Estimating System</h1>
            <p class="text-slate-400 mt-2 text-sm">Select your company to continue</p>
        </div>

        <!-- Error -->
        <div v-if="errorMessage" class="mb-6 w-full max-w-3xl">
            <Message severity="error" :closable="false">{{ errorMessage }}</Message>
        </div>

        <!-- Company Cards Grid (non-GLOBAL) -->
        <div class="company-grid w-full max-w-3xl">
            <div
                v-for="company in regularCompanies"
                :key="company.code"
                class="company-card"
                :class="{ 'last-used': lastUsed === company.code }"
                @click="handleSelect(company.code)"
                role="button"
                :tabindex="selecting ? -1 : 0"
                @keydown.enter="handleSelect(company.code)"
            >
                <div v-if="lastUsed === company.code" class="last-used-badge">
                    <i class="pi pi-check-circle" /> Last Used
                </div>

                <div class="company-initials" :style="{ background: companyColor(company.code) }">
                    {{ company.shortName.slice(0, 2).toUpperCase() }}
                </div>

                <div class="company-info">
                    <div class="company-short-name">{{ company.shortName }}</div>
                    <div class="company-full-name">{{ company.name }}</div>
                    <div v-if="company.jobLetter" class="job-letter-badge">
                        Job Prefix: {{ company.jobLetter }}
                    </div>
                </div>

                <div v-if="selecting === company.code" class="selecting-spinner">
                    <i class="pi pi-spin pi-spinner" />
                </div>
                <i v-else class="pi pi-arrow-right select-arrow" />
            </div>
        </div>

        <!-- Global Analytics Card (only for Admins/Analytics) -->
        <div
            v-if="globalCompany"
            class="global-card w-full max-w-3xl mt-4"
            :class="{ 'last-used': lastUsed === 'GLOBAL' }"
            @click="handleSelect('GLOBAL')"
            role="button"
            :tabindex="selecting ? -1 : 0"
            @keydown.enter="handleSelect('GLOBAL')"
        >
            <div v-if="lastUsed === 'GLOBAL'" class="last-used-badge light">
                <i class="pi pi-check-circle" /> Last Used
            </div>

            <div class="global-icon">
                <i class="pi pi-globe" />
            </div>

            <div class="global-info">
                <div class="global-title">Global Analytics</div>
                <div class="global-subtitle">Cross-company revenue forecast &amp; pipeline intelligence</div>
            </div>

            <div v-if="selecting === 'GLOBAL'" class="selecting-spinner light">
                <i class="pi pi-spin pi-spinner" />
            </div>
            <i v-else class="pi pi-arrow-right global-arrow" />
        </div>

        <p class="text-center text-xs text-slate-600 mt-10">
            Signed in as <span class="text-slate-400">{{ username }}</span>
            &nbsp;&middot;&nbsp;
            <button class="text-slate-500 hover:text-slate-300 underline" @click="handleLogout">Sign out</button>
        </p>
    </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import Message from 'primevue/message';
import { useUserStore } from '@/stores/userStore';
import { isAuthenticated, hasPendingCompanySelection, logout, getStoredCompanies } from '@/services/authService';
import type { CompanyInfo } from '@/services/authService';

const router = useRouter();
const userStore = useUserStore();

const companies = ref<CompanyInfo[]>([]);
const username = ref('');
const selecting = ref<string | null>(null);
const errorMessage = ref('');
const lastUsed = ref(localStorage.getItem('ste_last_company') ?? '');

onMounted(() => {
    // Already fully logged in — go straight to app
    if (isAuthenticated()) {
        router.replace('/estimating/estimates');
        return;
    }

    // Need a valid temp token to be here
    if (!hasPendingCompanySelection()) {
        router.replace('/login');
        return;
    }

    const stored = userStore.companies.length > 0 ? userStore.companies : getStoredCompanies();
    companies.value = stored;
    username.value = userStore.user?.username ?? '';
});

const regularCompanies = computed(() => companies.value.filter(c => c.code !== 'GLOBAL'));
const globalCompany = computed(() => companies.value.find(c => c.code === 'GLOBAL') ?? null);

const COMPANY_COLORS: Record<string, string> = {
    CSL: '#1e6fb5',
    ETS: '#2e7d4f',
    STS: '#7b3fa0',
    STG: '#b55a1e',
};

function companyColor(code: string): string {
    return COMPANY_COLORS[code] ?? '#374151';
}

async function handleSelect(code: string) {
    if (selecting.value) return;
    selecting.value = code;
    errorMessage.value = '';
    try {
        await userStore.selectCompany(code);
        await router.push('/estimating/estimates');
    } catch {
        errorMessage.value = 'Could not select company. Your session may have expired — please sign in again.';
        selecting.value = null;
    }
}

function handleLogout() {
    logout();
    router.push('/login');
}
</script>

<style scoped>
.company-select-bg {
    background: var(--shell-bg, #0f1729);
}

.select-logo {
    height: 52px;
    width: auto;
}

/* Grid */
.company-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
    gap: 1rem;
}

/* Regular company card */
.company-card {
    position: relative;
    display: flex;
    align-items: center;
    gap: 1rem;
    padding: 1.25rem 1.5rem;
    background: #1e293b;
    border: 1px solid #334155;
    border-radius: 12px;
    cursor: pointer;
    transition: border-color 0.15s, box-shadow 0.15s, transform 0.1s;
    overflow: hidden;
}

.company-card:hover {
    border-color: #60a5fa;
    box-shadow: 0 0 0 1px #60a5fa33, 0 4px 20px #0005;
    transform: translateY(-1px);
}

.company-card.last-used {
    border-color: #22c55e88;
}

.company-initials {
    flex-shrink: 0;
    width: 52px;
    height: 52px;
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.1rem;
    font-weight: 700;
    color: #fff;
    letter-spacing: 0.05em;
}

.company-info {
    flex: 1;
    min-width: 0;
}

.company-short-name {
    font-size: 1rem;
    font-weight: 700;
    color: #f1f5f9;
    line-height: 1.2;
}

.company-full-name {
    font-size: 0.8rem;
    color: #94a3b8;
    margin-top: 2px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.job-letter-badge {
    display: inline-block;
    margin-top: 6px;
    font-size: 0.7rem;
    color: #64748b;
    background: #0f172a;
    border: 1px solid #334155;
    border-radius: 4px;
    padding: 1px 6px;
}

.select-arrow {
    color: #475569;
    font-size: 0.9rem;
    transition: color 0.15s, transform 0.15s;
}

.company-card:hover .select-arrow {
    color: #60a5fa;
    transform: translateX(3px);
}

/* Last used badge */
.last-used-badge {
    position: absolute;
    top: 8px;
    right: 10px;
    font-size: 0.65rem;
    color: #22c55e;
    display: flex;
    align-items: center;
    gap: 3px;
}

.last-used-badge.light {
    color: #86efac;
}

/* Global analytics card */
.global-card {
    position: relative;
    display: flex;
    align-items: center;
    gap: 1.25rem;
    padding: 1.5rem 2rem;
    background: linear-gradient(135deg, #1e3a5f 0%, #1a2d4a 60%, #0f1e35 100%);
    border: 1px solid #2d5a9e66;
    border-radius: 12px;
    cursor: pointer;
    transition: border-color 0.15s, box-shadow 0.15s, transform 0.1s;
    overflow: hidden;
}

.global-card:hover {
    border-color: #3b82f6aa;
    box-shadow: 0 0 0 1px #3b82f633, 0 6px 24px #0006;
    transform: translateY(-1px);
}

.global-card.last-used {
    border-color: #22c55e88;
}

.global-icon {
    flex-shrink: 0;
    width: 56px;
    height: 56px;
    border-radius: 12px;
    background: linear-gradient(135deg, #2563eb, #1d4ed8);
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.5rem;
    color: #fff;
}

.global-info {
    flex: 1;
}

.global-title {
    font-size: 1.1rem;
    font-weight: 700;
    color: #e0f2fe;
}

.global-subtitle {
    font-size: 0.8rem;
    color: #7dd3fc;
    margin-top: 3px;
}

.global-arrow {
    color: #3b82f6;
    font-size: 1rem;
    transition: color 0.15s, transform 0.15s;
}

.global-card:hover .global-arrow {
    color: #93c5fd;
    transform: translateX(3px);
}

/* Spinner */
.selecting-spinner {
    color: #60a5fa;
    font-size: 1rem;
}

.selecting-spinner.light {
    color: #93c5fd;
}
</style>
