import axios from 'axios';

const BYPASS = import.meta.env.VITE_BYPASS_AUTH === 'true';

const TOKEN_KEY = 'ste_auth_token';
const TEMP_TOKEN_KEY = 'ste_auth_temp_token';
const USER_KEY = 'ste_auth_user';
const COMPANIES_KEY = 'ste_auth_companies';

export interface AuthUser {
    username: string;
    companyCode: string;
    roles: string[];
}

export interface CompanyInfo {
    code: string;
    name: string;
    shortName: string;
    jobLetter: string | null;
    logoUrl: string | null;
}

interface Step1Response {
    tempToken: string;
    username: string;
    companies: CompanyInfo[];
}

interface Step2Response {
    token: string;
    username: string;
    companyCode: string;
    roles: string[];
}

const baseUrl = () => import.meta.env.VITE_APP_API_BASE_URL as string;

export async function loginStep1(username: string, password: string): Promise<{ username: string; companies: CompanyInfo[] }> {
    const response = await axios.post<Step1Response>(`${baseUrl()}/api/auth/login`, { username, password });
    const { tempToken, companies } = response.data;
    localStorage.setItem(TEMP_TOKEN_KEY, tempToken);
    localStorage.setItem(COMPANIES_KEY, JSON.stringify(companies));
    return { username: response.data.username, companies };
}

export async function selectCompany(tempToken: string, companyCode: string): Promise<AuthUser> {
    const response = await axios.post<Step2Response>(`${baseUrl()}/api/auth/select-company`, { tempToken, companyCode });
    const { token, username, roles } = response.data;
    const user: AuthUser = { username, companyCode, roles };
    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    localStorage.setItem('ste_last_company', companyCode);
    localStorage.removeItem(TEMP_TOKEN_KEY);
    return user;
}

export function logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(TEMP_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    localStorage.removeItem(COMPANIES_KEY);
}

export function getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
}

export function getTempToken(): string | null {
    return localStorage.getItem(TEMP_TOKEN_KEY);
}

export function getStoredCompanies(): CompanyInfo[] {
    const raw = localStorage.getItem(COMPANIES_KEY);
    if (!raw) return [];
    try { return JSON.parse(raw); } catch { return []; }
}

export function getCurrentUser(): AuthUser | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;
    try { return JSON.parse(raw) as AuthUser; } catch { return null; }
}

export const DEV_BYPASS = BYPASS;

export async function tryAutoLogin(): Promise<void> {
    if (!BYPASS) return;
    if (getToken()) return;
    try {
        const { companies } = await loginStep1('dev.user', 'StrongholdDev2024');
        const tempToken = getTempToken()!;
        const devCompany = localStorage.getItem('ste_dev_company') || 'CSL';
        const target = companies.find(c => c.code === devCompany) ?? companies[0];
        if (target) {
            await selectCompany(tempToken, target.code);
        }
    } catch {
        localStorage.setItem(TOKEN_KEY, 'bypass-pending');
    }
}

export function isAuthenticated(): boolean {
    if (BYPASS) return true;
    const token = getToken();
    if (!token) return false;
    try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        if (payload.exp * 1000 <= Date.now()) return false;
        return !!payload.company_code;
    } catch {
        return false;
    }
}

export function hasPendingCompanySelection(): boolean {
    const temp = getTempToken();
    if (!temp) return false;
    try {
        const payload = JSON.parse(atob(temp.split('.')[1]));
        return payload.exp * 1000 > Date.now() && payload.auth_step === 'company_select';
    } catch {
        return false;
    }
}

export function isTokenExpiringSoon(): boolean {
    const token = getToken();
    if (!token) return false;
    try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        const fiveMinutes = 5 * 60 * 1000;
        return payload.exp * 1000 - Date.now() < fiveMinutes;
    } catch {
        return false;
    }
}
