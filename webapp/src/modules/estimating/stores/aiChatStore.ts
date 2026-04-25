import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import { useApiStore } from '@/stores/apiStore';
import { useEstimateStore } from './estimateStore';
import { useStaffingStore } from './staffingStore';

// ── Message types ─────────────────────────────────────────────────────────────

export interface ChatMessage {
    role: 'user' | 'assistant';
    content: string;
    timestamp: Date;
    actions?: AiAction[];
    applied?: boolean;
    error?: boolean;
    savePrompt?: boolean;
    pendingTemplate?: { templateId: number; templateName: string };
}

// ── Action types ──────────────────────────────────────────────────────────────

export type AiAction =
    | { action: 'fill_header'; fields: Record<string, string | number | boolean> }
    | { action: 'add_labor_rows'; rows: Array<{ position: string; shift: string; qty: number }> }
    | { action: 'set_dates'; start_date?: string; end_date?: string; days?: number }
    | { action: 'load_rate_book'; rate_book_id: number; rate_book_name: string }
    | { action: 'suggest_rate_book'; nearest_rate_book_id: number; nearest_rate_book_name: string; similarity_reason: string; clone_suggested_name?: string }
    | { action: 'apply_template'; template_id: number; template_name: string }
    | { action: 'ask_clarification'; question: string; options?: string[] }
    | { action: 'navigate'; route: string }
    | { action: 'rate_anomaly_warning'; position: string; current_rate: number; benchmark_rate: number; deviation_pct: number; direction: 'above' | 'below' }
    | { action: 'update_estimate_status'; estimate_id: number; new_status: string; lost_reason?: string }
    | { action: 'app_control'; command: 'switch_theme'; theme?: 'light' | 'dark' }
    | { action: 'app_control'; command: 'navigate_to'; route: string }
    | { action: 'app_control'; command: 'open_new_estimate' }
    | { action: 'app_control'; command: 'open_new_staffing_plan' };

// ── Context snapshot ──────────────────────────────────────────────────────────

export interface AiLaborRow {
    position: string;
    shift: string;
    stRate: number;
    otRate: number;
    dtRate: number;
    stHours: number;
    otHours: number;
    subtotal: number;
}

export interface AiContext {
    companyCode: string;
    currentPage: string;
    currentEstimateId?: number;
    headerSnapshot?: {
        name?: string; client?: string; city?: string; state?: string;
        startDate?: string; endDate?: string; days?: number; shift?: string;
        jobType?: string; jobLetter?: string; branch?: string;
    };
    rateBookName?: string;
    currentRateBookId?: number;
    availableRateBooks?: Array<{ rateBookId: number; name: string; client?: string; city?: string; state?: string; laborCount: number }>;
    laborRows?: AiLaborRow[];
}

// ── Store ─────────────────────────────────────────────────────────────────────

