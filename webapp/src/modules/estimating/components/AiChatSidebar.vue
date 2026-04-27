<template>
    <Teleport to="body">
        <!-- Floating chat window -->
        <Transition name="ai-pop">
            <div
                v-if="store.isOpen"
                class="ai-window"
                :style="windowStyle"
            >
                <!-- Header (drag handle) -->
                <div class="ai-header ai-drag-handle" @pointerdown="onWindowHeaderPointerDown">
                    <div class="ai-header-left">
                        <div class="ai-avatar">
                            <svg viewBox="0 0 24 24" fill="none" width="18" height="18">
                                <path d="M12 3l1.8 7.2L21 12l-7.2 1.8L12 21l-1.8-7.2L3 12l7.2-1.8Z" fill="white" opacity="0.95"/>
                                <path d="M20 2l.7 2.8L23.5 5.5l-2.8.7L20 9l-.7-2.8L16.5 5.5l2.8-.7Z" fill="white" opacity="0.7"/>
                                <path d="M4 17l.5 2L6.5 19.5l-2 .5L4 22l-.5-2L1.5 19.5l2-.5Z" fill="white" opacity="0.7"/>
                            </svg>
                        </div>
                        <div>
                            <div class="ai-title">Stronghold AI</div>
                            <div class="ai-subtitle">Your estimating assistant</div>
                        </div>
                    </div>
                    <div class="flex gap-1">
                        <Button
                            v-if="store.hasMessages"
                            icon="pi pi-trash"
                            text size="small" severity="secondary"
                            title="Clear chat"
                            @click="store.clearHistory()"
                        />
                        <Button icon="pi pi-times" text size="small" severity="secondary" @click="store.close()" />
                    </div>
                </div>

                <!-- API key warning -->
                <div v-if="noApiKey" class="ai-warning">
                    <i class="pi pi-exclamation-triangle" />
                    <span>AI not configured. Check appsettings.Local.json for Ai:Provider settings.</span>
                </div>

                <!-- Message thread -->
                <div ref="threadEl" class="ai-thread">
                    <!-- Welcome state -->
                    <div v-if="!store.hasMessages" class="ai-welcome">
                        <div class="ai-welcome-icon">
                            <svg viewBox="0 0 48 48" fill="none" width="38" height="38">
                                <circle cx="24" cy="24" r="22" fill="url(#aiGrad2)" opacity="0.12"/>
                                <path d="M24 10l2.5 9.5L36 22l-9.5 2.5L24 34l-2.5-9.5L12 22l9.5-2.5Z" fill="var(--primary-color)" opacity="0.9"/>
                                <path d="M38 8l1.2 4.8L44 14l-4.8 1.2L38 20l-1.2-4.8L32 14l4.8-1.2Z" fill="var(--primary-color)" opacity="0.6"/>
                                <path d="M10 34l.8 3.2L14 38.5l-3.2.8L10 43l-.8-3.2L6 38.5l3.2-.8Z" fill="var(--primary-color)" opacity="0.6"/>
                                <defs>
                                    <linearGradient id="aiGrad2" x1="0" y1="0" x2="1" y2="1">
                                        <stop offset="0%" stop-color="var(--primary-color)"/>
                                        <stop offset="100%" stop-color="#9c5cff"/>
                                    </linearGradient>
                                </defs>
                            </svg>
                        </div>
                        <div class="ai-welcome-title">What can I help with?</div>
                        <div class="ai-welcome-hints">
                            <div v-for="hint in hints" :key="hint" class="ai-hint" @click="sendHint(hint)">{{ hint }}</div>
                        </div>
                    </div>

                    <!-- Messages -->
                    <template v-for="(msg, idx) in store.messages" :key="idx">
                        <div v-if="msg.role === 'user'" class="ai-msg ai-msg--user">
                            <div class="ai-bubble ai-bubble--user">{{ msg.content }}</div>
                        </div>

                        <div v-else class="ai-msg ai-msg--assistant">
                            <div class="ai-avatar-sm">
                                <svg viewBox="0 0 20 20" fill="none" width="11" height="11">
                                    <path d="M10 2l1.5 5.5L17 9l-5.5 1.5L10 16l-1.5-5.5L3 9l5.5-1.5Z" fill="white" opacity="0.95"/>
                                </svg>
                            </div>
                            <div class="ai-msg-body">
                                <div class="ai-bubble ai-bubble--assistant" :class="{ 'ai-bubble--error': msg.error }">
                                    {{ msg.content }}
                                </div>

                                <!-- Save & Apply prompt when template was requested before save -->
                                <div v-if="msg.savePrompt && msg.pendingTemplate" class="ai-save-prompt">
                                    <Button
                                        label="Save & Apply Template"
                                        icon="pi pi-save"
                                        size="small"
                                        :loading="savingForTemplate"
                                        @click="saveAndApplyTemplate(msg.pendingTemplate!.templateId, msg.pendingTemplate!.templateName, idx)"
                                    />
                                </div>

                                <!-- Action preview card -->
                                <div v-if="msg.actions?.length && !msg.applied && !msg.error" class="ai-action-card">
                                    <div class="ai-action-header">
                                        <i class="pi pi-bolt" style="color: var(--primary-color)" />
                                        <span>Pending changes</span>
                                    </div>

                                    <ul v-if="store.describeActions(msg.actions).length" class="ai-action-list">
                                        <li v-for="(desc, di) in store.describeActions(msg.actions)" :key="di">{{ desc }}</li>
                                    </ul>

                                    <!-- Rate book suggestion -->
                                    <template v-for="a in msg.actions" :key="a.action + (a as any).nearest_rate_book_id">
                                        <div v-if="a.action === 'suggest_rate_book'" class="ai-suggest-card">
                                            <div class="ai-suggest-icon"><i class="pi pi-book" /></div>
                                            <div class="ai-suggest-body">
                                                <div class="ai-suggest-title">Rate Book Suggestion</div>
                                                <div class="ai-suggest-reason">{{ (a as any).similarity_reason }}</div>
                                                <div class="ai-suggest-btns">
                                                    <Button
                                                        :label="`Use ${(a as any).nearest_rate_book_name}`"
                                                        size="small" icon="pi pi-check"
                                                        @click="store.applyRateBookSuggestion(idx, (a as any).nearest_rate_book_id, (a as any).nearest_rate_book_name)"
                                                    />
                                                    <Button
                                                        v-if="(a as any).clone_suggested_name"
                                                        :label="`Clone as '${(a as any).clone_suggested_name}'`"
                                                        size="small" outlined icon="pi pi-copy"
                                                        @click="store.cloneRateBook(idx, (a as any).nearest_rate_book_id, (a as any).clone_suggested_name)"
                                                    />
                                                </div>
                                            </div>
                                        </div>
                                    </template>

                                    <!-- Rate anomaly warnings -->
                                    <template v-for="a in msg.actions" :key="'anomaly-' + (a as any).position">
                                        <div v-if="a.action === 'rate_anomaly_warning'" class="ai-anomaly-card">
                                            <i class="pi pi-exclamation-triangle" style="color: var(--yellow-500)" />
                                            <div class="ai-anomaly-body">
                                                <span class="ai-anomaly-title">Rate Warning — {{ (a as any).position }}</span>
                                                <span class="ai-anomaly-detail">
                                                    ST ${{ (a as any).current_rate?.toFixed(2) }} is
                                                    <strong>{{ Math.abs((a as any).deviation_pct ?? 0).toFixed(0) }}% {{ (a as any).direction }}</strong>
                                                    historical avg ${{ (a as any).benchmark_rate?.toFixed(2) }}
                                                </span>
                                            </div>
                                        </div>
                                    </template>

                                    <!-- Template suggestion -->
                                    <template v-for="a in msg.actions" :key="'tpl-' + (a as any).template_id">
                                        <div v-if="a.action === 'apply_template'" class="ai-template-card">
                                            <i class="pi pi-users" />
                                            <span>Apply template: <strong>{{ (a as any).template_name }}</strong></span>
                                        </div>
                                    </template>

                                    <!-- Clarification options -->
                                    <template v-for="a in msg.actions" :key="'clr-' + (a as any).question">
                                        <div v-if="a.action === 'ask_clarification' && (a as any).question" class="ai-clarification">
                                            <div class="ai-clarification-q">{{ (a as any).question }}</div>
                                            <div v-if="(a as any).options?.length" class="ai-clarification-opts">
                                                <Button
                                                    v-for="opt in (a as any).options" :key="opt"
                                                    :label="opt" size="small" outlined
                                                    @click="sendMessage(opt)"
                                                />
                                            </div>
                                        </div>
                                    </template>

                                    <div class="ai-action-footer">
                                        <Button label="Apply" icon="pi pi-check" size="small" @click="store.applyActionsFromMessage(idx)" />
                                        <Button label="Dismiss" size="small" text severity="secondary" @click="store.dismissActions(idx)" />
                                    </div>
                                </div>

                                <!-- Applied badge -->
                                <div v-else-if="msg.applied && msg.actions?.length && !msg.error" class="ai-applied-badge">
                                    <i class="pi pi-check-circle" /> Applied
                                </div>
                            </div>
                        </div>
                    </template>

                    <!-- Typing indicator -->
                    <div v-if="store.isLoading" class="ai-msg ai-msg--assistant">
                        <div class="ai-avatar-sm">
                            <svg viewBox="0 0 20 20" fill="none" width="11" height="11">
                                <path d="M10 2l1.5 5.5L17 9l-5.5 1.5L10 16l-1.5-5.5L3 9l5.5-1.5Z" fill="white" opacity="0.95"/>
                            </svg>
                        </div>
                        <div class="ai-bubble ai-bubble--assistant ai-typing-wrap">
                            <div class="ai-typing"><span /><span /><span /></div>
                            <div class="ai-thinking-label">{{ loadingStatus }}</div>
                        </div>
                        <button class="ai-stop-btn" title="Stop" @click="store.cancelRequest()">
                            <i class="pi pi-stop-circle" />
                        </button>
                    </div>
                </div>

                <!-- Input bar -->
                <div class="ai-input-bar">
                    <label class="ai-upload-btn" title="Parse RFQ document (PDF or DOCX)">
                        <i class="pi pi-upload" />
                        <input type="file" accept=".pdf,.docx" style="display:none" @change="onRfqUpload" />
                    </label>
                    <InputText
                        ref="inputEl"
                        v-model="draft"
                        placeholder="Ask me anything or upload RFQ…"
                        class="ai-input"
                        :disabled="store.isLoading"
                        @keydown.enter.exact.prevent="sendMessage()"
                    />
                    <Button
                        icon="pi pi-send"
                        :disabled="!draft.trim() || store.isLoading"
                        :loading="store.isLoading"
                        @click="sendMessage()"
                    />
                </div>
            </div>
        </Transition>

        <!-- Draggable FAB -->
        <button
            class="ai-fab"
            :class="{ 'ai-fab--open': store.isOpen, 'ai-fab--busy': store.isLoading }"
            :style="fabStyle"
            :title="store.isOpen ? 'Close AI assistant' : 'Open AI assistant (drag to move)'"
            @pointerdown="onPointerDown"
            @click="onFabClick"
        >
            <!-- Pulse ring when loading -->
            <span v-if="store.isLoading" class="ai-fab-pulse" />

            <!-- Sparkle/AI icon -->
            <svg viewBox="0 0 28 28" fill="none" xmlns="http://www.w3.org/2000/svg" width="24" height="24" class="ai-fab-icon">
                <path d="M14 4l2.2 8.8L25 15l-8.8 2.2L14 26l-2.2-8.8L3 15l8.8-2.2Z" fill="white" opacity="0.95"/>
                <path d="M23 3l.9 3.6L27.5 7.5l-3.6.9L23 13l-.9-3.6L18.5 8.5l3.6-.9Z" fill="white" opacity="0.75"/>
                <path d="M5 21l.6 2.4L8 24l-2.4.6L5 27l-.6-2.4L2 24l2.4-.6Z" fill="white" opacity="0.75"/>
            </svg>

            <!-- X icon when open -->
            <svg v-if="store.isOpen" viewBox="0 0 24 24" fill="none" width="22" height="22" class="ai-fab-icon ai-fab-icon--close">
                <path d="M18 6L6 18M6 6l12 12" stroke="white" stroke-width="2.5" stroke-linecap="round"/>
            </svg>
        </button>
    </Teleport>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick, reactive } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAiChatStore, type AiContext } from '../stores/aiChatStore';
