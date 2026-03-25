<template>
    <div class="space-y-6">
        <BasePageHeader
            title="Profile"
            :subtitle="pageSubtitle"
            icon="pi pi-user"
        />

        <Card class="border border-slate-700/70 bg-slate-800/70 shadow-lg shadow-slate-950/20">
            <template #content>
                <div class="space-y-5 p-6">
                    <div class="rounded-2xl border border-slate-700/70 bg-slate-900/50 p-4 text-sm text-slate-300">
                        These saved defaults are used when the dashboard loads and when the user drills into the Incident Register from the Total Incidents KPI.
                    </div>

                    <div v-if="targetUserSummary" class="rounded-2xl border border-blue-500/20 bg-blue-500/10 p-4">
                        <p class="m-0 text-xs font-semibold uppercase tracking-[0.24em] text-blue-200">Managing Profile</p>
                        <p class="mt-2 text-base font-semibold text-white">{{ targetUserSummary.name }}</p>
                        <p class="m-0 text-sm text-blue-100">{{ targetUserSummary.email }}</p>
                    </div>

                    <div v-if="isLoading" class="rounded-2xl border border-slate-700/70 bg-slate-900/40 px-4 py-8 text-center text-sm text-slate-400">
                        Loading profile defaults...
                    </div>

                    <div v-else class="grid gap-4 md:grid-cols-2">
                        <BaseFormField label="Default Date Range" :errors="validationErrors.defaultDateRange">
                            <Dropdown
                                v-model="form.defaultDateRange"
                                :options="profileDateRangeOptions"
                                optionLabel="label"
                                optionValue="value"
                                class="w-full"
                                placeholder="No default date range"
                                showClear
                            />
                        </BaseFormField>

                        <BaseFormField label="Default Company" :errors="validationErrors.defaultCompany">
                            <Dropdown
                                v-model="form.defaultCompany"
                                :options="companyOptions"
                                optionLabel="label"
                                optionValue="value"
                                class="w-full"
                                placeholder="No default company"
                                filter
                                showClear
                            />
                        </BaseFormField>

                        <BaseFormField label="Default Customer" :errors="validationErrors.defaultCustomer">
                            <Dropdown
                                v-model="form.defaultCustomer"
                                :options="customerOptions"
                                optionLabel="label"
                                optionValue="value"
                                class="w-full"
                                placeholder="No default customer"
                                filter
                                showClear
                            />
                        </BaseFormField>

                        <BaseFormField label="Default Incident Status" :errors="validationErrors.defaultIncidentStatus">
                            <MultiSelect
                                v-model="form.defaultIncidentStatuses"
                                :options="statusOptions"
                                optionLabel="label"
                                optionValue="value"
                                class="w-full"
                                placeholder="No default statuses"
                                display="chip"
                                filter
                                showClear
                            />
                        </BaseFormField>
                    </div>

                    <div class="flex items-center justify-end gap-3 border-t border-slate-700/70 pt-4">
                        <Button label="Cancel" text :disabled="!hasChanges || isSaving" @click="resetForm" />
                        <Button label="Save" :loading="isSaving" @click="saveProfileChanges" />
                    </div>
                </div>
            </template>
        </Card>
    </div>
</template>

