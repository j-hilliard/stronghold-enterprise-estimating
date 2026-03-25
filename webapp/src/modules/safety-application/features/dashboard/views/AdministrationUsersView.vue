<template>
    <div class="space-y-6">
        <BasePageHeader
            title="Users"
            subtitle="Manage application users"
            icon="pi pi-users"
        >
            <Button label="Add User" icon="pi pi-plus" @click="openCreateDialog" />
        </BasePageHeader>

        <Card class="border border-slate-700/70 bg-slate-800/70 shadow-lg shadow-slate-950/20">
            <template #content>
                <div class="grid gap-4 p-6 md:grid-cols-3">
                    <div class="rounded-2xl border border-slate-700/70 bg-slate-900/50 p-4">
                        <p class="mb-2 text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">Total Users</p>
                        <p class="m-0 text-3xl font-semibold text-white">{{ totalRecords }}</p>
                    </div>
                    <div class="rounded-2xl border border-emerald-500/20 bg-emerald-500/10 p-4">
                        <p class="mb-2 text-xs font-semibold uppercase tracking-[0.24em] text-emerald-200">Active</p>
                        <p class="m-0 text-3xl font-semibold text-white">{{ activeUserCount }}</p>
                    </div>
                    <div class="rounded-2xl border border-amber-500/20 bg-amber-500/10 p-4">
                        <p class="mb-2 text-xs font-semibold uppercase tracking-[0.24em] text-amber-200">Disabled</p>
                        <p class="m-0 text-3xl font-semibold text-white">{{ disabledUserCount }}</p>
                    </div>
                </div>
            </template>
        </Card>

        <BasePageSubheader title="User Management" />

        <Card class="border border-slate-700/70 bg-slate-800/70 shadow-lg shadow-slate-950/20">
            <template #content>
                <div class="space-y-4 p-6">
                    <div class="space-y-4 rounded-2xl border border-slate-700/70 bg-slate-900/50 px-4 py-3">
                        <div class="grid gap-3 md:grid-cols-3">
                            <span class="flex flex-col gap-2">
                                <label class="text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Name</label>
                                <InputText
                                    v-model="nameFilter"
                                    class="w-full"
                                    placeholder="Search by user name"
                                />
                            </span>
                            <span class="flex flex-col gap-2">
                                <label class="text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Company</label>
                                <Dropdown
                                    v-model="companyFilter"
                                    :options="filterCompanyOptions"
                                    optionLabel="label"
                                    optionValue="value"
                                    class="w-full"
                                    placeholder="All Companies"
                                    showClear
                                />
                            </span>
                            <span class="flex flex-col gap-2">
                                <label class="text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Status</label>
                                <Dropdown
                                    v-model="statusFilter"
                                    :options="statusFilterOptions"
                                    optionLabel="label"
                                    optionValue="value"
                                    class="w-full"
                                    placeholder="Select status"
                                />
                            </span>
                        </div>

                        <div class="flex flex-wrap items-center justify-between gap-3">
                            <div class="space-y-1">
                                <p class="m-0 text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">User Records</p>
                                <p class="m-0 text-sm text-slate-300">
                                    Showing {{ currentPageRows.length }} user{{ currentPageRows.length === 1 ? '' : 's' }} on this page.
                                </p>
                            </div>
                            <Button
                                v-if="showClearFilters"
                                label="Clear Filters"
                                text
                                @click="clearFilters"
                            />
                        </div>
                    </div>

                    <div class="overflow-x-auto rounded-3xl border border-slate-700/70 bg-slate-900/30">
                        <table class="min-w-full border-collapse">
                            <thead class="bg-slate-900/80">
                                <tr>
                                    <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Name</th>
                                    <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Email</th>
                                    <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Company</th>
                                    <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Region</th>
                                    <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Title</th>
                                    <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Status</th>
                                    <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Last Login</th>
                                    <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Actions</th>
                                </tr>
                            </thead>
                            <tbody v-if="currentPageRows.length">
                                <tr
                                    v-for="user in currentPageRows"
                                    :key="user.userId"
                                    class="border-t border-slate-800/80 text-sm text-slate-200"
                                >
                                    <td class="px-4 py-3 font-medium">
                                        <button
                                            type="button"
                                            class="bg-transparent p-0 text-left font-medium text-blue-300 transition hover:text-blue-200 hover:underline"
                                            @click="openEditDialog(user)"
                                        >
                                            {{ formatUserName(user) }}
                                        </button>
                                    </td>
                                    <td class="px-4 py-3">{{ user.email || 'N/A' }}</td>
                                    <td class="px-4 py-3">{{ user.companyName || 'N/A' }}</td>
                                    <td class="px-4 py-3">{{ user.regionName || 'N/A' }}</td>
                                    <td class="px-4 py-3">{{ user.title || 'N/A' }}</td>
                                    <td class="px-4 py-3">
                                        <Tag :value="user.active ? 'Active' : 'Disabled'" :severity="user.active ? 'success' : 'warning'" />
                                    </td>
                                    <td class="px-4 py-3">{{ formatDateTime(user.lastLogin) }}</td>
                                    <td class="px-4 py-3">
                                        <Button
                                            label="Manage Profile"
                                            text
                                            size="small"
                                            class="!px-0"
                                            @click="openProfile(user)"
                                        />
                                    </td>
                                </tr>
                            </tbody>
                            <tbody v-else>
                                <tr>
                                    <td colspan="8" class="px-4 py-8 text-center text-sm text-slate-400">
                                        {{ isLoading ? 'Loading users...' : 'No users found.' }}
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                    </div>

                    <div class="flex flex-col gap-3 rounded-2xl border border-slate-700/70 bg-slate-900/40 px-4 py-3 xl:flex-row xl:items-center xl:justify-between">
                        <p class="m-0 text-sm text-slate-400">{{ pageReport }}</p>
                        <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-end">
                            <div class="flex items-center gap-3">
                                <span class="text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Page Size</span>
                                <Dropdown
                                    :modelValue="pageSize"
                                    :options="pageSizeOptions"
                                    optionLabel="label"
                                    optionValue="value"
                                    class="w-28"
                                    @change="updatePageSize($event.value || defaultPageSize)"
                                />
                            </div>
                            <div class="rounded-xl border border-slate-700/70 bg-slate-950/50 px-3 py-2 text-sm font-medium text-slate-300">
                                Page {{ currentPage }} of {{ totalPages }}
                            </div>
                            <Paginator
                                :rows="pageSize"
                                :totalRecords="totalRecords"
                                :first="firstRow"
                                template="PrevPageLink PageLinks NextPageLink"
                                @page="updatePage($event.page + 1)"
                            />
                        </div>
                    </div>
                </div>
            </template>
        </Card>

        <BaseDialog
            v-model:visible="dialogVisible"
            :header="dialogMode === 'create' ? 'Add User' : 'Edit User'"
        >
            <BaseFormField label="Name" :errors="validationErrors.fullName">
                <InputText
                    v-model="form.fullName"
                    class="w-full"
                    :disabled="isEditFormLocked"
                    :class="{ '!border-2 !border-red-500': validationErrors.fullName.length }"
                    placeholder="Enter full name"
                    @blur="markTouched('fullName')"
                />
            </BaseFormField>

            <BaseFormField label="Email" :errors="validationErrors.email">
                <InputText
                    v-model="form.email"
                    class="w-full"
                    :disabled="isEditFormLocked"
                    :class="{ '!border-2 !border-red-500': validationErrors.email.length }"
                    placeholder="name@company.com"
                    @blur="markTouched('email')"
                />
            </BaseFormField>

            <BaseFormField label="Company">
                <Dropdown
                    v-model="form.companyId"
                    :options="companyOptions"
                    optionLabel="label"
                    optionValue="value"
                    class="w-full"
                    :disabled="isEditFormLocked"
                    placeholder="Select company"
                    showClear
                    @change="handleCompanyChange($event.value)"
                />
            </BaseFormField>

            <BaseFormField label="Region" :errors="validationErrors.regionId">
                <Dropdown
                    v-model="form.regionId"
                    :options="regionOptions"
                    optionLabel="label"
                    optionValue="value"
                    class="w-full"
                    :disabled="isEditFormLocked || !form.companyId || regionsLoading"
                    :placeholder="form.companyId ? 'Select region' : 'Select a company first'"
                    :class="{ '!border-2 !border-red-500': validationErrors.regionId.length }"
                    @change="markTouched('regionId')"
                />
            </BaseFormField>

            <BaseFormField label="Title">
                <InputText v-model="form.title" class="w-full" :disabled="isEditFormLocked" placeholder="Title" />
            </BaseFormField>

            <BaseFormField label="Status">
                <InputText
                    :modelValue="form.status === 'active' ? 'Active' : 'Disabled'"
                    class="w-full"
                    readonly
                    disabled
                />
            </BaseFormField>

            <div
                v-if="dialogMode === 'edit' && editingUserId"
                class="rounded-2xl border border-slate-700/70 bg-slate-900/40 px-4 py-3"
            >
                <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                    <div>
                        <p class="m-0 text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">Account Status</p>
                        <p class="m-0 mt-1 text-sm text-slate-300">
                            {{ form.status === 'active' ? 'This user can access the application.' : 'This user is currently disabled.' }}
                        </p>
                    </div>
                    <Button
                        v-if="form.status === 'active'"
                        label="Deactivate User"
                        severity="warning"
                        outlined
                        :loading="statusUpdating"
                        @click="updateUserStatus(false)"
                    />
                    <Button
                        v-else
                        label="Activate User"
                        severity="success"
                        outlined
                        :loading="statusUpdating"
                        @click="updateUserStatus(true)"
                    />
                </div>
            </div>

            <template #footer>
                <BaseButtonCancel v-if="dialogMode === 'create' || !isEditFormLocked" @click="closeDialog" />
                <Button
                    v-else
                    label="Close"
                    text
                    @click="closeDialog"
                />
                <BaseButtonCreate v-if="dialogMode === 'create'" :loading="saving" @click="saveUser" />
                <BaseButtonUpdate v-else-if="!isEditFormLocked" :loading="saving" @click="saveUser" />
            </template>
        </BaseDialog>
    </div>
