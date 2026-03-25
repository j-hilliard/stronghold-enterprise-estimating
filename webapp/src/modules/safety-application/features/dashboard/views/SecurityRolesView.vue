<template>
    <div class="space-y-6">
        <BasePageHeader
            title="Security Roles"
            subtitle="Manage membership and permissions for the fixed application security roles."
            icon="pi pi-id-card"
        />

        <Card class="border border-slate-700/70 bg-slate-800/70 shadow-lg shadow-slate-950/20">
            <template #content>
                <div class="grid gap-4 p-6 md:grid-cols-3">
                    <div class="rounded-2xl border border-slate-700/70 bg-slate-900/50 p-4">
                        <p class="mb-2 text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">Defined Roles</p>
                        <p class="m-0 text-3xl font-semibold text-white">{{ securityRoles.length }}</p>
                    </div>
                    <div class="rounded-2xl border border-emerald-500/20 bg-emerald-500/10 p-4">
                        <p class="mb-2 text-xs font-semibold uppercase tracking-[0.24em] text-emerald-200">Loaded Roles</p>
                        <p class="m-0 text-3xl font-semibold text-white">{{ availableRoleCount }}</p>
                    </div>
                    <div class="rounded-2xl border border-sky-500/20 bg-sky-500/10 p-4">
                        <p class="mb-2 text-xs font-semibold uppercase tracking-[0.24em] text-sky-200">Active Users Available</p>
                        <p class="m-0 text-3xl font-semibold text-white">{{ activeUsers.length }}</p>
                    </div>
                </div>
            </template>
        </Card>

        <BasePageSubheader title="Role Management" />

        <Card class="border border-slate-700/70 bg-slate-800/70 shadow-lg shadow-slate-950/20">
            <template #content>
                <div class="space-y-4 p-6">
                    <div class="rounded-2xl border border-slate-700/70 bg-slate-900/40 px-4 py-3">
                        <p class="m-0 text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">Fixed Roles</p>
                        <p class="m-0 mt-2 text-sm text-slate-300">
                            These roles are predefined and membership can be managed here without editing the role definitions themselves.
                        </p>
                    </div>

                    <div class="overflow-x-auto rounded-3xl border border-slate-700/70 bg-slate-900/30">
                        <table class="min-w-full border-collapse">
                            <thead class="bg-slate-900/80">
                                <tr>
                                    <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Role</th>
                                    <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Mapped System Role</th>
                                    <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Description</th>
                                    <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Members</th>
                                    <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Permissions</th>
                                    <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr
                                    v-for="roleDefinition in displayRoles"
                                    :key="roleDefinition.key"
                                    class="border-t border-slate-800/80 text-sm text-slate-200"
                                >
                                    <td class="px-4 py-4">
                                        <div>
                                            <p class="m-0 font-medium text-white">{{ roleDefinition.label }}</p>
                                            <p class="m-0 mt-1 text-xs uppercase tracking-[0.2em] text-slate-500">Fixed Role</p>
                                        </div>
                                    </td>
                                    <td class="px-4 py-4">
                                        <Tag
                                            :value="roleDefinition.role?.name || 'Unavailable'"
                                            :severity="roleDefinition.role ? 'info' : 'warning'"
                                        />
                                    </td>
                                    <td class="px-4 py-4">
                                        <p class="m-0 max-w-xl text-sm leading-6 text-slate-300">
                                            {{ roleDefinition.role?.description || roleDefinition.fallbackDescription }}
                                        </p>
                                    </td>
                                    <td class="px-4 py-4">
                                        <div class="space-y-2">
                                            <p class="m-0 font-medium text-white">
                                                {{ roleDefinition.members.length }} member{{ roleDefinition.members.length === 1 ? '' : 's' }}
                                            </p>
                                            <div v-if="roleDefinition.members.length" class="flex flex-wrap gap-2">
                                                <span
                                                    v-for="member in roleDefinition.members.slice(0, 4)"
                                                    :key="`${roleDefinition.key}-${member.userId}`"
                                                    class="rounded-full border border-slate-700 bg-slate-950/70 px-3 py-1 text-xs text-slate-200"
                                                >
                                                    {{ formatUserName(member) }}
                                                </span>
                                                <span
                                                    v-if="roleDefinition.members.length > 4"
                                                    class="rounded-full border border-slate-700 bg-slate-950/70 px-3 py-1 text-xs text-slate-400"
                                                >
                                                    +{{ roleDefinition.members.length - 4 }} more
                                                </span>
                                            </div>
                                            <p v-else class="m-0 text-sm text-slate-500">No members assigned.</p>
                                        </div>
                                    </td>
                                    <td class="px-4 py-4">
                                        <div class="space-y-2">
                                            <p class="m-0 font-medium text-white">
                                                {{ roleDefinition.permissions.length }} permission{{ roleDefinition.permissions.length === 1 ? '' : 's' }}
                                            </p>
                                            <div v-if="roleDefinition.permissions.length" class="flex flex-wrap gap-2">
                                                <span
                                                    v-for="permission in roleDefinition.permissions.slice(0, 2)"
                                                    :key="`${roleDefinition.key}-permission-${permission.permissionId}`"
                                                    class="rounded-full border border-sky-500/20 bg-sky-500/10 px-3 py-1 text-xs text-sky-100"
                                                >
                                                    {{ permission.name }}
                                                </span>
                                                <span
                                                    v-if="roleDefinition.permissions.length > 2"
                                                    class="rounded-full border border-slate-700 bg-slate-950/70 px-3 py-1 text-xs text-slate-400"
                                                >
                                                    +{{ roleDefinition.permissions.length - 2 }} more
                                                </span>
                                            </div>
                                            <p v-else class="m-0 text-sm text-slate-500">No permissions assigned.</p>
                                        </div>
                                    </td>
                                    <td class="px-4 py-4">
                                        <div class="flex flex-col gap-2">
                                            <Button
                                                label="Manage Members"
                                                icon="pi pi-users"
                                                :disabled="!roleDefinition.role"
                                                @click="openManageMembers(roleDefinition.key)"
                                            />
                                            <Button
                                                label="Manage Permissions"
                                                icon="pi pi-key"
                                                severity="secondary"
                                                outlined
                                                :disabled="!roleDefinition.role"
                                                @click="openManagePermissions(roleDefinition.key)"
                                            />
                                        </div>
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </template>
        </Card>

        <BaseDialog
            v-model:visible="manageDialogVisible"
            :header="selectedRoleDefinition ? `Manage Members: ${selectedRoleDefinition.label}` : 'Manage Members'"
            :style="{ width: '960px', maxWidth: '96vw' }"
        >
            <div v-if="selectedRoleDefinition" class="space-y-5">
                <div class="rounded-2xl border border-slate-700/70 bg-slate-900/40 px-4 py-3">
                    <p class="m-0 text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">Selected Role</p>
                    <div class="mt-2 flex flex-wrap items-center gap-3">
                        <Tag :value="selectedRoleDefinition.label" severity="info" />
                        <span class="text-sm text-slate-300">{{ selectedRoleDefinition.role?.description || selectedRoleDefinition.fallbackDescription }}</span>
                    </div>
                </div>

                <div class="grid gap-4 xl:grid-cols-[minmax(0,1.2fr)_minmax(0,1fr)]">
                    <section class="space-y-4 rounded-3xl border border-slate-700/70 bg-slate-900/30 p-4">
                        <div class="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
                            <div class="space-y-1">
                                <p class="m-0 text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">Active Users</p>
                                <p class="m-0 text-sm text-slate-300">Search active users and choose who should belong to this role.</p>
                            </div>
                            <div class="w-full md:max-w-sm">
                                <label class="mb-2 block text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Search</label>
                                <InputText
                                    v-model="memberSearch"
                                    class="w-full"
                                    placeholder="Search by name, email, or company"
                                />
                            </div>
                        </div>

                        <div class="rounded-2xl border border-slate-700/70 bg-slate-950/50">
                            <div class="grid grid-cols-[auto_minmax(0,1fr)_auto] gap-3 border-b border-slate-800/80 px-4 py-3 text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">
                                <span>Assign</span>
                                <span>User</span>
                                <span>Status</span>
                            </div>

                            <div v-if="filteredActiveUsers.length" class="max-h-[420px] overflow-y-auto">
                                <div
                                    v-for="user in filteredActiveUsers"
                                    :key="user.userId"
                                    class="grid grid-cols-[auto_minmax(0,1fr)_auto] gap-3 border-b border-slate-800/60 px-4 py-3 transition hover:bg-slate-900/60"
                                >
                                    <Checkbox
                                        v-model="selectedMemberIds"
                                        :inputId="`role-user-${selectedRoleDefinition.key}-${user.userId}`"
                                        :value="user.userId"
                                    />
                                    <label
                                        :for="`role-user-${selectedRoleDefinition.key}-${user.userId}`"
                                        class="min-w-0 cursor-pointer"
                                    >
                                        <p class="m-0 truncate font-medium text-white">{{ formatUserName(user) }}</p>
                                        <p class="m-0 mt-1 truncate text-sm text-slate-400">{{ user.email || 'No email' }}</p>
                                        <p class="m-0 mt-1 text-xs uppercase tracking-[0.2em] text-slate-500">{{ user.companyName || 'No company' }}</p>
                                    </label>
                                    <Tag
                                        :value="isCurrentlyAssigned(user.userId) ? 'Assigned' : 'Available'"
                                        :severity="isCurrentlyAssigned(user.userId) ? 'success' : 'info'"
                                    />
                                </div>
                            </div>

                            <div v-else class="px-4 py-10 text-center text-sm text-slate-500">
                                {{ activeUsersLoading ? 'Loading active users...' : 'No active users match the current search.' }}
                            </div>
                        </div>
                    </section>

                    <section class="space-y-4 rounded-3xl border border-slate-700/70 bg-slate-900/30 p-4">
                        <div class="space-y-1">
                            <p class="m-0 text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">Assigned Members</p>
                            <p class="m-0 text-sm text-slate-300">Assigned and unassigned states are shown separately so changes are clear before saving.</p>
                        </div>

                        <div class="rounded-2xl border border-emerald-500/20 bg-emerald-500/10 p-4">
                            <div class="flex items-center justify-between gap-3">
                                <div>
                                    <p class="m-0 text-xs font-semibold uppercase tracking-[0.2em] text-emerald-200">Selected Active Members</p>
                                    <p class="m-0 mt-1 text-sm text-slate-200">{{ selectedMemberObjects.length }} selected</p>
                                </div>
                                <Tag :value="`${selectedMemberObjects.length}`" severity="success" />
                            </div>
                            <div class="mt-3 flex max-h-40 flex-wrap gap-2 overflow-y-auto">
                                <span
                                    v-for="member in selectedMemberObjects"
                                    :key="`selected-${member.userId}`"
                                    class="rounded-full border border-emerald-400/20 bg-slate-950/60 px-3 py-1 text-xs text-slate-100"
                                >
                                    {{ formatUserName(member) }}
                                </span>
                                <p v-if="!selectedMemberObjects.length" class="m-0 text-sm text-slate-400">No active users selected.</p>
                            </div>
                        </div>

                        <div
                            v-if="inactiveAssignedMembers.length"
                            class="rounded-2xl border border-amber-500/20 bg-amber-500/10 p-4"
                        >
                            <div class="flex items-center justify-between gap-3">
                                <div>
                                    <p class="m-0 text-xs font-semibold uppercase tracking-[0.2em] text-amber-200">Inactive Assigned Members</p>
                                    <p class="m-0 mt-1 text-sm text-slate-200">These users are already assigned but cannot be re-added unless they become active again.</p>
                                </div>
                                <Tag :value="`${inactiveAssignedMembers.length}`" severity="warning" />
                            </div>

                            <div class="mt-3 space-y-2">
                                <div
                                    v-for="member in inactiveAssignedMembers"
                                    :key="`inactive-${member.userId}`"
                                    class="flex items-center justify-between gap-3 rounded-2xl border border-amber-400/20 bg-slate-950/50 px-3 py-3"
                                >
                                    <div>
                                        <p class="m-0 font-medium text-white">{{ formatUserName(member) }}</p>
                                        <p class="m-0 mt-1 text-sm text-slate-400">{{ member.email || 'No email' }}</p>
                                    </div>
                                    <Button
                                        label="Remove"
                                        icon="pi pi-times"
                                        severity="warning"
                                        text
                                        @click="removeInactiveAssignedMember(member.userId)"
                                    />
                                </div>
                            </div>
                        </div>

                        <div class="rounded-2xl border border-slate-700/70 bg-slate-950/40 px-4 py-3">
                            <p class="m-0 text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Pending Result</p>
                            <p class="m-0 mt-2 text-sm text-slate-300">
                                Saving will keep {{ desiredMemberCount }} member{{ desiredMemberCount === 1 ? '' : 's' }} in this role.
                            </p>
                        </div>
                    </section>
                </div>
            </div>

            <template #footer>
                <BaseButtonCancel :disabled="savingMembers" @click="closeManageDialog" />
                <Button
                    label="Save"
                    icon="pi pi-check"
                    :loading="savingMembers"
                    @click="saveRoleMembers"
                />
            </template>
        </BaseDialog>

        <BaseDialog
            v-model:visible="permissionDialogVisible"
            :header="selectedRoleDefinition ? `Manage Permissions: ${selectedRoleDefinition.label}` : 'Manage Permissions'"
            :style="{ width: '960px', maxWidth: '96vw' }"
        >
            <div v-if="selectedRoleDefinition" class="space-y-5">
                <div class="rounded-2xl border border-slate-700/70 bg-slate-900/40 px-4 py-3">
                    <p class="m-0 text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">Selected Role</p>
                    <div class="mt-2 flex flex-wrap items-center gap-3">
                        <Tag :value="selectedRoleDefinition.label" severity="info" />
                        <span class="text-sm text-slate-300">Choose which permissions belong to this role. Assigned permissions are persisted to <code>dbo.RolePermissions</code>.</span>
                    </div>
                </div>

                <div class="grid gap-4 xl:grid-cols-[minmax(0,1.2fr)_minmax(0,1fr)]">
                    <section class="space-y-4 rounded-3xl border border-slate-700/70 bg-slate-900/30 p-4">
                        <div class="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
                            <div class="space-y-1">
                                <p class="m-0 text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">Available Permissions</p>
                                <p class="m-0 text-sm text-slate-300">Search and select permissions to add or remove for this role.</p>
                            </div>
                            <div class="w-full md:max-w-sm">
                                <label class="mb-2 block text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">Search</label>
                                <InputText
                                    v-model="permissionSearch"
                                    class="w-full"
                                    placeholder="Search by code, name, or category"
                                />
                            </div>
                        </div>

                        <div class="rounded-2xl border border-slate-700/70 bg-slate-950/50">
                            <div class="grid grid-cols-[auto_minmax(0,1fr)_auto] gap-3 border-b border-slate-800/80 px-4 py-3 text-xs font-semibold uppercase tracking-[0.2em] text-slate-400">
                                <span>Assign</span>
                                <span>Permission</span>
                                <span>State</span>
                            </div>

                            <div v-if="filteredPermissions.length" class="max-h-[420px] overflow-y-auto">
                                <div
                                    v-for="permission in filteredPermissions"
                                    :key="permission.permissionId"
                                    class="grid grid-cols-[auto_minmax(0,1fr)_auto] gap-3 border-b border-slate-800/60 px-4 py-3 transition hover:bg-slate-900/60"
                                >
                                    <Checkbox
                                        v-model="selectedPermissionIds"
                                        :inputId="`role-permission-${selectedRoleDefinition.key}-${permission.permissionId}`"
                                        :value="permission.permissionId"
                                    />
                                    <label
                                        :for="`role-permission-${selectedRoleDefinition.key}-${permission.permissionId}`"
                                        class="min-w-0 cursor-pointer"
                                    >
                                        <p class="m-0 truncate font-medium text-white">{{ permission.name }}</p>
                                        <p class="m-0 mt-1 truncate text-sm text-slate-400">{{ permission.code }}</p>
                                        <p class="m-0 mt-1 text-xs uppercase tracking-[0.2em] text-slate-500">{{ permission.category || 'General' }}</p>
                                    </label>
                                    <Tag
                                        :value="originalPermissionIds.has(permission.permissionId) ? 'Assigned' : 'Available'"
                                        :severity="originalPermissionIds.has(permission.permissionId) ? 'success' : 'info'"
                                    />
                                </div>
                            </div>

                            <div v-else class="px-4 py-10 text-center text-sm text-slate-500">
                                {{ permissionsLoading ? 'Loading permissions...' : 'No permissions match the current search.' }}
                            </div>
                        </div>
                    </section>

                    <section class="space-y-4 rounded-3xl border border-slate-700/70 bg-slate-900/30 p-4">
                        <div class="space-y-1">
                            <p class="m-0 text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">Selected Permissions</p>
                            <p class="m-0 text-sm text-slate-300">Review the permission set before saving changes.</p>
                        </div>

                        <div class="rounded-2xl border border-sky-500/20 bg-sky-500/10 p-4">
                            <div class="flex items-center justify-between gap-3">
                                <div>
                                    <p class="m-0 text-xs font-semibold uppercase tracking-[0.2em] text-sky-200">Chosen Permissions</p>
                                    <p class="m-0 mt-1 text-sm text-slate-200">{{ selectedPermissionObjects.length }} selected</p>
                                </div>
                                <Tag :value="`${selectedPermissionObjects.length}`" severity="info" />
                            </div>
                            <div class="mt-3 space-y-2">
                                <div
                                    v-for="permission in selectedPermissionObjects"
                                    :key="`selected-permission-${permission.permissionId}`"
                                    class="rounded-2xl border border-sky-400/20 bg-slate-950/50 px-3 py-3"
                                >
                                    <p class="m-0 font-medium text-white">{{ permission.name }}</p>
                                    <p class="m-0 mt-1 text-sm text-slate-400">{{ permission.code }}</p>
                                    <p class="m-0 mt-1 text-xs uppercase tracking-[0.2em] text-slate-500">{{ permission.category || 'General' }}</p>
                                </div>
                                <p v-if="!selectedPermissionObjects.length" class="m-0 text-sm text-slate-400">No permissions selected.</p>
                            </div>
                        </div>
                    </section>
                </div>
            </div>

            <template #footer>
                <BaseButtonCancel :disabled="savingPermissions" @click="closePermissionDialog" />
                <Button
                    label="Save"
                    icon="pi pi-check"
                    :loading="savingPermissions"
                    @click="saveRolePermissions"
                />
            </template>
        </BaseDialog>
    </div>
