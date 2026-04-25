<template>
    <Sidebar
        :visible="visible"
        position="right"
        style="width: 440px"
        :dismissable="true"
        @update:visible="emit('update:visible', $event)"
    >
        <template #header>
            <span class="font-bold text-base">Revision History</span>
        </template>

        <!-- Action bar -->
        <div class="rev-action-bar">
            <Button
                label="+ Create Revision"
                size="small"
                @click="startCreate"
            />
            <Button
                :label="compareMode ? 'Cancel Compare' : 'Compare'"
                size="small"
                :outlined="!compareMode"
                :severity="compareMode ? 'danger' : 'secondary'"
                @click="toggleCompareMode"
            />
        </div>

        <!-- Inline create form -->
        <div v-if="creating" class="rev-create-form">
            <div class="rev-create-label">Revision note (optional)</div>
            <InputText
                v-model="createDescription"
                placeholder="e.g. Revised labor rates after client call"
                class="w-full"
                autofocus
                @keydown.enter="confirmCreate"
                @keydown.esc="cancelCreate"
            />
            <div class="rev-create-actions">
                <Button label="Confirm" size="small" :loading="saving" @click="confirmCreate" />
                <Button label="Cancel" size="small" text @click="cancelCreate" />
            </div>
        </div>

        <!-- Compare hint -->
        <div v-if="compareMode && compareSelected.length < 2" class="rev-compare-hint">
            <i class="pi pi-info-circle" />
            Select two revisions to compare ({{ compareSelected.length }}/2 selected)
        </div>

        <!-- Compare panel -->
        <div v-if="compareMode && compareSelected.length === 2" class="rev-compare-panel">
            <div class="rev-compare-title">Comparison</div>
            <div class="rev-compare-grid">
                <div class="rev-compare-col">
                    <div class="rev-compare-col-hdr">Rev {{ compareRevA?.revisionNumber }}</div>
                    <div class="rev-compare-row"><span>Labor</span><span>{{ fmt(compareRevA?.laborTotal) }}</span></div>
                    <div class="rev-compare-row"><span>Equipment</span><span>{{ fmt(compareRevA?.equipTotal) }}</span></div>
                    <div class="rev-compare-row bold"><span>Grand Total</span><span>{{ fmt(compareRevA?.grandTotal) }}</span></div>
                </div>
                <div class="rev-compare-col">
                    <div class="rev-compare-col-hdr">Rev {{ compareRevB?.revisionNumber }}</div>
                    <div class="rev-compare-row"><span>Labor</span><span>{{ fmt(compareRevB?.laborTotal) }}</span></div>
                    <div class="rev-compare-row"><span>Equipment</span><span>{{ fmt(compareRevB?.equipTotal) }}</span></div>
                    <div class="rev-compare-row bold"><span>Grand Total</span><span>{{ fmt(compareRevB?.grandTotal) }}</span></div>
                </div>
                <div class="rev-compare-col">
                    <div class="rev-compare-col-hdr">Delta</div>
                    <div class="rev-compare-row">
                        <span>Labor</span>
                        <span :class="deltaClass(delta.labor)">{{ fmtDelta(delta.labor) }}</span>
                    </div>
                    <div class="rev-compare-row">
                        <span>Equipment</span>
                        <span :class="deltaClass(delta.equip)">{{ fmtDelta(delta.equip) }}</span>
                    </div>
                    <div class="rev-compare-row bold">
                        <span>Grand Total</span>
                        <span :class="deltaClass(delta.grand)">{{ fmtDelta(delta.grand) }}</span>
                    </div>
                </div>
            </div>
        </div>

        <!-- Empty state -->
        <div v-if="!loading && revisions.length === 0 && !creating" class="rev-empty">
            <i class="pi pi-history rev-empty-icon" />
            <p>No revisions yet.</p>
            <p class="rev-empty-sub">Create Rev 1 to snapshot your current work.</p>
        </div>

        <!-- Loading state -->
        <div v-if="loading" class="rev-loading">
            <ProgressSpinner style="width: 32px; height: 32px" />
        </div>

        <!-- Revision cards -->
        <div v-if="!loading" class="rev-list">
            <div
                v-for="rev in revisions"
                :key="rev.estimateRevisionId"
                class="rev-card"
                :class="{ 'rev-card--current': rev.isCurrent }"
            >
                <!-- Card header row -->
                <div class="rev-card-header">
                    <div class="rev-card-title-row">
                        <span class="rev-card-number">Rev {{ rev.revisionNumber }}</span>
                        <span v-if="rev.isCurrent" class="rev-current-badge">CURRENT</span>
                    </div>
                    <div v-if="compareMode" class="rev-compare-check">
                        <Checkbox
                            :modelValue="compareSelected.includes(rev.estimateRevisionId)"
                            :binary="true"
                            :disabled="compareSelected.length >= 2 && !compareSelected.includes(rev.estimateRevisionId)"
                            @update:modelValue="toggleCompareSelect(rev.estimateRevisionId)"
                        />
                    </div>
                </div>

                <!-- Description -->
                <div v-if="rev.description" class="rev-card-desc">{{ rev.description }}</div>

                <!-- Meta row -->
                <div class="rev-card-meta">
                    <span><i class="pi pi-calendar" /> {{ fmtDate(rev.savedAt) }}</span>
                    <span><i class="pi pi-users" /> {{ rev.laborCount }} labor</span>
                    <span><i class="pi pi-wrench" /> {{ rev.equipCount }} equip</span>
                </div>

                <!-- Totals row -->
                <div class="rev-card-totals">
                    <span class="rev-total-grand">Total: {{ fmt(rev.grandTotal) }}</span>
                    <span class="rev-total-sub">L: {{ fmt(rev.laborTotal) }}</span>
                    <span class="rev-total-sub">E: {{ fmt(rev.equipTotal) }}</span>
                </div>

                <!-- Saved by -->
                <div class="rev-card-savedby">Saved by {{ rev.savedBy }}</div>

                <!-- Actions -->
                <div class="rev-card-actions">
                    <Button
                        label="View"
                        size="small"
                        outlined
                        @click="viewRevision(rev)"
                    />
                    <Button
                        v-if="!rev.isCurrent"
                        label="Restore"
                        size="small"
                        outlined
                        severity="success"
                        :loading="restoringId === rev.estimateRevisionId"
                        @click="restoreRevision(rev)"
                    />
                    <Button
                        v-if="revisions.length > 1"
                        label="Delete"
                        size="small"
                        outlined
                        severity="danger"
                        :loading="deletingId === rev.estimateRevisionId"
                        @click="deleteRevision(rev)"
                    />
                </div>
            </div>
        </div>

        <!-- View (read-only) dialog -->
        <Dialog
            v-model:visible="viewDialogVisible"
            :header="`Rev ${viewingRev?.revisionNumber} — Read Only Preview`"
            modal
            :style="{ width: '560px' }"
            :closable="true"
        >
            <div v-if="viewingRev" class="rev-view-content">
                <div class="rev-view-row">
                    <span class="rev-view-label">Saved</span>
                    <span>{{ fmtDate(viewingRev.savedAt) }} by {{ viewingRev.savedBy }}</span>
                </div>
                <div v-if="viewingRev.description" class="rev-view-row">
                    <span class="rev-view-label">Note</span>
                    <span>{{ viewingRev.description }}</span>
                </div>
                <div class="rev-view-row">
                    <span class="rev-view-label">Labor rows</span>
                    <span>{{ viewingRev.laborCount }}</span>
                </div>
                <div class="rev-view-row">
                    <span class="rev-view-label">Equipment rows</span>
                    <span>{{ viewingRev.equipCount }}</span>
                </div>
                <div class="rev-view-row">
                    <span class="rev-view-label">Labor Total</span>
                    <span>{{ fmt(viewingRev.laborTotal) }}</span>
                </div>
                <div class="rev-view-row">
                    <span class="rev-view-label">Equipment Total</span>
                    <span>{{ fmt(viewingRev.equipTotal) }}</span>
                </div>
                <div class="rev-view-row bold">
                    <span class="rev-view-label">Grand Total</span>
                    <span>{{ fmt(viewingRev.grandTotal) }}</span>
                </div>
            </div>
            <template #footer>
                <Button label="Close" text @click="viewDialogVisible = false" />
                <Button
                    v-if="viewingRev && !viewingRev.isCurrent"
                    label="Restore This Revision"
                    severity="success"
                    :loading="restoringId === viewingRev.estimateRevisionId"
                    @click="viewingRev && restoreRevision(viewingRev)"
                />
            </template>
        </Dialog>

        <!-- Delete confirm dialog -->
        <Dialog
            v-model:visible="deleteDialogVisible"
            header="Delete Revision"
            modal
            :style="{ width: '380px' }"
        >
            <p>Are you sure you want to delete <strong>Rev {{ deletingRev?.revisionNumber }}</strong>? This cannot be undone.</p>
            <template #footer>
                <Button label="Cancel" text @click="deleteDialogVisible = false" />
                <Button
                    label="Delete"
                    severity="danger"
                    :loading="deletingId === deletingRev?.estimateRevisionId"
                    @click="confirmDelete"
                />
            </template>
        </Dialog>

        <Toast />
    </Sidebar>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { useToast } from 'primevue/usetoast';