import { useEstimateStore } from '../stores/estimateStore';
import { useUserStore } from '@/stores/userStore';
import { useTheme } from '@/composables/useTheme';

const props = defineProps<{ currentPage?: string }>();

const store = useAiChatStore();
const estimateStore = useEstimateStore();
const userStore = useUserStore();
const route = useRoute();
const router = useRouter();
const { isDark, toggleTheme } = useTheme();

// Execute app_control commands (theme toggle, navigation)
watch(() => store.appControlQueue.length, (len) => {
    if (len === 0) return;
    const items = store.drainAppControlQueue();
    for (const a of items) {
        if (a.action !== 'app_control') continue;
        if (a.command === 'switch_theme') {
            const wantDark = a.theme !== 'light';
            if (isDark.value !== wantDark) toggleTheme();
        } else if (a.command === 'navigate_to') {
            router.push(a.route);
        } else if (a.command === 'open_new_estimate') {
            router.push('/estimating/estimates/new');
        } else if (a.command === 'open_new_staffing_plan') {
            router.push('/estimating/staffing/new');
        }
    }
});

const draft = ref('');
const threadEl = ref<HTMLElement>();
const inputEl = ref<any>();
const noApiKey = ref(false);
const savingForTemplate = ref(false);

// ── Loading status cycling ──────────────────────────────────────────────────
const loadingElapsed = ref(0);
let _loadingTimer: ReturnType<typeof setInterval> | null = null;

