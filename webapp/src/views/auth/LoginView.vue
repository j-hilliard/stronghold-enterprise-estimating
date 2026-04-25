<template>
    <div class="min-h-screen flex items-center justify-center login-bg">
        <div class="login-card w-full max-w-md px-4">
            <!-- Logo / Header -->
            <div class="flex flex-col items-center mb-8">
                <img src="@/assets/images/header-logo.svg" alt="Stronghold" class="login-logo mb-4" />
                <h1 class="text-2xl font-bold text-white tracking-wide">Enterprise Estimating</h1>
                <p class="text-sm text-slate-400 mt-1">Sign in to continue</p>
            </div>

            <!-- Login Card -->
            <Card class="app-shell-card">
                <template #content>
                    <form class="flex flex-col gap-4" @submit.prevent="handleLogin">
                        <BaseFormField label="Username">
                            <InputText
                                v-model="form.username"
                                placeholder="Enter your username"
                                class="w-full"
                                autocomplete="username"
                                :disabled="loading"
                            />
                        </BaseFormField>

                        <BaseFormField label="Password">
                            <Password
                                v-model="form.password"
                                placeholder="Enter your password"
                                class="w-full"
                                inputClass="w-full"
                                :feedback="false"
                                :toggleMask="true"
                                autocomplete="current-password"
                                :disabled="loading"
                            />
                        </BaseFormField>

                        <Message v-if="errorMessage" severity="error" :closable="false" class="mt-1">
                            {{ errorMessage }}
                        </Message>

                        <Button
                            type="submit"
                            label="Sign In"
                            icon="pi pi-sign-in"
                            class="w-full mt-2"
                            :loading="loading"
                            :disabled="!canSubmit"
                        />
                    </form>
                </template>
            </Card>

            <p class="text-center text-xs text-slate-500 mt-6">
                Stronghold Enterprise Estimating &copy; {{ year }}
            </p>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import Card from 'primevue/card';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Password from 'primevue/password';
import Message from 'primevue/message';
import BaseFormField from '@/components/forms/BaseFormField.vue';
import { useUserStore } from '@/stores/userStore';

const router = useRouter();
const userStore = useUserStore();

const year = new Date().getFullYear();

const form = reactive({
    username: '',
    password: '',
});

const loading = ref(false);
const errorMessage = ref('');

const canSubmit = computed(() => form.username.trim() && form.password);

async function handleLogin() {
    if (!canSubmit.value) return;

    loading.value = true;
    errorMessage.value = '';

    try {
        await userStore.loginStep1(form.username.trim(), form.password);
        await router.push('/company-select');
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
        errorMessage.value = msg || 'Invalid username or password.';
    } finally {
        loading.value = false;
    }
}
</script>

<style scoped>
.login-bg {
    background: var(--shell-bg, #0f1729);
}

.login-card {
    animation: fadeInUp 0.3s ease;
}

.login-logo {
    height: 48px;
    width: auto;
}

@keyframes fadeInUp {
    from {
        opacity: 0;
        transform: translateY(16px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}
</style>
