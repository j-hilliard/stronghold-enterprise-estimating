import { computed, ref } from 'vue';
import { defineStore } from 'pinia';
import { blobToBase64 } from '@/utils.ts';
import { useApiStore } from '@/stores/apiStore';
import { AccountInfo } from '@azure/msal-browser';
import { Role, User, UserClient, UserRole } from '@/apiclient/client';
import { getUserProfilePhoto } from '@/services/graphService';
import { isLocalAuthEnabled, logout, msalInstance } from '@/services/msalService';

export const useUserStore = defineStore('user', () => {
    const user = ref<User | null>(null);
    const userPhoto = ref<string | null>(null);
    const userAccountInfo = ref<AccountInfo | null>(null);

    const apiStore = useApiStore();

    const userTitle = computed(() => {
        return user.value?.title || '';
    });

    const isAuthenticated = computed(() => {
        return !!user.value;
    });

    const apiClient = computed(() => {
        return new UserClient(apiStore.api.defaults.baseURL, apiStore.api);
    });

    const userFullName = computed(() => {
        return `${user.value?.firstName || ''} ${user.value?.lastName || ''}`;
    });

    const isAdmin = computed(() => {
        return !!user.value?.roles?.map(role => role.role?.name).includes('Administrator');
    });

    async function logoutUser() {
        await logout();
        await setUser(null);
    }

    async function setUser(accountInfo: AccountInfo | null) {
        userAccountInfo.value = accountInfo;

        await getUserPhoto(accountInfo);
        await getUser(accountInfo?.idTokenClaims?.oid || '');
    }

    async function getUser(azureAdObjectId: string) {
        if (!azureAdObjectId && isLocalAuthEnabled) {
            user.value = getLocalDevUser();
            return;
        }

        if (!azureAdObjectId) {
            return;
        }

        user.value = await apiClient.value.getUserByAzureAdObjectId(azureAdObjectId).catch(() => {
            if (isLocalAuthEnabled) {
                return getLocalDevUser();
            }

            return null;
        });
    }

    function setMockUser() {
        user.value = {
            firstName: 'Joseph',
            lastName: 'Hilliard',
            email: 'joseph.hilliard@quantaservices.com',
            roles: [{ role: { name: 'Administrator' } }],
        } as User;
    }

    async function getUserPhoto(account: AccountInfo | null) {
        if (!account || isLocalAuthEnabled) {
            userPhoto.value = null;
            return;
        }

        try {
            const tokenResponse = await msalInstance.acquireTokenSilent({
                account,
                scopes: ['User.Read'],
            });
            const accessToken = tokenResponse?.accessToken || '';
            const response = await getUserProfilePhoto(accessToken);

            userPhoto.value = (await blobToBase64(response))?.toString() || null;
        } catch {
            userPhoto.value = null;
        }
    }

    return {
        user,
        isAdmin,
        userPhoto,
        apiClient,
        userTitle,
        userFullName,
        isAuthenticated,
        userAccountInfo,
        setUser,
        setMockUser,
        logoutUser,
    };
});

function getLocalDevUser() {
    return User.fromJS({
        userId: 0,
        azureAdObjectId: '00000000-0000-0000-0000-000000000000',
        firstName: 'Local',
        lastName: 'Developer',
        email: 'local.developer@localhost',
        company: 'Stronghold',
        department: 'Safety',
        title: 'Local Dev Testing',
        active: true,
        roles: [
            UserRole.fromJS({
                userId: 0,
                roleId: 1,
                role: Role.fromJS({
                    roleId: 1,
                    name: 'Administrator',
                    description: 'Local development administrator',
                }),
            }),
        ],
    });
}