watch(() => store.isLoading, (loading) => {
    if (loading) {
        loadingElapsed.value = 0;
        _loadingTimer = setInterval(() => { loadingElapsed.value++; }, 1000);
    } else {
        if (_loadingTimer) { clearInterval(_loadingTimer); _loadingTimer = null; }
        loadingElapsed.value = 0;
    }
});

const loadingStatus = computed(() => {
    const s = loadingElapsed.value;
    if (s < 5)  return 'Thinking…';
    if (s < 15) return 'Searching past jobs…';
    if (s < 30) return 'Analyzing crew & equipment…';
    if (s < 50) return 'Comparing estimates…';
    if (s < 70) return 'Calculating gaps…';
    return 'Almost there…';
});

const hints = computed(() => {
    const page = props.currentPage ?? '';
    if (page === 'staffing') return [
        'New staffing plan for Shell Pasadena TX, 13 days starting June 10, day shift',
        'Add boilermaker crew: 1 General Foreman, 4 Journeymen, 2 Helpers',
        'What staffing plans are currently active?',
        'Add the same crew for night shift',
    ];
    if (page === 'estimates-list') return [
        "What's our total pipeline worth right now?",
        'Show me all Shell jobs this summer',
        "What's our win rate against Chevron?",
        'How many workers are active on jobs today?',
    ];
    if (page === 'analytics' || page === 'global-analytics') return [
        "What's our total pipeline worth right now?",
        'How many workers are active on jobs today?',
        "What's our win rate this year?",
        'Show me all awarded jobs',
    ];
    return [
        'New estimate for Shell Deer Park TX — both shifts, 14 days starting June 10',
        'Add a boilermaker crew: 1 General Foreman, 4 Journeymen, 2 Helpers, day shift',
        'Check this estimate for rate anomalies and pricing risks',
        'Do we have Shell Deer Park TX rates? Load them and verify against benchmarks',
    ];
});