</template>

<script setup lang="ts">
import BaseButtonCancel from '@/components/buttons/BaseButtonCancel.vue';
import BaseDialog from '@/components/dialogs/BaseDialog.vue';
import BasePageHeader from '@/components/layout/BasePageHeader.vue';
import BasePageSubheader from '@/components/layout/BasePageSubheader.vue';
import { Role, RoleClient, User, UserClient, UserRole, UserRoleClient } from '@/apiclient/client';
import { useApiStore } from '@/stores/apiStore';
import Button from 'primevue/button';
import Card from 'primevue/card';
import Checkbox from 'primevue/checkbox';
import InputText from 'primevue/inputtext';
import Tag from 'primevue/tag';
import { useToast } from 'primevue/usetoast';
import { computed, onMounted, ref } from 'vue';

type SecurityRoleKey = 'users' | 'safety-team' | 'safety-admin';

type SecurityRoleDefinition = {
    key: SecurityRoleKey;
    label: string;
    backendName: string;
    fallbackDescription: string;
};

type PermissionRecord = {
    permissionId: number;
    code: string;
    name: string;
    description?: string;
    category?: string;
    isActive: boolean;
};

const securityRoles: SecurityRoleDefinition[] = [
    {
        key: 'users',
        label: 'Users',
        backendName: 'User',
        fallbackDescription: 'General user access for submitting and working within the application.',
    },
    {
        key: 'safety-team',
        label: 'Safety Team',
        backendName: 'SafetyTeam',
        fallbackDescription: 'Safety team access for working with incidents, investigations, and supporting safety data.',
    },
    {
        key: 'safety-admin',
        label: 'Safety Admin',
        backendName: 'Administrator',
        fallbackDescription: 'Administrative access for managing application security and core administration tasks.',
    },
];

