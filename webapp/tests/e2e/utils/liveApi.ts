/**
 * Live API helpers for real E2E tests that talk directly to the running API+DB.
 * REQ-GOV-012: All local testing uses isolated Docker SQL only.
 * REQ-GOV-003: All test files trace to requirements.
 */

import type { APIRequestContext } from '@playwright/test';
import { API_BASE_URL, DEV_CREDENTIALS } from './constants';

export { API_BASE_URL };

export async function getAuthToken(request: APIRequestContext): Promise<string> {
    const loginResp = await request.post(`${API_BASE_URL}/api/auth/login`, {
        data: { username: DEV_CREDENTIALS.username, password: DEV_CREDENTIALS.password },
        ignoreHTTPSErrors: true,
    });
    if (!loginResp.ok()) {
        throw new Error(`Login failed: ${loginResp.status()} — ${await loginResp.text()}`);
    }
    const { tempToken } = await loginResp.json();

    const selectResp = await request.post(`${API_BASE_URL}/api/auth/select-company`, {
        data: { tempToken, companyCode: DEV_CREDENTIALS.companyCode },
        ignoreHTTPSErrors: true,
    });
    if (!selectResp.ok()) {
        throw new Error(`select-company failed: ${selectResp.status()} — ${await selectResp.text()}`);
    }
    const { token } = await selectResp.json();
    return token as string;
}

async function authedHeaders(request: APIRequestContext) {
    const token = await getAuthToken(request);
    return { Authorization: `Bearer ${token}` };
}

export async function apiGet<T = any>(request: APIRequestContext, path: string): Promise<T> {
    const resp = await request.get(`${API_BASE_URL}${path}`, {
        headers: await authedHeaders(request),
        ignoreHTTPSErrors: true,
    });
    if (!resp.ok()) throw new Error(`GET ${path} → ${resp.status()}: ${await resp.text()}`);
    return resp.json() as Promise<T>;
}

export async function apiPost<T = any>(
    request: APIRequestContext,
    path: string,
    body: unknown,
): Promise<T> {
    const resp = await request.post(`${API_BASE_URL}${path}`, {
        data: body,
        headers: await authedHeaders(request),
        ignoreHTTPSErrors: true,
    });
    if (!resp.ok()) throw new Error(`POST ${path} → ${resp.status()}: ${await resp.text()}`);
    const text = await resp.text();
    return (text ? JSON.parse(text) : null) as T;
}

export async function apiPut(
    request: APIRequestContext,
    path: string,
    body: unknown,
): Promise<void> {
    const resp = await request.put(`${API_BASE_URL}${path}`, {
        data: body,
        headers: await authedHeaders(request),
        ignoreHTTPSErrors: true,
    });
    if (!resp.ok()) throw new Error(`PUT ${path} → ${resp.status()}: ${await resp.text()}`);
}

export async function apiDelete(request: APIRequestContext, path: string): Promise<void> {
    const resp = await request.delete(`${API_BASE_URL}${path}`, {
        headers: await authedHeaders(request),
        ignoreHTTPSErrors: true,
    });
    if (!resp.ok()) throw new Error(`DELETE ${path} → ${resp.status()}: ${await resp.text()}`);
}

/** Seed dev data (rate books, cost book, estimates). 409 = already seeded — treated as OK. */
export async function seedDevData(request: APIRequestContext): Promise<void> {
    const resp = await request.post(`${API_BASE_URL}/api/v1/dev/seed`, {
        headers: await authedHeaders(request),
        ignoreHTTPSErrors: true,
    });
    if (!resp.ok() && resp.status() !== 409) {
        throw new Error(`dev/seed failed: ${resp.status()}: ${await resp.text()}`);
    }
}

/** Reset all seeded dev data. Wipes estimates, rate books, staffing plans. */
export async function resetDevData(request: APIRequestContext): Promise<void> {
    const resp = await request.post(`${API_BASE_URL}/api/v1/dev/reset`, {
        headers: await authedHeaders(request),
        ignoreHTTPSErrors: true,
    });
    if (!resp.ok()) throw new Error(`dev/reset failed: ${resp.status()}: ${await resp.text()}`);
}

/** Seed cost book with standard burden rates and positions. */
export async function seedCostBook(request: APIRequestContext): Promise<void> {
    const resp = await request.post(`${API_BASE_URL}/api/v1/cost-books/reset-standard`, {
        headers: await authedHeaders(request),
        ignoreHTTPSErrors: true,
    });
    if (!resp.ok()) throw new Error(`cost-book reset-standard failed: ${resp.status()}: ${await resp.text()}`);
}