export const useAiChatStore = defineStore('aiChat', () => {
    const apiStore = useApiStore();

    const messages = ref<ChatMessage[]>([]);
    const isLoading = ref(false);
    const isOpen = ref(false);
    const pendingMessageIdx = ref<number | null>(null);
    const rateBookCache = ref<AiContext['availableRateBooks']>([]);
    const appControlQueue = ref<AiAction[]>([]);

    function drainAppControlQueue(): AiAction[] {
        const items = [...appControlQueue.value];
        appControlQueue.value = [];
        return items;
    }

    const hasMessages = computed(() => messages.value.length > 0);

    function open()   { isOpen.value = true; }
    function close()  { isOpen.value = false; }
    function toggle() { isOpen.value = !isOpen.value; }

    function clearHistory() {
        messages.value = [];
        pendingMessageIdx.value = null;
    }

    async function loadRateBookCache() {
        if (rateBookCache.value?.length) return;
        try {
            const { data } = await apiStore.api.get('/api/v1/rate-books');
            rateBookCache.value = (data as any[]).map(rb => ({
                rateBookId: rb.rateBookId,
                name: rb.name,
                client: rb.client,
                city: rb.city,
                state: rb.state,
                laborCount: rb.laborCount ?? 0,
            }));
        } catch { /* cache stays empty */ }
    }

    // ── Send a user message ───────────────────────────────────────────────────

    async function sendMessage(text: string, context: AiContext) {
        if (!text.trim()) return;

        await loadRateBookCache();
        const fullContext: AiContext = { ...context, availableRateBooks: rateBookCache.value };

        messages.value.push({ role: 'user', content: text, timestamp: new Date() });
        isLoading.value = true;

        try {
            const history = messages.value
                .filter(m => m.applied !== false)
                .slice(-20)
                .map(m => ({ role: m.role, content: m.content }));

            const { data } = await apiStore.api.post('/api/v1/ai/chat', {
                message: text,
                history,
                context: fullContext,
            });

            const response: string = data.response ?? '';
            const rawActions: any[] = data.actions ?? [];
            const actions = rawActions.filter(a => isValidAction(a)) as AiAction[];

            // Determine if any actions need confirmation before applying
            const needsConfirmation = actions.some(a =>
                a.action !== 'ask_clarification' &&
                a.action !== 'navigate' &&
                a.action !== 'suggest_rate_book' &&
                a.action !== 'rate_anomaly_warning' &&
                a.action !== 'app_control'
            );
            const hasSuggestion = actions.some(a => a.action === 'suggest_rate_book');

            const msg: ChatMessage = {
                role: 'assistant',
                content: response,
                timestamp: new Date(),
                actions: actions.length > 0 ? actions : undefined,
                applied: needsConfirmation || hasSuggestion ? false : true,
            };

            messages.value.push(msg);

            if (!needsConfirmation && !hasSuggestion && actions.length > 0) {
                applyActionsFromMessage(messages.value.length - 1);
            } else if (needsConfirmation || hasSuggestion) {
                pendingMessageIdx.value = messages.value.length - 1;
            }
        } catch (err: any) {
            const detail = err?.response?.data?.message ?? 'AI service error. Check Groq API key.';
            messages.value.push({
                role: 'assistant',
                content: `Error: ${detail}`,
                timestamp: new Date(),
                error: true,
                applied: true,
            });
        } finally {
            isLoading.value = false;
        }
    }

    // ── Apply confirmed actions ───────────────────────────────────────────────

    async function applyActionsFromMessage(msgIdx: number) {
        const msg = messages.value[msgIdx];
        if (!msg?.actions?.length || msg.applied) return;

        const estimateStore = useEstimateStore();
        const staffingStore = useStaffingStore();

        for (const action of msg.actions) {
            if (action.action === 'suggest_rate_book') continue; // handled by sidebar buttons
            await dispatchAction(action, estimateStore, staffingStore);
        }

        msg.applied = true;
        pendingMessageIdx.value = null;
    }

    function dismissActions(msgIdx: number) {
        const msg = messages.value[msgIdx];
        if (msg) { msg.applied = true; msg.actions = undefined; }
        pendingMessageIdx.value = null;
    }

    // ── Rate book suggestion handlers ─────────────────────────────────────────

    async function applyRateBookSuggestion(msgIdx: number, rateBookId: number, rateBookName: string) {
        const estimateStore = useEstimateStore();
        try {
            await estimateStore.loadRateBook(rateBookId);
            messages.value.push({
                role: 'assistant',
                content: `Loaded "${rateBookName}". Rates are now active.`,
                timestamp: new Date(),
                applied: true,
            });
        } catch {
            messages.value.push({
                role: 'assistant',
                content: 'Failed to load that rate book. Try loading it manually from the header.',
                timestamp: new Date(),
                error: true,
                applied: true,
            });
        }
        const msg = messages.value[msgIdx];
        if (msg) msg.applied = true;
        pendingMessageIdx.value = null;
    }

    async function cloneRateBook(msgIdx: number, sourceId: number, newName: string) {
        try {
            const { data } = await apiStore.api.post(`/api/v1/rate-books/${sourceId}/clone`, { name: newName });
            rateBookCache.value = []; // invalidate cache
            const estimateStore = useEstimateStore();
            await estimateStore.loadRateBook(data.rateBookId);
            messages.value.push({
                role: 'assistant',
                content: `Created and loaded "${newName}". You can edit it in the Rate Library.`,
                timestamp: new Date(),
                applied: true,
            });
        } catch {
            messages.value.push({
                role: 'assistant',
                content: 'Clone failed. Please create the rate book manually in the Rate Library.',
                timestamp: new Date(),
                error: true,
                applied: true,
            });
        }
        const msg = messages.value[msgIdx];
        if (msg) msg.applied = true;
        pendingMessageIdx.value = null;
    }

    // ── Template application ──────────────────────────────────────────────────

    async function applyTemplate(msgIdx: number, templateId: number, templateName: string) {
        const estimateStore = useEstimateStore();
        const estimateId = estimateStore.header.estimateId;
        if (!estimateId) {
            messages.value.push({
                role: 'assistant',
                content: `Save the estimate first, then I can apply "${templateName}".`,
                timestamp: new Date(),
                error: true,
                applied: true,
                savePrompt: true,
                pendingTemplate: { templateId, templateName },
            });
            return;
        }
        try {
            const rateBookId = estimateStore.header.rateBookId;
            const { data } = await apiStore.api.post(`/api/v1/crew-templates/${templateId}/apply`, {
                estimateId,
                rateBookId: rateBookId || null,
            });
            await estimateStore.fetchEstimate(estimateId);
            messages.value.push({
                role: 'assistant',
                content: `Applied "${templateName}" — added ${data.addedCount ?? 0} labor row(s)${rateBookId ? ' with active rate book rates' : ''}.`,
                timestamp: new Date(),
                applied: true,
            });
        } catch {
            messages.value.push({
                role: 'assistant',
                content: 'Template apply failed. Try applying it manually from Library → Templates.',
                timestamp: new Date(),
                error: true,
                applied: true,
            });
        }
        const msg = messages.value[msgIdx];
        if (msg) msg.applied = true;
        pendingMessageIdx.value = null;
    }

    // ── Action dispatcher ─────────────────────────────────────────────────────

    async function dispatchAction(
        action: AiAction,
        estimateStore: ReturnType<typeof useEstimateStore>,
        staffingStore: ReturnType<typeof useStaffingStore>
    ) {
        switch (action.action) {
            case 'fill_header': {
                const f = action.fields;
                const h = estimateStore.header;
                if (f.name !== undefined)          h.name = String(f.name);
                if (f.client !== undefined)        h.client = String(f.client);
                if (f.city !== undefined)          h.city = String(f.city);
                if (f.state !== undefined)         h.state = String(f.state);
                if (f.shift !== undefined)         h.shift = String(f.shift);
                if (f.status !== undefined)        h.status = String(f.status);
                if (f.hoursPerShift !== undefined) h.hoursPerShift = Number(f.hoursPerShift);
                if (f.days !== undefined)          h.days = Number(f.days);
                if (f.otMethod !== undefined)      h.otMethod = String(f.otMethod);
                if (f.confidencePct !== undefined) h.confidencePct = Number(f.confidencePct);
                if (f.site !== undefined)          h.site = String(f.site);
                if (f.branch !== undefined)        h.branch = String(f.branch);
                if (f.msaNumber !== undefined)     h.msaNumber = String(f.msaNumber);
                if (f.jobType !== undefined)       h.jobType = String(f.jobType);
                if (f.jobLetter !== undefined)     h.jobLetter = String(f.jobLetter);
                if (f.clientCode !== undefined)    h.clientCode = String(f.clientCode);
                if (f.lostReason !== undefined)    h.lostReason = String(f.lostReason);
                if (f.vp !== undefined)            h.vp = String(f.vp);
                if (f.director !== undefined)      h.director = String(f.director);
                if (f.region !== undefined)        h.region = String(f.region);
                if (f.startDate !== undefined)     h.startDate = String(f.startDate);
                if (f.endDate !== undefined)       h.endDate = String(f.endDate);
                // Also sync to staffing store if on staffing page
                if (staffingStore.isDirty || staffingStore.header.staffingPlanId) {
                    const sh = staffingStore.header;
                    if (f.name !== undefined)    sh.name = String(f.name);
                    if (f.client !== undefined)  sh.client = String(f.client);
                    if (f.city !== undefined)    sh.city = String(f.city);
                    if (f.state !== undefined)   sh.state = String(f.state);
                    if (f.shift !== undefined)   sh.shift = String(f.shift);
                    if (f.branch !== undefined)  sh.branch = String(f.branch);
                    staffingStore.markDirty();
                }
                estimateStore.markDirty();
                break;
            }
            case 'set_dates': {
                const h = estimateStore.header;
                if (action.start_date) h.startDate = action.start_date;
                if (action.end_date)   h.endDate = action.end_date;
                if (action.days !== undefined) h.days = action.days;
                if (staffingStore.isDirty || staffingStore.header.staffingPlanId) {
                    const sh = staffingStore.header;
                    if (action.start_date) sh.startDate = action.start_date;
                    if (action.end_date)   sh.endDate = action.end_date;
                    staffingStore.markDirty();
                }
                estimateStore.markDirty();
                break;
            }
            case 'add_labor_rows': {
                const isStaffing = !!staffingStore.header.staffingPlanId || staffingStore.isDirty;
                for (const row of action.rows) {
                    if (isStaffing) {
                        staffingStore.addLaborRowFromAi({ position: row.position, shift: row.shift, qty: row.qty });
                    } else {
                        estimateStore.addLaborRowFromAi({ position: row.position, shift: row.shift, qty: row.qty });
                    }
                }
                if (!isStaffing) estimateStore.recalcSummary();
                break;
            }
            case 'load_rate_book': {
                try { await estimateStore.loadRateBook(action.rate_book_id); } catch { /* ignore */ }
                break;
            }
            case 'apply_template': {
                await applyTemplate(-1, action.template_id, action.template_name);
                break;
            }
            case 'update_estimate_status': {
                const apiStore = useApiStore();
                await apiStore.api.patch(
                    `/api/v1/estimates/${action.estimate_id}/status`,
                    { status: action.new_status, lostReason: action.lost_reason ?? null }
                );
                break;
            }
            case 'app_control':
                appControlQueue.value.push(action);
                break;
            case 'navigate':
            case 'ask_clarification':
            case 'suggest_rate_book':
            case 'rate_anomaly_warning':
                break; // informational only — handled in template
        }
    }

    // ── Describe actions for preview card ─────────────────────────────────────

    function describeActions(actions: AiAction[]): string[] {
        return actions
            .filter(a => a.action !== 'ask_clarification' && a.action !== 'suggest_rate_book' && a.action !== 'rate_anomaly_warning')
            .map(a => {
                switch (a.action) {
                    case 'fill_header':
                        return `Set header — ${Object.entries(a.fields).map(([k, v]) => `${k}: ${v}`).join(', ')}`;
                    case 'set_dates':
                        return `Set dates: ${a.start_date ?? '?'} → ${a.end_date ?? '?'} (${a.days ?? '?'} days)`;
                    case 'add_labor_rows':
                        return a.rows.map(r => `Add ${r.qty}× ${r.position} (${r.shift})`).join(', ');
                    case 'load_rate_book':
                        return `Load rate book: ${a.rate_book_name}`;
                    case 'apply_template':
                        return `Apply template: ${a.template_name}`;
                    case 'navigate':
                        return `Navigate to ${a.route}`;
                    default: return '';
                }
            })
            .filter(Boolean);
    }

    // ── RFQ document upload ───────────────────────────────────────────────────

    async function uploadRfq(file: File) {
        messages.value.push({ role: 'user', content: `📄 Parsing RFQ: ${file.name}`, timestamp: new Date() });
        isLoading.value = true;
        try {
            const form = new FormData();
            form.append('file', file);
            const { data } = await apiStore.api.post('/api/v1/ai/parse-rfq', form, {
                headers: { 'Content-Type': 'multipart/form-data' },
            });
            const response: string = data.response ?? '';
            const rawActions: any[] = data.actions ?? [];
            const actions = rawActions.filter(a => isValidAction(a)) as AiAction[];
            const needsConfirmation = actions.some(a =>
                a.action !== 'ask_clarification' &&
                a.action !== 'navigate' &&
                a.action !== 'suggest_rate_book' &&
                a.action !== 'rate_anomaly_warning' &&
                a.action !== 'app_control'
            );
            const msg: ChatMessage = { role: 'assistant', content: response, timestamp: new Date(), actions: actions.length > 0 ? actions : undefined, applied: needsConfirmation ? false : true };
            messages.value.push(msg);
            if (!needsConfirmation && actions.length > 0) {
                applyActionsFromMessage(messages.value.length - 1);
            } else if (needsConfirmation) {
                pendingMessageIdx.value = messages.value.length - 1;
            }
        } catch (err: any) {
            messages.value.push({ role: 'assistant', content: `Error parsing document: ${err?.response?.data?.message ?? 'Unknown error'}`, timestamp: new Date(), error: true, applied: true });
        } finally {
            isLoading.value = false;
        }
    }

    return {
        messages, isLoading, isOpen, pendingMessageIdx, hasMessages, rateBookCache,
        appControlQueue, drainAppControlQueue,
        open, close, toggle, clearHistory,
        sendMessage, uploadRfq, applyActionsFromMessage, dismissActions,
        applyRateBookSuggestion, cloneRateBook, applyTemplate,
        describeActions,
    };
});

// ── Validation ────────────────────────────────────────────────────────────────

function isValidAction(a: any): boolean {
    if (!a || typeof a !== 'object' || typeof a.action !== 'string') return false;
    const valid = ['fill_header', 'add_labor_rows', 'set_dates', 'load_rate_book',
        'suggest_rate_book', 'apply_template', 'ask_clarification', 'navigate',
        'rate_anomaly_warning', 'update_estimate_status', 'app_control'];
    return valid.includes(a.action);
}