const apiStore = useApiStore();
const toast = useToast();

const rolesLoading = ref(false);
const activeUsersLoading = ref(false);
const permissionsLoading = ref(false);
const savingMembers = ref(false);
const savingPermissions = ref(false);
const availableRoles = ref<Role[]>([]);
const activeUsers = ref<User[]>([]);
const permissions = ref<PermissionRecord[]>([]);
const roleMembers = ref<Record<number, User[]>>({});
const rolePermissions = ref<Record<number, PermissionRecord[]>>({});
const manageDialogVisible = ref(false);
const permissionDialogVisible = ref(false);
const selectedRoleKey = ref<SecurityRoleKey | null>(null);
const memberSearch = ref('');
const permissionSearch = ref('');
const selectedMemberIds = ref<number[]>([]);
const selectedPermissionIds = ref<number[]>([]);
const retainedInactiveAssignedIds = ref<number[]>([]);
const originalPermissionIds = ref(new Set<number>());

const roleClient = computed(() => new RoleClient(apiStore.api.defaults.baseURL, apiStore.api));
const userClient = computed(() => new UserClient(apiStore.api.defaults.baseURL, apiStore.api));
const userRoleClient = computed(() => new UserRoleClient(apiStore.api.defaults.baseURL, apiStore.api));

const selectedRoleDefinition = computed(() =>
    displayRoles.value.find(roleDefinition => roleDefinition.key === selectedRoleKey.value),
);