import { useApiStore } from '@/stores/apiStore';

// ── Types ────────────────────────────────────────────────────────────────────

interface RevisionRecord {
    estimateRevisionId: number;
    revisionNumber: number;
    isCurrent: boolean;
    description?: string;
    savedBy: string;
    savedAt: string;
    laborCount: number;
    equipCount: number;
    laborTotal: number;
    equipTotal: number;
    grandTotal: number;
}

// ── Props / emits ────────────────────────────────────────────────────────────

const props = defineProps<{
    estimateId: number | undefined;
    visible: boolean;
}>();

const emit = defineEmits<{
    (e: 'update:visible', v: boolean): void;
    (e: 'restored'): void;
}>();

// ── State ────────────────────────────────────────────────────────────────────

const apiStore = useApiStore();
const toast = useToast();

const revisions = ref<RevisionRecord[]>([]);
const loading = ref(false);
const saving = ref(false);

// Create
const creating = ref(false);
const createDescription = ref('');

// Restore / delete
const restoringId = ref<number | null>(null);
const deletingId = ref<number | null>(null);
const deleteDialogVisible = ref(false);
const deletingRev = ref<RevisionRecord | null>(null);

// View
const viewDialogVisible = ref(false);
const viewingRev = ref<RevisionRecord | null>(null);