// ── Drag logic ─────────────────────────────────────────────────────────────

const FAB_SIZE = 56;
const WIN_W = 390;
const WIN_H = 530;
const EDGE_PAD = 16;

const fabPos = reactive({ right: 28, bottom: 28 });
let pointerStartX = 0;
let pointerStartY = 0;
let startRight = 0;
let startBottom = 0;
let totalMovement = 0;
let dragging = false;

function onPointerDown(e: PointerEvent) {
    if (e.button !== 0) return;
    pointerStartX = e.clientX;
    pointerStartY = e.clientY;
    startRight = fabPos.right;
    startBottom = fabPos.bottom;
    totalMovement = 0;
    dragging = false;
    (e.currentTarget as HTMLElement).setPointerCapture(e.pointerId);
    document.addEventListener('pointermove', onPointerMove);
    document.addEventListener('pointerup', onPointerUp, { once: true });
    e.preventDefault();
}

function onPointerMove(e: PointerEvent) {
    const dx = e.clientX - pointerStartX;
    const dy = e.clientY - pointerStartY;
    totalMovement += Math.abs(dx) + Math.abs(dy);
    if (totalMovement > 6) dragging = true;
    if (!dragging) return;

    const vw = window.innerWidth;
    const vh = window.innerHeight;

    fabPos.right = Math.max(EDGE_PAD, Math.min(vw - FAB_SIZE - EDGE_PAD, startRight - dx));
    fabPos.bottom = Math.max(EDGE_PAD, Math.min(vh - FAB_SIZE - EDGE_PAD, startBottom - dy));
}

function onPointerUp() {
    document.removeEventListener('pointermove', onPointerMove);
}

function onFabClick() {
    if (dragging) return; // was a drag, not a click
    store.toggle();
}

// ── Styles ─────────────────────────────────────────────────────────────────

const fabStyle = computed(() => ({
    right: `${fabPos.right}px`,
    bottom: `${fabPos.bottom}px`,
}));

// Window can be freely dragged — null means anchor to FAB
const winFreePos = reactive<{ left: number; top: number; pinned: boolean }>({
    left: 0, top: 0, pinned: false,
});