</template>

<script setup lang="ts">
import BasePageHeader from '@/components/layout/BasePageHeader.vue';
import BasePageSubheader from '@/components/layout/BasePageSubheader.vue';
import BaseButtonCancel from '@/components/buttons/BaseButtonCancel.vue';
import BaseButtonCreate from '@/components/buttons/BaseButtonCreate.vue';
import BaseButtonUpdate from '@/components/buttons/BaseButtonUpdate.vue';
import BaseDialog from '@/components/dialogs/BaseDialog.vue';
import BaseFormField from '@/components/forms/BaseFormField.vue';
import { ReferenceDataClient, type RefCompanyDto, type RefRegionDto, User, UserClient } from '@/apiclient/client';
import { useApiStore } from '@/stores/apiStore';
import Button from 'primevue/button';
import Card from 'primevue/card';
import Dropdown from 'primevue/dropdown';
import InputText from 'primevue/inputtext';
import Paginator from 'primevue/paginator';
import Tag from 'primevue/tag';
import { useToast } from 'primevue/usetoast';
import { computed, onMounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';

const apiStore = useApiStore();
const toast = useToast();
const router = useRouter();

const defaultPageSize = 20;
const pageSize = ref(defaultPageSize);
const currentPage = ref(1);
const isLoading = ref(false);
const saving = ref(false);
const statusUpdating = ref(false);
const regionsLoading = ref(false);
const users = ref<User[]>([]);
const companies = ref<RefCompanyDto[]>([]);
const regions = ref<RefRegionDto[]>([]);
const nameFilter = ref('');
const companyFilter = ref<string | undefined>(undefined);
const statusFilter = ref<'active' | 'disabled' | 'all'>('active');
const dialogVisible = ref(false);
const dialogMode = ref<'create' | 'edit'>('create');
const editingUserId = ref<number | null>(null);
const form = ref(createEmptyForm());
const submitAttempted = ref(false);
const touchedFields = ref<Record<'fullName' | 'email' | 'regionId', boolean>>({
    fullName: false,
    email: false,
    regionId: false,
});

const userClient = computed(() => new UserClient(apiStore.api.defaults.baseURL, apiStore.api));
const referenceDataClient = computed(() => new ReferenceDataClient(apiStore.api.defaults.baseURL, apiStore.api));
const isEditFormLocked = computed(() => dialogMode.value === 'edit' && form.value.status === 'disabled');
const filteredUsers = computed(() => {
    const normalizedName = nameFilter.value.trim().toLowerCase();

    return users.value.filter(user => {
        const fullName = formatUserName(user).toLowerCase();
        const userCompanyId = normalizeLookupId(user.companyId);

        const matchesName = !normalizedName || fullName.includes(normalizedName);
        const matchesCompany = !companyFilter.value || userCompanyId === companyFilter.value;
        const matchesStatus =
            statusFilter.value === 'all'
            || (statusFilter.value === 'active' && user.active)
            || (statusFilter.value === 'disabled' && !user.active);

        return matchesName && matchesCompany && matchesStatus;
    });
});
const totalRecords = computed(() => filteredUsers.value.length);
const activeUserCount = computed(() => users.value.filter(user => user.active).length);
const disabledUserCount = computed(() => users.value.filter(user => !user.active).length);
const totalPages = computed(() => Math.max(1, Math.ceil(totalRecords.value / pageSize.value)));
const firstRow = computed(() => (currentPage.value - 1) * pageSize.value);
const currentPageRows = computed(() =>
    filteredUsers.value.slice(firstRow.value, firstRow.value + pageSize.value),
);
const pageSizeOptions = [
    { label: '20', value: 20 },
    { label: '40', value: 40 },
    { label: '100', value: 100 },
];
const statusFilterOptions = [
    { label: 'Active', value: 'active' },
    { label: 'Disabled', value: 'disabled' },
    { label: 'All', value: 'all' },
];
const filterCompanyOptions = computed(() => [
    { label: 'All Companies', value: undefined },
    ...companies.value.map(company => ({
        label: company.name || company.code || 'Unnamed Company',
        value: company.id,
    })),
]);
const companyOptions = computed(() => [
    { label: '', value: undefined },
    ...companies.value.map(company => ({
        label: company.name || company.code || 'Unnamed Company',
        value: company.id,
    })),
]);
const regionOptions = computed(() =>
    regions.value.map(region => ({
        label: region.name || region.code || 'Unnamed Region',
        value: region.id,
    })),
);
const rawValidationErrors = computed(() => {
    const fullNameErrors: string[] = [];
    const emailErrors: string[] = [];
    const regionErrors: string[] = [];

    if (!form.value.fullName.trim()) {
        fullNameErrors.push('Name is required.');
    }

    const normalizedEmail = form.value.email.trim();
    if (!normalizedEmail) {
        emailErrors.push('Email is required.');
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(normalizedEmail)) {
        emailErrors.push('Email must be valid.');
    }

    if (!form.value.companyId && form.value.regionId) {
        regionErrors.push('Region cannot be selected unless a Company is selected.');
    } else if (form.value.regionId && !regions.value.some(region => region.id === form.value.regionId)) {
        regionErrors.push('Selected Region does not belong to the selected Company.');
    }

    return {
        fullName: fullNameErrors,
        email: emailErrors,
        regionId: regionErrors,
    };
});
const validationErrors = computed(() => ({
    fullName: shouldShowFieldError('fullName') ? rawValidationErrors.value.fullName : [],
    email: shouldShowFieldError('email') ? rawValidationErrors.value.email : [],
    regionId: shouldShowFieldError('regionId') ? rawValidationErrors.value.regionId : [],
}));
const pageReport = computed(() => {
    if (!totalRecords.value) {
        return 'Showing 0 users';
    }

    const startRow = firstRow.value + 1;
    const endRow = Math.min(firstRow.value + currentPageRows.value.length, totalRecords.value);
    return `Showing ${startRow}-${endRow} of ${totalRecords.value} users`;
});
const showClearFilters = computed(() =>
    !!nameFilter.value.trim() || !!companyFilter.value || statusFilter.value !== 'active',
);

watch([nameFilter, companyFilter, statusFilter], () => {
    currentPage.value = 1;
});

onMounted(async () => {
    await Promise.all([loadUsers(), loadCompanies()]);
});

async function loadUsers() {
    isLoading.value = true;

    try {
        const result = await userClient.value.getAllUsers();
        users.value = [...result].sort((left, right) => {
            const leftName = formatUserName(left);
            const rightName = formatUserName(right);
            return leftName.localeCompare(rightName);
        });
        currentPage.value = 1;
    } catch (error) {
        users.value = [];
        toast.add({
            severity: 'error',
            summary: 'Users Unavailable',
            detail: 'The user management screen could not load dbo.Users from the API.',
            life: 4000,
        });
        console.error(error);
    } finally {
        isLoading.value = false;
    }
}

async function loadCompanies() {
    try {
        const result = await referenceDataClient.value.getCompanies();
        companies.value = [...result]
            .filter(company => company.isActive !== false)
            .sort((left, right) => (left.name || '').localeCompare(right.name || ''));
    } catch (error) {
        companies.value = [];
        toast.add({
            severity: 'warn',
            summary: 'Companies Unavailable',
            detail: 'Company options could not be loaded. You can still leave Company blank.',
            life: 4000,
        });
        console.error(error);
    }
}

async function loadRegions(companyId?: string) {
    if (!companyId) {
        regions.value = [];
        return;
    }

    regionsLoading.value = true;

    try {
        const result = await referenceDataClient.value.getRegions(companyId);
        regions.value = result.filter(region => region.isActive !== false);
    } catch (error) {
        regions.value = [];
        toast.add({
            severity: 'warn',
            summary: 'Regions Unavailable',
            detail: 'Region options could not be loaded for the selected Company.',
            life: 4000,
        });
        console.error(error);
    } finally {
        regionsLoading.value = false;
    }
}

function updatePage(nextPage: number) {
    currentPage.value = Math.min(Math.max(nextPage, 1), totalPages.value);
}

function updatePageSize(nextPageSize: number) {
    pageSize.value = nextPageSize;
    currentPage.value = 1;
}

function clearFilters() {
    nameFilter.value = '';
    companyFilter.value = undefined;
    statusFilter.value = 'active';
}

function openCreateDialog() {
    dialogMode.value = 'create';
    editingUserId.value = null;
    form.value = createEmptyForm();
    regions.value = [];
    resetDialogValidation();
    dialogVisible.value = true;
}

async function openEditDialog(user: User) {
    dialogMode.value = 'edit';
    editingUserId.value = user.userId ?? null;
    const selectedCompanyId = normalizeLookupId(user.companyId);
    const selectedRegionId = normalizeLookupId(user.regionId);
    await loadRegions(selectedCompanyId);
    form.value = {
        fullName: formatUserName(user),
        email: user.email || '',
        companyId: selectedCompanyId,
        regionId: selectedRegionId,
        title: user.title || '',
        status: user.active ? 'active' : 'disabled',
        azureAdObjectId: user.azureAdObjectId,
    };

    if (form.value.regionId && !regions.value.some(region => region.id === form.value.regionId)) {
        form.value.regionId = undefined;
    }

    resetDialogValidation();
    dialogVisible.value = true;
}

function closeDialog() {
    dialogVisible.value = false;
    editingUserId.value = null;
    form.value = createEmptyForm();
    regions.value = [];
    resetDialogValidation();
}

function resetDialogValidation() {
    submitAttempted.value = false;
    touchedFields.value = {
        fullName: false,
        email: false,
        regionId: false,
    };
}

function markTouched(fieldName: 'fullName' | 'email' | 'regionId') {
    touchedFields.value[fieldName] = true;
}

function shouldShowFieldError(fieldName: 'fullName' | 'email' | 'regionId') {
    return submitAttempted.value || touchedFields.value[fieldName];
}

async function handleCompanyChange(nextCompanyId?: string) {
    form.value.companyId = normalizeLookupId(nextCompanyId);

    if (!form.value.companyId) {
        form.value.regionId = undefined;
        regions.value = [];
        return;
    }

    await loadRegions(form.value.companyId);

    if (form.value.regionId && !regions.value.some(region => region.id === form.value.regionId)) {
        form.value.regionId = undefined;
    }
}

async function saveUser() {
    if (isEditFormLocked.value) {
        return;
    }

    submitAttempted.value = true;

    if (
        rawValidationErrors.value.fullName.length
        || rawValidationErrors.value.email.length
        || rawValidationErrors.value.regionId.length
    ) {
        return;
    }

    saving.value = true;

    try {
        if (dialogMode.value === 'create') {
            const newUser = buildUserPayload();
            await userClient.value.createUser(newUser);

            toast.add({
                severity: 'success',
                summary: 'User Created',
                detail: `${form.value.fullName} was added successfully.`,
                life: 3000,
            });
        } else {
            const userId = editingUserId.value;
            if (!userId) {
                throw new Error('UserId is required for editing.');
            }

            const existingUser = users.value.find(user => user.userId === userId);
            if (!existingUser) {
                throw new Error('Selected user was not found.');
            }

            const updatedUser = buildUserPayload(existingUser);
            updatedUser.userId = userId;
            await userClient.value.updateUser(userId, updatedUser);

            toast.add({
                severity: 'success',
                summary: 'User Updated',
                detail: `${form.value.fullName} was updated successfully.`,
                life: 3000,
            });
        }

        await loadUsers();
        closeDialog();
    } catch (error) {
        toast.add({
            severity: 'error',
            summary: 'Save Failed',
            detail: 'The user record could not be saved.',
            life: 4000,
        });
        console.error(error);
    } finally {
        saving.value = false;
    }
}

async function updateUserStatus(shouldActivate: boolean) {
    const userId = editingUserId.value;
    if (!userId) {
        return;
    }

    statusUpdating.value = true;

    try {
        if (shouldActivate) {
            await userClient.value.activateUser(userId);
            form.value.status = 'active';
        } else {
            await userClient.value.disableUser(userId);
            form.value.status = 'disabled';
        }

        await loadUsers();

        toast.add({
            severity: 'success',
            summary: shouldActivate ? 'User Activated' : 'User Deactivated',
            detail: `${form.value.fullName} is now ${shouldActivate ? 'active' : 'disabled'}.`,
            life: 3000,
        });

        if (!shouldActivate) {
            closeDialog();
        }
    } catch (error) {
        toast.add({
            severity: 'error',
            summary: 'Status Update Failed',
            detail: 'The user status could not be changed.',
            life: 4000,
        });
        console.error(error);
    } finally {
        statusUpdating.value = false;
    }
}

function openProfile(user: User) {
    if (!user.userId) {
        return;
    }

    void router.push({
        path: '/safety-application/administration/profile',
        query: { userId: String(user.userId) },
    });
}

function formatUserName(user: User) {
    const fullName = `${user.firstName || ''} ${user.lastName || ''}`.trim();
    if (fullName) {
        return fullName;
    }

    return user.email || `User ${user.userId}`;
}

function formatDateTime(value: Date | undefined) {
    if (!value) {
        return 'Never';
    }

    return new Intl.DateTimeFormat('en-US', {
        month: '2-digit',
        day: '2-digit',
        year: 'numeric',
        hour: 'numeric',
        minute: '2-digit',
    }).format(value);
}

function createEmptyForm() {
    return {
        fullName: '',
        email: '',
        companyId: undefined as string | undefined,
        regionId: undefined as string | undefined,
        title: '',
        status: 'active' as 'active' | 'disabled',
        azureAdObjectId: crypto.randomUUID(),
    };
}

function buildUserPayload(existingUser?: User) {
    const companyId = normalizeLookupId(form.value.companyId);
    const regionId = companyId ? normalizeLookupId(form.value.regionId) : undefined;
    const { firstName, lastName } = splitFullName(form.value.fullName);
    return User.fromJS({
        userId: existingUser?.userId,
        azureAdObjectId: existingUser?.azureAdObjectId || form.value.azureAdObjectId,
        firstName,
        lastName,
        email: form.value.email.trim(),
        companyId,
        regionId,
        companyName: undefined,
        regionName: undefined,
        title: form.value.title.trim() || undefined,
        active: form.value.status === 'active',
        disabledOn: existingUser?.disabledOn,
        disabledBy: existingUser?.disabledBy,
        disabledReason: existingUser?.disabledReason,
        lastLogin: existingUser?.lastLogin,
        roles: existingUser?.roles,
        createdOn: existingUser?.createdOn,
        modifiedOn: existingUser?.modifiedOn,
    });
}

function normalizeLookupId(value: string | null | undefined) {
    const normalizedValue = value?.trim();
    return normalizedValue ? normalizedValue : undefined;
}

function splitFullName(fullName: string) {
    const parts = fullName
        .trim()
        .split(/\s+/)
        .filter(Boolean);

    if (!parts.length) {
        return { firstName: '', lastName: '' };
    }

    if (parts.length === 1) {
        return { firstName: parts[0], lastName: '' };
    }

    return {
        firstName: parts[0],
        lastName: parts.slice(1).join(' '),
    };
}
</script>