const displayRoles = computed(() =>
    securityRoles.map(roleDefinition => {
        const role = availableRoles.value.find(candidate => candidate.name === roleDefinition.backendName);
        const members = role?.roleId ? roleMembers.value[role.roleId] || [] : [];
        const assignedPermissions = role?.roleId ? rolePermissions.value[role.roleId] || [] : [];

        return {
            ...roleDefinition,
            role,
            members: [...members].sort((left, right) => formatUserName(left).localeCompare(formatUserName(right))),
            permissions: [...assignedPermissions].sort((left, right) => left.name.localeCompare(right.name)),
        };
    }),
);

const availableRoleCount = computed(() => displayRoles.value.filter(roleDefinition => !!roleDefinition.role).length);

const assignedMembers = computed(() => selectedRoleDefinition.value?.members || []);
const assignedPermissionRecords = computed(() => selectedRoleDefinition.value?.permissions || []);
const inactiveAssignedMembers = computed(() =>
    assignedMembers.value
        .filter(member => !member.active && member.userId && retainedInactiveAssignedIds.value.includes(member.userId))
        .sort((left, right) => formatUserName(left).localeCompare(formatUserName(right))),
);
const selectedMemberObjects = computed(() =>
    activeUsers.value
        .filter(user => user.userId && selectedMemberIds.value.includes(user.userId))
        .sort((left, right) => formatUserName(left).localeCompare(formatUserName(right))),
);
const selectedPermissionObjects = computed(() =>
    permissions.value
        .filter(permission => selectedPermissionIds.value.includes(permission.permissionId))
        .sort((left, right) => left.name.localeCompare(right.name)),
);
const desiredMemberCount = computed(() => selectedMemberIds.value.length + retainedInactiveAssignedIds.value.length);
const filteredActiveUsers = computed(() => {
    const normalizedSearch = memberSearch.value.trim().toLowerCase();

    return [...activeUsers.value]
        .filter(user => {
            if (!normalizedSearch) {
                return true;
            }

            const searchTarget = [formatUserName(user), user.email || '', user.companyName || '']
                .join(' ')
                .toLowerCase();

            return searchTarget.includes(normalizedSearch);
        })
        .sort((left, right) => formatUserName(left).localeCompare(formatUserName(right)));
});
const filteredPermissions = computed(() => {
    const normalizedSearch = permissionSearch.value.trim().toLowerCase();

    return [...permissions.value]
        .filter(permission => {
            if (!normalizedSearch) {
                return true;
            }

            const searchTarget = [permission.name, permission.code, permission.category || '', permission.description || '']
                .join(' ')
                .toLowerCase();

            return searchTarget.includes(normalizedSearch);
        })
        .sort((left, right) =>
            `${left.category || ''} ${left.name}`.localeCompare(`${right.category || ''} ${right.name}`),
        );
});