<script setup lang="ts">
import BaseFormField from '@/components/forms/BaseFormField.vue';
import BasePageHeader from '@/components/layout/BasePageHeader.vue';
import { UserClient } from '@/apiclient/client';
import {
    fetchProfileFilterOptions,
    fetchMyProfile,
    fetchUserProfile,
    profileDateRangeOptions,
    saveMyProfile,
    saveUserProfile,
    type ProfileFilterOptions,
    type UserProfileSettings,
} from '@/modules/safety-application/features/dashboard/services/userProfileService';
import { useApiStore } from '@/stores/apiStore';
import { useUserStore } from '@/stores/userStore';
import axios from 'axios';
import Button from 'primevue/button';
import Card from 'primevue/card';
import Dropdown from 'primevue/dropdown';
import MultiSelect from 'primevue/multiselect';
import { useToast } from 'primevue/usetoast';
import { computed, onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';

type ProfileForm = {
    userId: number;
    defaultDateRange?: UserProfileSettings['defaultDateRange'];
    defaultCompany?: string;
    defaultCustomer?: string;
    defaultIncidentStatuses: string[];
};

const apiStore = useApiStore();
const userStore = useUserStore();
const route = useRoute();
const toast = useToast();

const isLoading = ref(true);
const isSaving = ref(false);
const initialProfile = ref<ProfileForm>(createEmptyForm());
const form = ref<ProfileForm>(createEmptyForm());
const companyOptions = ref<{ label: string; value: string }[]>([]);
const customerOptions = ref<{ label: string; value: string }[]>([]);
const statusOptions = ref<{ label: string; value: string }[]>([]);
const targetUserSummary = ref<{ name: string; email: string } | null>(null);

const targetUserId = computed(() => {
    if (!userStore.isAdmin) {
        return null;
    }

    const parsed = Number(route.query.userId);
    return Number.isInteger(parsed) && parsed > 0 ? parsed : null;
});

const pageSubtitle = computed(() =>
    targetUserSummary.value
        ? 'Manage the default dashboard and incident filters for the selected user.'
        : 'Manage your default dashboard and incident filter settings.',
);

const hasChanges = computed(() => JSON.stringify(form.value) !== JSON.stringify(initialProfile.value));

const validationErrors = computed(() => ({
    defaultDateRange:
        form.value.defaultDateRange && !profileDateRangeOptions.some(option => option.value === form.value.defaultDateRange)
            ? ['Default Date Range is invalid.']
            : [],
    defaultCompany:
        form.value.defaultCompany && !companyOptions.value.some(option => option.value === form.value.defaultCompany)
            ? ['Default Company is invalid.']
            : [],
    defaultCustomer:
        form.value.defaultCustomer && !customerOptions.value.some(option => option.value === form.value.defaultCustomer)
            ? ['Default Customer is invalid.']
            : [],
    defaultIncidentStatus:
        form.value.defaultIncidentStatuses.some(status => !statusOptions.value.some(option => option.value === status))
            ? ['Default Incident Status is invalid.']
            : [],
}));

onMounted(async () => {
    await loadProfilePage();
});

async function loadProfilePage() {
    isLoading.value = true;

    try {
        const [profile, filterOptions] = await Promise.all([
            targetUserId.value ? fetchUserProfile(apiStore.api, targetUserId.value) : fetchMyProfile(apiStore.api),
            fetchProfileFilterOptions(apiStore.api),
        ]);

        if (targetUserId.value) {
            const userClient = new UserClient(apiStore.api.defaults.baseURL, apiStore.api);
            const targetUser = await userClient.getUserByUserId(targetUserId.value);
            targetUserSummary.value = {
                name: `${targetUser.firstName || ''} ${targetUser.lastName || ''}`.trim() || targetUser.email || `User ${targetUser.userId}`,
                email: targetUser.email || 'No email available',
            };
        } else {
            targetUserSummary.value = null;
        }

        applyFilterOptions(filterOptions, profile);

        initialProfile.value = mapProfileToForm(profile);
        form.value = mapProfileToForm(profile);
    } catch (error) {
        toast.add({
            severity: 'error',
            summary: 'Profile Unavailable',
            detail: 'The user profile defaults could not be loaded.',
            life: 4000,
        });
        console.error(error);
    } finally {
        isLoading.value = false;
    }
}

function resetForm() {
    form.value = {
        ...initialProfile.value,
        defaultIncidentStatuses: [...initialProfile.value.defaultIncidentStatuses],
    };
}

async function saveProfileChanges() {
    if (
        validationErrors.value.defaultDateRange.length
        || validationErrors.value.defaultCompany.length
        || validationErrors.value.defaultCustomer.length
        || validationErrors.value.defaultIncidentStatus.length
    ) {
        return;
    }

    isSaving.value = true;

    try {
        const payload: UserProfileSettings = {
            profileId: 0,
            userId: form.value.userId,
            defaultDateRange: form.value.defaultDateRange,
            defaultCompany: form.value.defaultCompany,
            defaultCustomer: form.value.defaultCustomer,
            defaultIncidentStatuses: [...form.value.defaultIncidentStatuses],
        };

        const savedProfile = targetUserId.value
            ? await saveUserProfile(apiStore.api, targetUserId.value, payload)
            : await saveMyProfile(apiStore.api, payload);

        initialProfile.value = mapProfileToForm(savedProfile);
        form.value = mapProfileToForm(savedProfile);

        toast.add({
            severity: 'success',
            summary: 'Profile Saved',
            detail: 'Default dashboard and incident filters were updated.',
            life: 3000,
        });
    } catch (error) {
        const errorDetail = extractErrorDetail(error);
        toast.add({
            severity: 'error',
            summary: 'Save Failed',
            detail: errorDetail,
            life: 5000,
        });
        console.error(error);
    } finally {
        isSaving.value = false;
    }
}

function mapProfileToForm(profile: UserProfileSettings): ProfileForm {
    return {
        userId: profile.userId || targetUserId.value || userStore.user?.userId || 0,
        defaultDateRange: profile.defaultDateRange,
        defaultCompany: profile.defaultCompany,
        defaultCustomer: profile.defaultCustomer,
        defaultIncidentStatuses: [...(profile.defaultIncidentStatuses || [])],
    };
}

function createEmptyForm(): ProfileForm {
    return {
        userId: 0,
        defaultDateRange: undefined,
        defaultCompany: undefined,
        defaultCustomer: undefined,
        defaultIncidentStatuses: [],
    };
}

function applyFilterOptions(filterOptions: ProfileFilterOptions, profile: UserProfileSettings) {
    companyOptions.value = includeSavedValue(
        withAllOption(filterOptions.companies || [], 'All Companies'),
        profile.defaultCompany,
    );
    customerOptions.value = includeSavedValue(
        withAllOption(filterOptions.customers || [], 'All Customers'),
        profile.defaultCustomer,
    );
    statusOptions.value = includeSavedValue(
        (filterOptions.statuses || []).map(option => ({
            label: option.label === 'Investigation' ? 'Active Investigation' : option.label,
            value: option.value,
        })),
        ...(profile.defaultIncidentStatuses || []),
    );
}

function withAllOption(options: { label: string; value: string }[], label: string) {
    return [{ label, value: '' }, ...options];
}

function includeSavedValue(
    options: { label: string; value: string }[],
    ...savedValues: Array<string | undefined>
) {
    const missingValues = savedValues
        .filter((value): value is string => !!value)
        .filter(value => !options.some(option => option.value === value))
        .map(value => ({ label: value, value }));

    return missingValues.length ? [...options, ...missingValues] : options;
}

function extractErrorDetail(error: unknown) {
    if (axios.isAxiosError(error)) {
        const responseData = error.response?.data;
        if (typeof responseData === 'string' && responseData.trim()) {
            return responseData;
        }

        if (responseData && typeof responseData === 'object') {
            const detail = 'detail' in responseData ? responseData.detail : null;
            if (typeof detail === 'string' && detail.trim()) {
                return detail;
            }

            const title = 'title' in responseData ? responseData.title : null;
            if (typeof title === 'string' && title.trim()) {
                return title;
            }

            const errors = 'errors' in responseData ? responseData.errors : null;
            if (errors && typeof errors === 'object') {
                const firstError = Object.values(errors)
                    .flatMap(value => Array.isArray(value) ? value : [])
                    .find(value => typeof value === 'string' && value.trim());

                if (typeof firstError === 'string' && firstError.trim()) {
                    return firstError;
                }
            }
        }
    }

    if (error instanceof Error && error.message.trim()) {
        return error.message;
    }

    return 'The profile defaults could not be saved.';
}
</script>