// Compare
const compareMode = ref(false);
const compareSelected = ref<number[]>([]);

// ── Computed ─────────────────────────────────────────────────────────────────

const compareRevA = computed(() =>
    revisions.value.find(r => r.estimateRevisionId === compareSelected.value[0]) ?? null
);
const compareRevB = computed(() =>
    revisions.value.find(r => r.estimateRevisionId === compareSelected.value[1]) ?? null
);
const delta = computed(() => ({
    labor: (compareRevB.value?.laborTotal ?? 0) - (compareRevA.value?.laborTotal ?? 0),
    equip: (compareRevB.value?.equipTotal ?? 0) - (compareRevA.value?.equipTotal ?? 0),
    grand: (compareRevB.value?.grandTotal ?? 0) - (compareRevA.value?.grandTotal ?? 0),
}));

// ── Lifecycle ────────────────────────────────────────────────────────────────

onMounted(() => {
    if (props.estimateId && props.visible) loadRevisions();
});

watch(() => props.visible, (v) => {
    if (v && props.estimateId) loadRevisions();
});

watch(() => props.estimateId, (id) => {
    if (id && props.visible) loadRevisions();
});

// ── Data loading ─────────────────────────────────────────────────────────────

async function loadRevisions() {
    if (!props.estimateId) return;
    loading.value = true;
    try {
        const { data } = await apiStore.api.get(`/api/v1/estimates/${props.estimateId}/revisions`);
        revisions.value = data;
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load revisions', life: 4000 });
    } finally {
        loading.value = false;
    }
}

// ── Create ───────────────────────────────────────────────────────────────────

function startCreate() {
    creating.value = true;
    createDescription.value = '';
}

function cancelCreate() {
    creating.value = false;
    createDescription.value = '';
}

async function confirmCreate() {
    if (!props.estimateId) return;
    saving.value = true;
    try {
        await apiStore.api.post(`/api/v1/estimates/${props.estimateId}/revisions`, {
            description: createDescription.value || null,
        });
        creating.value = false;
        createDescription.value = '';
        await loadRevisions();
        toast.add({ severity: 'success', summary: 'Revision Created', detail: `Rev ${revisions.value.length} snapshot saved`, life: 3000 });
    } catch (err: any) {
        toast.add({ severity: 'error', summary: 'Error', detail: err?.response?.data?.message ?? 'Failed to create revision', life: 4000 });
    } finally {
        saving.value = false;
    }
}

