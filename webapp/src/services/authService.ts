/**
 * authService.ts — Local JWT authentication (no Azure AD / no MSAL)
 *
 * Flow:
 *   login()    → POST /api/auth/login → store token in localStorage
 *   getToken() → read token from localStorage
 *   logout()   → clear localStorage, redirect to /login
 */

import axios from 'axios';

const TOKEN_KEY = 'ste_auth_token';
const USER_KEY = 'ste_auth_user';

export interface AuthUser {
    username: string;
    companyCode: string;
    roles: string[];
}

interface LoginResponse {
    token: string;
    username: string;
    companyCode: string;
    roles: string[];
}

export async function login(username: string, password: string, companyCode: string): Promise<AuthUser> {
    const baseUrl = import.meta.env.VITE_APP_API_BASE_URL as string;
    const response = await axios.post<LoginResponse>(`${baseUrl}/api/auth/login`, {
        username,
        password,
        companyCode,
    });

    const { token, roles } = response.data;
    const user: AuthUser = { username, companyCode, roles };

    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(USER_KEY, JSON.stringify(user));

    return user;
}

export function logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
}

export function getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
}

export function getCurrentUser(): AuthUser | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;
    try {
        return JSON.parse(raw) as AuthUser;
    } catch {
        return null;
    }
}

export function isAuthenticated(): boolean {
    const token = getToken();
    if (!token) return false;

    try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        return payload.exp * 1000 > Date.now();
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