function computeAnchoredPos() {
    const vw = window.innerWidth;
    const vh = window.innerHeight;
    let wRight = fabPos.right;
    const maxRight = vw - WIN_W - EDGE_PAD;
    if (wRight > maxRight) wRight = maxRight;
    wRight = Math.max(EDGE_PAD, wRight);
    let wBottom = fabPos.bottom + FAB_SIZE + 12;
    if (wBottom + WIN_H > vh - EDGE_PAD) wBottom = Math.max(EDGE_PAD, vh - WIN_H - EDGE_PAD);
    return { left: vw - wRight - WIN_W, top: vh - wBottom - WIN_H };
}

const windowStyle = computed(() => {
    if (winFreePos.pinned) {
        return {
            left: `${winFreePos.left}px`,
            top: `${winFreePos.top}px`,
            right: 'auto',
            bottom: 'auto',
            width: `${WIN_W}px`,
            height: `${WIN_H}px`,
        };
    }
    const vw = window.innerWidth;
    const vh = window.innerHeight;
    let wRight = fabPos.right;
    const maxRight = vw - WIN_W - EDGE_PAD;
    if (wRight > maxRight) wRight = maxRight;
    wRight = Math.max(EDGE_PAD, wRight);
    let wBottom = fabPos.bottom + FAB_SIZE + 12;
    if (wBottom + WIN_H > vh - EDGE_PAD) wBottom = Math.max(EDGE_PAD, vh - WIN_H - EDGE_PAD);
    return {
        right: `${wRight}px`,
        bottom: `${wBottom}px`,
        left: 'auto',
        bottom_: 'auto',
        width: `${WIN_W}px`,
        height: `${WIN_H}px`,
    };
});

// ── Window drag ─────────────────────────────────────────────────────────────

let winDragStartX = 0;
let winDragStartY = 0;
let winDragStartLeft = 0;
let winDragStartTop = 0;

function onWindowHeaderPointerDown(e: PointerEvent) {
    if (e.button !== 0) return;
    // Don't hijack clicks on buttons inside the header (trash, close)
    if ((e.target as HTMLElement).closest('button')) return;
    // If not already pinned, capture current rendered position
    if (!winFreePos.pinned) {
        const p = computeAnchoredPos();
        winFreePos.left = p.left;
        winFreePos.top = p.top;
        winFreePos.pinned = true;
    }
    winDragStartX = e.clientX;
    winDragStartY = e.clientY;
    winDragStartLeft = winFreePos.left;
    winDragStartTop = winFreePos.top;
    (e.currentTarget as HTMLElement).setPointerCapture(e.pointerId);
    document.addEventListener('pointermove', onWindowPointerMove);
    document.addEventListener('pointerup', onWindowPointerUp, { once: true });
    e.preventDefault();
}

function onWindowPointerMove(e: PointerEvent) {
    const vw = window.innerWidth;
    const vh = window.innerHeight;
    winFreePos.left = Math.max(0, Math.min(vw - WIN_W, winDragStartLeft + e.clientX - winDragStartX));
    winFreePos.top  = Math.max(0, Math.min(vh - WIN_H,  winDragStartTop  + e.clientY - winDragStartY));
}

function onWindowPointerUp() {
    document.removeEventListener('pointermove', onWindowPointerMove);
}

// ── Chat logic ──────────────────────────────────────────────────────────────

function buildContext(): AiContext {
    const h = estimateStore.header;
    const page = props.currentPage ?? (route.path.includes('staffing') ? 'staffing' : 'estimate');
    return {
        companyCode: userStore.companyCode ?? '',
        currentPage: page,
        currentEstimateId: h.estimateId,
        headerSnapshot: {
            name: h.name || undefined,
            client: h.client || undefined,
            city: h.city || undefined,
            state: h.state || undefined,
            startDate: h.startDate || undefined,
            endDate: h.endDate || undefined,
            days: h.days || undefined,
            shift: h.shift || undefined,
            jobType: h.jobType || undefined,
            jobLetter: h.jobLetter || undefined,
            branch: h.branch || undefined,
        },
        rateBookName: estimateStore.rateBookName || undefined,
        currentRateBookId: estimateStore.header.rateBookId || undefined,
    };
}

async function sendMessage(text?: string) {
    const msg = (text ?? draft.value).trim();
    if (!msg) return;
    draft.value = '';
    await store.sendMessage(msg, buildContext());
    scrollToBottom();
}

function sendHint(hint: string) {
    draft.value = hint;
    nextTick(() => inputEl.value?.$el?.focus());
}

