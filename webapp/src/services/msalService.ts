import {
    AccountInfo,
    Configuration,
    InteractionRequiredAuthError,
    LogLevel,
    PublicClientApplication,
    ServerError,
} from '@azure/msal-browser';

const tenantId = import.meta.env.VITE_TENANT_ID;
const appBaseUrl = import.meta.env.VITE_APP_BASE_URL;
const webClientId = import.meta.env.VITE_WEB_CLIENT_ID;
const apiClientId = import.meta.env.VITE_API_CLIENT_ID;
const useLocalAuth = import.meta.env.VITE_USE_LOCAL_AUTH === 'true';
const localAzureAdObjectId = '00000000-0000-0000-0000-000000000000';

const scopes = [`api://${apiClientId}/default`, 'openid', 'profile', 'User.Read'];
const msalConfig: Configuration = {
    auth: {
        clientId: webClientId,
        postLogoutRedirectUri: appBaseUrl,
        authority: `https://login.microsoftonline.com/${tenantId}`,
        redirectUri: `${appBaseUrl}/authentication/login-callback`,
    },
    cache: { cacheLocation: 'localStorage', storeAuthStateInCookie: true, secureCookies: true },
    system: { loggerOptions: { logLevel: LogLevel.Info, piiLoggingEnabled: false } },
};

export const msalInstance = new PublicClientApplication(msalConfig);
export const isLocalAuthEnabled = useLocalAuth;
export const localAccount = {
    homeAccountId: 'local-home-account',
    environment: 'local',
    tenantId: tenantId || 'local',
    username: 'local.developer@localhost',
    localAccountId: localAzureAdObjectId,
    name: 'Local Dev Testing',
    idTokenClaims: {
        oid: localAzureAdObjectId,
        name: 'Local Dev Testing',
        preferred_username: 'local.developer@localhost',
    },
} as AccountInfo;

export async function logout() {
    if (useLocalAuth) {
        return;
    }

    await msalInstance.logoutRedirect();
}

export async function login() {
    if (useLocalAuth) {
        return;
    }

    await msalInstance.loginRedirect({ scopes });
}

export async function handleRedirectResponse() {
    if (useLocalAuth) {
        return localAccount;
    }

    const currentAccounts = msalInstance.getAllAccounts();
    const response = await msalInstance.handleRedirectPromise();

    if (response && response.account) {
        return response.account;
    }

    if (currentAccounts.length === 1) {
        return currentAccounts[0];
    }

    return null;
}

export async function getToken(account: AccountInfo | null) {
    if (useLocalAuth) {
        return '';
    }

    if (!account) {
        await msalInstance.loginRedirect({ scopes });
        return null;
    }

    try {
        const response = await msalInstance.acquireTokenSilent({ account, scopes });
        return response.accessToken;
    } catch (error) {
        const expiredTokenError = 'AADSTS700084';

        if (error instanceof InteractionRequiredAuthError) {
            await msalInstance.acquireTokenRedirect({ account, scopes });
            return null;
        }

        if (error instanceof ServerError && error.message.includes(expiredTokenError)) {
            await msalInstance.loginRedirect({ scopes });
            return null;
        }

        throw error;
    }
}