// ── View ─────────────────────────────────────────────────────────────────────

function viewRevision(rev: RevisionRecord) {
    viewingRev.value = rev;
    viewDialogVisible.value = true;
}

// ── Restore ──────────────────────────────────────────────────────────────────

async function restoreRevision(rev: RevisionRecord) {
    if (!props.estimateId) return;
    restoringId.value = rev.estimateRevisionId;
    viewDialogVisible.value = false;
    try {
        await apiStore.api.post(`/api/v1/estimates/${props.estimateId}/revisions/${rev.estimateRevisionId}/restore`);
        await loadRevisions();
        toast.add({ severity: 'success', summary: 'Restored', detail: `Estimate restored to Rev ${rev.revisionNumber}`, life: 3000 });
        emit('restored');
    } catch (err: any) {
        toast.add({ severity: 'error', summary: 'Error', detail: err?.response?.data?.message ?? 'Failed to restore revision', life: 4000 });
    } finally {
        restoringId.value = null;
    }
}

// ── Delete ───────────────────────────────────────────────────────────────────

function deleteRevision(rev: RevisionRecord) {
    deletingRev.value = rev;
    deleteDialogVisible.value = true;
}

async function confirmDelete() {
    if (!props.estimateId || !deletingRev.value) return;
    const rev = deletingRev.value;
    deletingId.value = rev.estimateRevisionId;
    try {
        await apiStore.api.delete(`/api/v1/estimates/${props.estimateId}/revisions/${rev.estimateRevisionId}`);
        deleteDialogVisible.value = false;
        deletingRev.value = null;
        await loadRevisions();
        toast.add({ severity: 'success', summary: 'Deleted', detail: `Rev ${rev.revisionNumber} deleted`, life: 3000 });
    } catch (err: any) {
        toast.add({ severity: 'error', summary: 'Error', detail: err?.response?.data?.message ?? 'Failed to delete revision', life: 4000 });
    } finally {
        deletingId.value = null;
    }
}

// ── Compare ──────────────────────────────────────────────────────────────────

function toggleCompareMode() {
    compareMode.value = !compareMode.value;
    compareSelected.value = [];
}

function toggleCompareSelect(id: number) {
    const idx = compareSelected.value.indexOf(id);
    if (idx >= 0) {
        compareSelected.value.splice(idx, 1);
    } else if (compareSelected.value.length < 2) {
        compareSelected.value.push(id);
    }
}

// ── Formatters ────────────────────────────────────────────────────────────────

function fmt(val: number | undefined) {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(val ?? 0);
}

function fmtDelta(val: number) {
    const sign = val >= 0 ? '+' : '';
    return sign + new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(val);
}

function deltaClass(val: number) {
    if (val > 0) return 'delta-positive';
    if (val < 0) return 'delta-negative';
    return 'delta-neutral';
}

function fmtDate(iso: string) {
    return new Date(iso).toLocaleString('en-US', {
        month: 'short', day: 'numeric', year: 'numeric',
        hour: 'numeric', minute: '2-digit', hour12: true
    });
}
</script>

<style scoped>
/* Action bar */
.rev-action-bar {
    display: flex;
    gap: 8px;
    margin-bottom: 12px;
}

/* Inline create form */
.rev-create-form {
    background: var(--surface-section);
    border: 1px solid var(--surface-border);
    border-radius: 6px;
    padding: 10px 12px;
    margin-bottom: 12px;
    display: flex;
    flex-direction: column;
    gap: 8px;
}

.rev-create-label {
    font-size: 0.75rem;
    font-weight: 600;
    color: var(--text-color-secondary);
    text-transform: uppercase;
    letter-spacing: 0.05em;
}

.rev-create-actions {
    display: flex;
    gap: 6px;
}

/* Compare hint */
.rev-compare-hint {
    background: color-mix(in srgb, var(--primary-color) 8%, transparent);
    border: 1px solid color-mix(in srgb, var(--primary-color) 30%, transparent);
    border-radius: 6px;
    padding: 8px 12px;
    font-size: 0.8rem;
    color: var(--primary-color);
    margin-bottom: 12px;
    display: flex;
    align-items: center;
    gap: 6px;
}