onMounted(async () => {
    await Promise.all([loadRoles(), loadActiveUsers(), loadPermissions()]);
    await Promise.all([loadDisplayedRoleMembers(), loadDisplayedRolePermissions()]);
});

async function loadRoles() {
    rolesLoading.value = true;

    try {
        availableRoles.value = await roleClient.value.getAllRoles();
    } catch (error) {
        availableRoles.value = [];
        toast.add({
            severity: 'error',
            summary: 'Roles Unavailable',
            detail: 'The security role definitions could not be loaded from the API.',
            life: 4000,
        });
        console.error(error);
    } finally {
        rolesLoading.value = false;
    }
}

async function loadActiveUsers() {
    activeUsersLoading.value = true;

    try {
        activeUsers.value = await userClient.value.getActiveUsers();
    } catch (error) {
        activeUsers.value = [];
        toast.add({
            severity: 'error',
            summary: 'Active Users Unavailable',
            detail: 'Active users could not be loaded for role assignment.',
            life: 4000,
        });
        console.error(error);
    } finally {
        activeUsersLoading.value = false;
    }
}

async function loadPermissions() {
    permissionsLoading.value = true;

    try {
        const response = await apiStore.api.get<PermissionRecord[]>('/v1/Permission');
        permissions.value = [...response.data]
            .filter(permission => permission.isActive !== false)
            .sort((left, right) => `${left.category || ''} ${left.name}`.localeCompare(`${right.category || ''} ${right.name}`));
    } catch (error) {
        permissions.value = [];
        toast.add({
            severity: 'error',
            summary: 'Permissions Unavailable',
            detail: 'Permissions could not be loaded from dbo.Permissions.',
            life: 4000,
        });
        console.error(error);
    } finally {
        permissionsLoading.value = false;
    }
}