async function saveAndApplyTemplate(templateId: number, templateName: string, msgIdx: number) {
    savingForTemplate.value = true;
    try {
        await estimateStore.saveEstimate();
        // Hide the save prompt on this message
        const msg = store.messages[msgIdx];
        if (msg) msg.savePrompt = false;
        // Now apply the template (estimate is saved, has an ID)
        await store.applyTemplate(-1, templateId, templateName);
        scrollToBottom();
    } catch {
        store.messages.push({
            role: 'assistant',
            content: 'Save failed. Please save the estimate manually and try again.',
            timestamp: new Date(),
            error: true,
            applied: true,
        });
        scrollToBottom();
    } finally {
        savingForTemplate.value = false;
    }
}

async function onRfqUpload(e: Event) {
    const file = (e.target as HTMLInputElement).files?.[0];
    if (!file) return;
    (e.target as HTMLInputElement).value = ''; // reset so same file can be re-uploaded
    await store.uploadRfq(file);
    scrollToBottom();
}

function scrollToBottom() {
    nextTick(() => {
        if (threadEl.value) threadEl.value.scrollTop = threadEl.value.scrollHeight;
    });
}

watch(() => store.messages.length, scrollToBottom);
watch(() => store.isOpen, (open) => {
    if (open) nextTick(() => inputEl.value?.$el?.focus());
});
</script>