/* Compare panel */
.rev-compare-panel {
    background: var(--surface-section);
    border: 1px solid var(--surface-border);
    border-radius: 6px;
    padding: 12px;
    margin-bottom: 12px;
}

.rev-compare-title {
    font-size: 0.72rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: var(--text-color-secondary);
    margin-bottom: 8px;
}

.rev-compare-grid {
    display: grid;
    grid-template-columns: 1fr 1fr 1fr;
    gap: 8px;
}

.rev-compare-col-hdr {
    font-size: 0.72rem;
    font-weight: 700;
    color: var(--text-color);
    margin-bottom: 4px;
    text-transform: uppercase;
    letter-spacing: 0.04em;
}

.rev-compare-row {
    display: flex;
    justify-content: space-between;
    font-size: 0.78rem;
    padding: 2px 0;
    color: var(--text-color-secondary);
}

.rev-compare-row.bold {
    font-weight: 700;
    color: var(--text-color);
    border-top: 1px solid var(--surface-border);
    margin-top: 4px;
    padding-top: 4px;
}

.delta-positive { color: #22c55e; font-weight: 600; }
.delta-negative { color: #ef4444; font-weight: 600; }
.delta-neutral  { color: var(--text-color-secondary); }

/* Empty / loading states */
.rev-empty {
    text-align: center;
    padding: 40px 20px;
    color: var(--text-color-secondary);
}

.rev-empty-icon {
    font-size: 2.5rem;
    opacity: 0.3;
    display: block;
    margin-bottom: 12px;
}

.rev-empty p { margin: 0 0 4px; font-size: 0.9rem; }
.rev-empty-sub { font-size: 0.78rem; opacity: 0.75; }

.rev-loading {
    display: flex;
    justify-content: center;
    padding: 40px 0;
}

/* Revision list */
.rev-list {
    display: flex;
    flex-direction: column;
    gap: 10px;
}

/* Revision card */
.rev-card {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 8px;
    padding: 12px 14px;
    transition: box-shadow 0.15s;
}

.rev-card--current {
    border-color: #1e3a5f;
    box-shadow: 0 2px 8px rgba(30, 58, 95, 0.15);
}

.rev-card-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 6px;
}

.rev-card-title-row {
    display: flex;
    align-items: center;
    gap: 8px;
}

.rev-card-number {
    font-size: 0.9rem;
    font-weight: 700;
    color: var(--text-color);
}

.rev-current-badge {
    background: #1e3a5f;
    color: #fff;
    font-size: 0.6rem;
    font-weight: 700;
    letter-spacing: 0.08em;
    padding: 2px 7px;
    border-radius: 20px;
    text-transform: uppercase;
}

.rev-compare-check {
    display: flex;
    align-items: center;
}

.rev-card-desc {
    font-size: 0.82rem;
    color: var(--text-color);
    margin-bottom: 6px;
    font-style: italic;
}

.rev-card-meta {
    display: flex;
    gap: 12px;
    font-size: 0.75rem;
    color: var(--text-color-secondary);
    margin-bottom: 4px;
    flex-wrap: wrap;
}

.rev-card-meta i {
    font-size: 0.7rem;
    margin-right: 3px;
}

.rev-card-totals {
    display: flex;
    gap: 10px;
    font-size: 0.78rem;
    margin-bottom: 4px;
    flex-wrap: wrap;
}

.rev-total-grand {
    font-weight: 700;
    color: var(--text-color);
}

.rev-total-sub {
    color: var(--text-color-secondary);
}

.rev-card-savedby {
    font-size: 0.72rem;
    color: var(--text-color-secondary);
    margin-bottom: 8px;
}

.rev-card-actions {
    display: flex;
    gap: 6px;
    flex-wrap: wrap;
}

/* View dialog */
.rev-view-content {
    display: flex;
    flex-direction: column;
    gap: 8px;
    padding: 4px 0;
}

.rev-view-row {
    display: flex;
    gap: 12px;
    font-size: 0.85rem;
}

.rev-view-row.bold {
    font-weight: 700;
}

.rev-view-label {
    min-width: 110px;
    color: var(--text-color-secondary);
    font-size: 0.78rem;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    padding-top: 1px;
}
</style>