async function loadDisplayedRoleMembers() {
    const roleEntries = displayRoles.value.filter(roleDefinition => roleDefinition.role?.roleId);

    await Promise.all(
        roleEntries.map(roleDefinition => loadRoleMembers(roleDefinition.role!.roleId!)),
    );
}

async function loadDisplayedRolePermissions() {
    const roleEntries = displayRoles.value.filter(roleDefinition => roleDefinition.role?.roleId);

    await Promise.all(
        roleEntries.map(roleDefinition => loadRolePermissions(roleDefinition.role!.roleId!)),
    );
}

async function loadRoleMembers(roleId: number) {
    try {
        const members = await userRoleClient.value.getRoleMembership(roleId);
        roleMembers.value = {
            ...roleMembers.value,
            [roleId]: members || [],
        };
    } catch (error: any) {
        if (error?.status === 404) {
            roleMembers.value = {
                ...roleMembers.value,
                [roleId]: [],
            };
            return;
        }

        toast.add({
            severity: 'warn',
            summary: 'Members Unavailable',
            detail: 'One or more role membership lists could not be loaded.',
            life: 4000,
        });
        console.error(error);
    }
}

async function loadRolePermissions(roleId: number) {
    try {
        const response = await apiStore.api.get<PermissionRecord[]>(`/v1/RolePermission/RolePermissions/${roleId}`);
        rolePermissions.value = {
            ...rolePermissions.value,
            [roleId]: response.data || [],
        };
    } catch (error: any) {
        if (error?.response?.status === 404) {
            rolePermissions.value = {
                ...rolePermissions.value,
                [roleId]: [],
            };
            return;
        }

        toast.add({
            severity: 'warn',
            summary: 'Permissions Unavailable',
            detail: 'One or more role permission lists could not be loaded.',
            life: 4000,
        });
        console.error(error);
    }
}