<style scoped>
/* ── FAB ─────────────────────────────────────────────────────────────────── */
.ai-fab {
    position: fixed;
    width: 56px; height: 56px;
    border-radius: 50%;
    border: none;
    background: linear-gradient(135deg, #3b82f6 0%, #6d28d9 100%);
    cursor: grab;
    z-index: 1200;
    display: flex; align-items: center; justify-content: center;
    box-shadow: 0 4px 20px rgba(59, 130, 246, 0.5), 0 2px 8px rgba(0,0,0,0.3);
    transition: box-shadow 0.2s, transform 0.15s;
    user-select: none;
    touch-action: none;
    overflow: visible;
}
.ai-fab:hover {
    box-shadow: 0 6px 28px rgba(59, 130, 246, 0.65), 0 3px 12px rgba(0,0,0,0.35);
    transform: scale(1.06);
}
.ai-fab:active { cursor: grabbing; transform: scale(0.97); }
.ai-fab--open {
    background: linear-gradient(135deg, #6d28d9 0%, #3b82f6 100%);
}

/* Icon states */
.ai-fab-icon { pointer-events: none; transition: opacity 0.15s, transform 0.15s; }
.ai-fab-icon--close {
    position: absolute;
    opacity: 0;
    transform: rotate(-45deg) scale(0.6);
}
.ai-fab--open .ai-fab-icon:first-of-type {
    opacity: 0;
    transform: rotate(45deg) scale(0.6);
}
.ai-fab--open .ai-fab-icon--close {
    opacity: 1;
    transform: rotate(0deg) scale(1);
}

/* Pulse ring while AI is working */
.ai-fab-pulse {
    position: absolute;
    inset: -6px;
    border-radius: 50%;
    border: 2px solid rgba(59, 130, 246, 0.6);
    animation: ai-pulse-ring 1.4s cubic-bezier(0.4, 0, 0.6, 1) infinite;
    pointer-events: none;
}
@keyframes ai-pulse-ring {
    0% { transform: scale(0.88); opacity: 0.8; }
    50% { transform: scale(1.08); opacity: 0.3; }
    100% { transform: scale(0.88); opacity: 0.8; }
}

/* ── Chat window ─────────────────────────────────────────────────────────── */
.ai-window {
    position: fixed;
    z-index: 1199;
    display: flex; flex-direction: column;
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 16px;
    box-shadow: 0 12px 48px rgba(0,0,0,0.35), 0 4px 16px rgba(0,0,0,0.2);
    overflow: hidden;
}

/* Pop transition */
.ai-pop-enter-active, .ai-pop-leave-active {
    transition: opacity 0.2s ease, transform 0.22s cubic-bezier(0.34, 1.56, 0.64, 1);
    transform-origin: bottom right;
}
.ai-pop-enter-from, .ai-pop-leave-to {
    opacity: 0;
    transform: scale(0.85) translateY(12px);
}

/* Header */
.ai-header {
    display: flex; align-items: center; justify-content: space-between;
    padding: 10px 12px;
    border-bottom: 1px solid var(--surface-border);
    background: linear-gradient(135deg, rgba(59,130,246,0.08) 0%, rgba(109,40,217,0.06) 100%);
    flex-shrink: 0;
}
.ai-drag-handle {
    cursor: grab;
    user-select: none;
}
.ai-drag-handle:active { cursor: grabbing; }
.ai-header-left { display: flex; align-items: center; gap: 10px; }

.ai-avatar {
    width: 34px; height: 34px; border-radius: 10px;
    background: linear-gradient(135deg, #3b82f6, #6d28d9);
    display: flex; align-items: center; justify-content: center;
    flex-shrink: 0;
    box-shadow: 0 2px 8px rgba(59, 130, 246, 0.4);
}
.ai-title { font-size: 0.86rem; font-weight: 700; color: var(--text-color); line-height: 1.2; }
.ai-subtitle { font-size: 0.66rem; color: var(--text-color-secondary); }

/* Warning */
.ai-warning {
    display: flex; align-items: flex-start; gap: 8px;
    padding: 7px 12px;
    background: color-mix(in srgb, var(--yellow-500) 10%, transparent);
    border-bottom: 1px solid color-mix(in srgb, var(--yellow-500) 28%, transparent);
    color: var(--yellow-500); font-size: 0.72rem; flex-shrink: 0;
}
.ai-warning code {
    font-family: monospace; font-size: 0.78em;
    background: rgba(255,255,255,0.1); padding: 1px 3px; border-radius: 3px;
}

/* Thread */
.ai-thread {
    flex: 1; overflow-y: auto; padding: 14px 10px;
    display: flex; flex-direction: column; gap: 12px;
}

/* Welcome */
.ai-welcome {
    display: flex; flex-direction: column; align-items: center;
    gap: 10px; padding: 18px 6px; text-align: center;
}
.ai-welcome-icon {
    width: 58px; height: 58px; border-radius: 50%;
    background: color-mix(in srgb, #3b82f6 10%, transparent);
    display: flex; align-items: center; justify-content: center;
}
.ai-welcome-title { font-size: 0.95rem; font-weight: 700; color: var(--text-color); }
.ai-welcome-hints { display: flex; flex-direction: column; gap: 5px; width: 100%; max-width: 330px; }
.ai-hint {
    padding: 7px 11px;
    background: var(--surface-ground); border: 1px solid var(--surface-border);
    border-radius: 8px; font-size: 0.75rem; color: var(--text-color-secondary);
    cursor: pointer; text-align: left;
    transition: background 0.12s, border-color 0.12s;
}
.ai-hint:hover { background: var(--surface-hover); border-color: #3b82f6; color: var(--text-color); }

/* Messages */
.ai-msg { display: flex; gap: 7px; }
.ai-msg--user { justify-content: flex-end; }
.ai-msg--assistant { align-items: flex-start; }
.ai-msg-body { display: flex; flex-direction: column; gap: 5px; max-width: calc(100% - 36px); }

.ai-avatar-sm {
    width: 24px; height: 24px; border-radius: 7px;
    background: linear-gradient(135deg, #3b82f6, #6d28d9);
    display: flex; align-items: center; justify-content: center;
    flex-shrink: 0; margin-top: 2px;
}

.ai-bubble {
    padding: 8px 12px; border-radius: 12px;
    font-size: 0.81rem; line-height: 1.55; word-break: break-word;
}
.ai-bubble--user {
    background: linear-gradient(135deg, #3b82f6, #6d28d9);
    color: white;
    border-bottom-right-radius: 4px; max-width: 82%;
}
.ai-bubble--assistant {
    background: var(--surface-ground); border: 1px solid var(--surface-border);
    border-bottom-left-radius: 4px; color: var(--text-color);
}
.ai-bubble--error {
    background: color-mix(in srgb, var(--red-500) 10%, transparent);
    border-color: color-mix(in srgb, var(--red-500) 30%, transparent);
    color: var(--red-400);
}

/* Typing */
.ai-typing-wrap { display: flex; flex-direction: column; gap: 4px; padding: 10px 14px; }
.ai-typing { display: flex; align-items: center; gap: 4px; }
.ai-thinking-label { font-size: 0.68rem; color: var(--text-color-secondary); font-style: italic; }
.ai-stop-btn { background: none; border: none; color: rgba(255,255,255,0.5); cursor: pointer; padding: 4px 8px; font-size: 16px; margin-left: 4px; border-radius: 4px; transition: color 0.15s; }
.ai-stop-btn:hover { color: #f87171; }
.ai-typing span {
    width: 5px; height: 5px; border-radius: 50%;
    background: var(--text-color-secondary);
    animation: ai-bounce 1.2s infinite;
}
.ai-typing span:nth-child(2) { animation-delay: 0.2s; }
.ai-typing span:nth-child(3) { animation-delay: 0.4s; }
@keyframes ai-bounce {
    0%, 80%, 100% { transform: scale(0.8); opacity: 0.5; }
    40% { transform: scale(1.1); opacity: 1; }
}

/* Action card */
.ai-action-card {
    background: var(--surface-card);
    border: 1px solid color-mix(in srgb, #3b82f6 35%, transparent);
    border-radius: 10px; overflow: hidden;
}
.ai-action-header {
    display: flex; align-items: center; gap: 6px;
    padding: 6px 11px;
    background: color-mix(in srgb, #3b82f6 8%, transparent);
    border-bottom: 1px solid color-mix(in srgb, #3b82f6 20%, transparent);
    font-size: 0.70rem; font-weight: 700; text-transform: uppercase;
    letter-spacing: 0.06em; color: var(--primary-color);
}
.ai-action-list {
    margin: 0; padding: 7px 11px 7px 26px;
    font-size: 0.77rem; color: var(--text-color); line-height: 1.7;
}
.ai-action-footer {
    display: flex; gap: 8px; padding: 7px 11px;
    border-top: 1px solid var(--surface-border);
    background: var(--surface-ground);
}

/* Rate book suggestion */
.ai-suggest-card {
    display: flex; align-items: flex-start; gap: 10px;
    padding: 9px 11px;
    border-top: 1px solid var(--surface-border);
    background: color-mix(in srgb, #10b981 6%, transparent);
}
.ai-suggest-icon {
    width: 26px; height: 26px; border-radius: 6px;
    background: color-mix(in srgb, #10b981 15%, transparent);
    color: #10b981; display: flex; align-items: center; justify-content: center;
    font-size: 0.75rem; flex-shrink: 0; margin-top: 2px;
}
.ai-suggest-body { flex: 1; min-width: 0; }
.ai-suggest-title { font-size: 0.73rem; font-weight: 700; color: #10b981; margin-bottom: 2px; }
.ai-suggest-reason { font-size: 0.71rem; color: var(--text-color-secondary); margin-bottom: 7px; }
.ai-suggest-btns { display: flex; flex-wrap: wrap; gap: 5px; }

/* Template card */
.ai-template-card {
    display: flex; align-items: center; gap: 8px;
    padding: 7px 11px; border-top: 1px solid var(--surface-border);
    font-size: 0.77rem; color: var(--text-color);
    background: color-mix(in srgb, #3b82f6 5%, transparent);
}

/* Clarification */
.ai-clarification { padding: 7px 11px; border-top: 1px solid var(--surface-border); }
.ai-clarification-q { font-size: 0.79rem; color: var(--text-color); margin-bottom: 7px; font-style: italic; }
.ai-clarification-opts { display: flex; flex-wrap: wrap; gap: 5px; }

/* Applied badge */
.ai-applied-badge {
    display: flex; align-items: center; gap: 4px;
    font-size: 0.69rem; color: #4ade80; padding: 2px 0;
}

/* Input bar */
.ai-input-bar {
    display: flex; gap: 7px; padding: 10px;
    border-top: 1px solid var(--surface-border);
    background: var(--surface-section); flex-shrink: 0;
    border-radius: 0 0 16px 16px;
}
.ai-input { flex: 1; font-size: 0.81rem; }

/* Save prompt */
.ai-save-prompt {
    padding: 5px 0 2px;
}

/* ── Upload button ─────────────────────────────────────────────────────────── */
.ai-upload-btn {
    display: flex; align-items: center; justify-content: center;
    width: 32px; height: 32px; flex-shrink: 0;
    cursor: pointer; border-radius: 6px;
    color: var(--text-color-secondary);
    transition: background 0.15s, color 0.15s;
}
.ai-upload-btn:hover { background: var(--surface-hover); color: var(--primary-color); }

/* ── Rate anomaly warning ──────────────────────────────────────────────────── */
.ai-anomaly-card {
    display: flex; align-items: flex-start; gap: 8px;
    background: color-mix(in srgb, var(--yellow-500) 10%, transparent);
    border: 1px solid color-mix(in srgb, var(--yellow-500) 40%, transparent);
    border-radius: 6px; padding: 8px 10px; margin-top: 6px;
}
.ai-anomaly-body {
    display: flex; flex-direction: column; gap: 2px;
}
.ai-anomaly-title {
    font-size: 0.75rem; font-weight: 700; color: var(--yellow-700, #a16207);
}
.ai-anomaly-detail { font-size: 0.73rem; color: var(--text-color-secondary); }
</style>