function openManageMembers(roleKey: SecurityRoleKey) {
    selectedRoleKey.value = roleKey;
    memberSearch.value = '';

    const roleDefinition = displayRoles.value.find(role => role.key === roleKey);
    const members = roleDefinition?.members || [];

    selectedMemberIds.value = members
        .filter(member => member.active && member.userId)
        .map(member => member.userId!);
    retainedInactiveAssignedIds.value = members
        .filter(member => !member.active && member.userId)
        .map(member => member.userId!);

    manageDialogVisible.value = true;
}

function closeManageDialog() {
    manageDialogVisible.value = false;
    selectedRoleKey.value = null;
    memberSearch.value = '';
    selectedMemberIds.value = [];
    retainedInactiveAssignedIds.value = [];
}

function openManagePermissions(roleKey: SecurityRoleKey) {
    selectedRoleKey.value = roleKey;
    permissionSearch.value = '';

    const roleDefinition = displayRoles.value.find(role => role.key === roleKey);
    const assignedPermissions = roleDefinition?.permissions || [];

    selectedPermissionIds.value = assignedPermissions.map(permission => permission.permissionId);
    originalPermissionIds.value = new Set(selectedPermissionIds.value);

    permissionDialogVisible.value = true;
}

function closePermissionDialog() {
    permissionDialogVisible.value = false;
    selectedRoleKey.value = null;
    permissionSearch.value = '';
    selectedPermissionIds.value = [];
    originalPermissionIds.value = new Set<number>();
}

function isCurrentlyAssigned(userId?: number) {
    return !!userId && assignedMembers.value.some(member => member.userId === userId);
}

function removeInactiveAssignedMember(userId?: number) {
    if (!userId) {
        return;
    }

    retainedInactiveAssignedIds.value = retainedInactiveAssignedIds.value.filter(id => id !== userId);
}

async function saveRoleMembers() {
    const selectedRole = selectedRoleDefinition.value?.role;
    const roleId = selectedRole?.roleId;

    if (!roleId) {
        return;
    }

    const currentAssignedIds = new Set(
        assignedMembers.value.map(member => member.userId).filter((userId): userId is number => !!userId),
    );
    const desiredAssignedIds = new Set([
        ...selectedMemberIds.value,
        ...retainedInactiveAssignedIds.value,
    ]);
    const userIdsToAdd = [...desiredAssignedIds].filter(userId => !currentAssignedIds.has(userId));
    const userIdsToRemove = [...currentAssignedIds].filter(userId => !desiredAssignedIds.has(userId));

    savingMembers.value = true;

    try {
        await Promise.all([
            ...userIdsToAdd.map(userId =>
                userRoleClient.value.addUserToRole(
                    UserRole.fromJS({
                        userId,
                        roleId,
                    }),
                ),
            ),
            ...userIdsToRemove.map(userId => userRoleClient.value.deleteRole(userId, roleId)),
        ]);

        await loadRoleMembers(roleId);

        toast.add({
            severity: 'success',
            summary: 'Role Updated',
            detail: `${selectedRoleDefinition.value?.label} membership was saved successfully.`,
            life: 3000,
        });

        closeManageDialog();
    } catch (error) {
        toast.add({
            severity: 'error',
            summary: 'Save Failed',
            detail: 'The selected role membership changes could not be saved.',
            life: 4000,
        });
        console.error(error);
    } finally {
        savingMembers.value = false;
    }
}

async function saveRolePermissions() {
    const selectedRole = selectedRoleDefinition.value?.role;
    const roleId = selectedRole?.roleId;

    if (!roleId) {
        return;
    }

    const desiredPermissionIds = new Set(selectedPermissionIds.value);
    const currentPermissionIds = new Set(
        assignedPermissionRecords.value.map(permission => permission.permissionId),
    );
    const permissionIdsToAdd = [...desiredPermissionIds].filter(permissionId => !currentPermissionIds.has(permissionId));
    const permissionIdsToRemove = [...currentPermissionIds].filter(permissionId => !desiredPermissionIds.has(permissionId));

    savingPermissions.value = true;

    try {
        await Promise.all([
            ...permissionIdsToAdd.map(permissionId =>
                apiStore.api.post('/v1/RolePermission', {
                    roleId,
                    permissionId,
                }),
            ),
            ...permissionIdsToRemove.map(permissionId =>
                apiStore.api.delete(`/v1/RolePermission/${roleId}/${permissionId}`),
            ),
        ]);

        await loadRolePermissions(roleId);

        toast.add({
            severity: 'success',
            summary: 'Permissions Updated',
            detail: `${selectedRoleDefinition.value?.label} permissions were saved successfully.`,
            life: 3000,
        });

        closePermissionDialog();
    } catch (error) {
        toast.add({
            severity: 'error',
            summary: 'Save Failed',
            detail: 'The selected role permission changes could not be saved.',
            life: 4000,
        });
        console.error(error);
    } finally {
        savingPermissions.value = false;
    }
}

function formatUserName(user: User) {
    const fullName = `${user.firstName || ''} ${user.lastName || ''}`.trim();
    if (fullName) {
        return fullName;
    }

    return user.email || `User ${user.userId}`;
}
</script>
