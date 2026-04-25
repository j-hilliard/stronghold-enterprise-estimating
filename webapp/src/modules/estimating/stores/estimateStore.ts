import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import { useApiStore } from '@/stores/apiStore';
import { calcOtHours } from '../composables/useOtCalculator';

// ── Types ──────────────────────────────────────────────────────────────────

export interface EstimateHeader {
    estimateId?: number;
    estimateNumber?: string;
    staffingPlanId?: number | null;
    staffingPlanNumber?: string | null;
    rateBookId?: number;
    revisionNumber?: number | null;
    name: string;
    client: string;
    clientCode?: string;
    msaNumber?: string;
    jobType?: string;
    branch?: string;
    city?: string;
    state?: string;
    site?: string;
    jobLetter?: string;
    shift: string;
    hoursPerShift: number;
    days: number;
    startDate?: string;
    endDate?: string;
    otMethod: string;
    dtWeekends: boolean;
    status: string;
    confidencePct: number;
    lostReason?: string;
    lostNotes?: string;
    isScenario?: boolean;
    vp?: string;
    director?: string;
    region?: string;
}

export interface LaborRow {
    laborRowId?: number;
    position: string;
    laborType: string;
    shift: string;
    craftCode?: string;
    navCode?: string;
    billStRate: number;
    billOtRate: number;
    billDtRate: number;
    scheduleJson?: string;
    stHours: number;
    otHours: number;
    dtHours: number;
    subtotal: number;
    sortOrder?: number;
}

export interface EquipmentRow {
    equipmentRowId?: number;
    name: string;
    rateType: string;
    rate: number;
    qty: number;
    days: number;
    subtotal: number;
    sortOrder?: number;
}

export interface ExpenseRow {
    expenseRowId?: number;
    category: string;
    description: string;
    rate: number;
    unit: string;
    daysOrQty: number;
    people: number;
    billable: boolean;
    subtotal: number;
    sortOrder?: number;
}

export interface EstimateSummary {
    billSubtotal: number;
    discountType: string;
    discountValue: number;
    discountAmount: number;
    taxRate: number;
    taxAmount: number;
    grandTotal: number;
    internalCostTotal: number;
    grossProfit: number;
    grossMarginPct: number;
}

export interface RateBookRate {
    rateBookLaborRateId?: number;
    rateBookId?: number;
    position: string;
    laborType: string;
    craftCode?: string;
    navCode?: string;
    stRate: number;
    otRate: number;
    dtRate: number;
    sortOrder?: number;
}

export interface EstimateRevision {
    estimateRevisionId: number;
    revisionNumber: number;
    isCurrent: boolean;
    description?: string;
    savedBy: string;
    savedAt: string;
}

// ── Defaults ───────────────────────────────────────────────────────────────

export function defaultHeader(): EstimateHeader {
    return {
        name: '',
        client: '',
        shift: 'Day',
        hoursPerShift: 10,
        days: 0,
        otMethod: 'daily8',
        dtWeekends: false,
        status: 'Draft',
        confidencePct: 50,
    };
}

export function defaultSummary(): EstimateSummary {
    return {
        billSubtotal: 0,
        discountType: 'percent',
        discountValue: 0,
        discountAmount: 0,
        taxRate: 0,
        taxAmount: 0,
        grandTotal: 0,
        internalCostTotal: 0,
        grossProfit: 0,
        grossMarginPct: 0,
    };
}

// ── Store ──────────────────────────────────────────────────────────────────

export const useEstimateStore = defineStore('estimate', () => {
    const apiStore = useApiStore();

    const header = ref<EstimateHeader>(defaultHeader());
    const laborRows = ref<LaborRow[]>([]);
    const equipmentRows = ref<EquipmentRow[]>([]);
    const expenseRows = ref<ExpenseRow[]>([]);
    const summary = ref<EstimateSummary>(defaultSummary());
    const revisions = ref<EstimateRevision[]>([]);
    const rateBookRates = ref<RateBookRate[]>([]);
    const rateBookName = ref<string>('');
    const isDirty = ref(false);
    const isLoading = ref(false);
    const isSaving = ref(false);

    const isNew = computed(() => !header.value.estimateId);

    // ── Helpers ─────────────────────────────────────────────────────────────

    function markDirty() { isDirty.value = true; }

    function recalcLaborRow(row: LaborRow) {
        const { stHours, otHours, dtHours } = calcOtHours(
            row.scheduleJson,
            header.value.hoursPerShift,
            header.value.otMethod,
            header.value.dtWeekends,
        );
        row.stHours = stHours;
        row.otHours = otHours;
        row.dtHours = dtHours;
        row.subtotal = Math.round(
            (stHours * row.billStRate + otHours * row.billOtRate + dtHours * row.billDtRate) * 100,
        ) / 100;
    }

    function recalcAllLaborRows() {
        for (const row of laborRows.value) recalcLaborRow(row);
    }

    function recalcEquipmentRow(row: EquipmentRow) {
        row.subtotal = Math.round(row.rate * row.qty * row.days * 100) / 100;
    }

    function recalcExpenseRow(row: ExpenseRow) {
        row.subtotal = Math.round(row.rate * row.daysOrQty * row.people * 100) / 100;
    }

    function recalcSummary() {
        const billSub = [
            ...laborRows.value.map(r => r.subtotal),
            ...equipmentRows.value.map(r => r.subtotal),
            ...expenseRows.value.filter(r => r.billable).map(r => r.subtotal),
        ].reduce((a, b) => a + b, 0);

        const disc = summary.value.discountType === 'percent'
            ? Math.round(billSub * summary.value.discountValue) / 100
            : summary.value.discountValue;

        const taxable = billSub - disc;
        const tax = Math.round(taxable * summary.value.taxRate) / 100;
        const grand = taxable + tax;

        summary.value.billSubtotal = Math.round(billSub * 100) / 100;
        summary.value.discountAmount = Math.round(disc * 100) / 100;
        summary.value.taxAmount = Math.round(tax * 100) / 100;
        summary.value.grandTotal = Math.round(grand * 100) / 100;
        summary.value.grossProfit = Math.round((grand - summary.value.internalCostTotal) * 100) / 100;
        summary.value.grossMarginPct = grand > 0
            ? Math.round((summary.value.grossProfit / grand) * 10000) / 100
            : 0;
    }

    // ── Actions ──────────────────────────────────────────────────────────────

    function reset() {
        header.value = defaultHeader();
        laborRows.value = [];
        equipmentRows.value = [];
        expenseRows.value = [];
        summary.value = defaultSummary();
        revisions.value = [];
        rateBookRates.value = [];
        rateBookName.value = '';
        isDirty.value = false;
    }

    async function loadRateBook(rateBookId: number) {
        const { data } = await apiStore.api.get(`/api/v1/rate-books/${rateBookId}`);
        rateBookRates.value = (data.laborRates ?? []).map((r: any) => ({
            rateBookLaborRateId: r.rateBookLaborRateId,
            rateBookId: r.rateBookId,
            position: r.position,
            laborType: r.laborType,
            craftCode: r.craftCode,
            navCode: r.navCode,
            stRate: r.stRate,
            otRate: r.otRate,
            dtRate: r.dtRate,
            sortOrder: r.sortOrder,
        }));
        rateBookName.value = data.name ?? '';
        header.value.rateBookId = rateBookId;
    }

    function clearRateBook() {
        rateBookRates.value = [];
        rateBookName.value = '';
        header.value.rateBookId = undefined;
    }

    async function fetchEstimate(id: number) {
        isLoading.value = true;
        try {
            const { data } = await apiStore.api.get(`/api/v1/estimates/${id}`);
            const currentRev = (data.revisions ?? [])
                .filter((r: any) => r.isCurrent)
                .sort((a: any, b: any) => b.revisionNumber - a.revisionNumber)[0];
            header.value = {
                estimateId: data.estimateId,
                estimateNumber: data.estimateNumber,
                staffingPlanId: data.staffingPlanId,
                staffingPlanNumber: data.staffingPlan?.staffingPlanNumber ?? null,
                revisionNumber: currentRev?.revisionNumber ?? null,
                rateBookId: data.rateBookId ?? undefined,
                name: data.name,
                client: data.client,
                clientCode: data.clientCode,
                msaNumber: data.msaNumber,
                jobType: data.jobType,
                branch: data.branch,
                city: data.city,
                state: data.state,
                site: data.site,
                jobLetter: data.jobLetter,
                shift: data.shift,
                hoursPerShift: data.hoursPerShift,
                days: data.days,
                startDate: data.startDate?.slice(0, 10),
                endDate: data.endDate?.slice(0, 10),
                otMethod: data.otMethod,
                dtWeekends: data.dtWeekends,
                status: data.status,
                confidencePct: data.confidencePct,
                lostReason: data.lostReason,
                lostNotes: data.lostNotes,
                isScenario: data.isScenario ?? false,
                vp: data.vp ?? undefined,
                director: data.director ?? undefined,
                region: data.region ?? undefined,
            };
            if (data.rateBookId) {
                await loadRateBook(data.rateBookId);
            }
            laborRows.value = data.laborRows ?? [];
            equipmentRows.value = data.equipmentRows ?? [];
            expenseRows.value = data.expenseRows ?? [];
            summary.value = data.summary ?? defaultSummary();
            revisions.value = data.revisions ?? [];
            isDirty.value = false;
        } finally {
            isLoading.value = false;
        }
    }

    async function fetchNextNumber(jobLetter?: string, clientCode?: string): Promise<string> {
        const params = new URLSearchParams();
        if (jobLetter) params.set('jobLetter', jobLetter);
        if (clientCode) params.set('clientCode', clientCode);
        const { data } = await apiStore.api.get(`/api/v1/estimates/next-number?${params}`);
        return data.number;
    }

    async function saveEstimate(revisionDescription?: string): Promise<number> {
        isSaving.value = true;
        try {
            const id = header.value.estimateId;

            const headerPayload = {
                name: header.value.name,
                client: header.value.client,
                clientCode: header.value.clientCode,
                msaNumber: header.value.msaNumber,
                jobType: header.value.jobType,
                branch: header.value.branch,
                city: header.value.city,
                state: header.value.state,
                site: header.value.site,
                jobLetter: header.value.jobLetter,
                shift: header.value.shift,
                hoursPerShift: header.value.hoursPerShift,
                days: header.value.days,
                startDate: header.value.startDate || null,
                endDate: header.value.endDate || null,
                otMethod: header.value.otMethod,
                dtWeekends: header.value.dtWeekends,
                status: header.value.status,
                confidencePct: header.value.confidencePct,
                lostReason: header.value.lostReason,
                lostNotes: header.value.lostNotes,
                isScenario: header.value.isScenario ?? false,
                staffingPlanId: header.value.staffingPlanId,
                vp: header.value.vp,
                director: header.value.director,
                region: header.value.region,
                rateBookId: header.value.rateBookId ?? null,
            };

            let estimateId: number;

            if (!id) {
                const { data } = await apiStore.api.post('/api/v1/estimates', headerPayload);
                estimateId = data.estimateId;
                header.value.estimateId = estimateId;
                header.value.estimateNumber = data.estimateNumber;
            } else {
                await apiStore.api.put(`/api/v1/estimates/${id}`, headerPayload);
                estimateId = id;
            }

            // Save labor rows
            await apiStore.api.post(`/api/v1/estimates/${estimateId}/labor`, laborRows.value.map(r => ({
                position: r.position,
                laborType: r.laborType,
                shift: r.shift,
                craftCode: r.craftCode,
                navCode: r.navCode,
                billStRate: r.billStRate,
                billOtRate: r.billOtRate,
                billDtRate: r.billDtRate,
                scheduleJson: r.scheduleJson,
                stHours: r.stHours,
                otHours: r.otHours,
                dtHours: r.dtHours,
                subtotal: r.subtotal,
            })));

            // Save equipment rows
            await apiStore.api.post(`/api/v1/estimates/${estimateId}/equipment`, equipmentRows.value.map(r => ({
                name: r.name,
                rateType: r.rateType,
                rate: r.rate,
                qty: r.qty,
                days: r.days,
                subtotal: r.subtotal,
            })));

            // Save expense rows
            await apiStore.api.post(`/api/v1/estimates/${estimateId}/expenses`, expenseRows.value.map(r => ({
                category: r.category,
                description: r.description,
                rate: r.rate,
                unit: r.unit,
                daysOrQty: r.daysOrQty,
                people: r.people,
                billable: r.billable,
                subtotal: r.subtotal,
            })));

            // Save summary
            recalcSummary();
            await apiStore.api.put(`/api/v1/estimates/${estimateId}/summary`, summary.value);

            // Optional revision snapshot
            if (revisionDescription !== undefined) {
                await apiStore.api.post(`/api/v1/estimates/${estimateId}/revisions`, {
                    description: revisionDescription,
                });
            }

            isDirty.value = false;
            return estimateId;
        } finally {
            isSaving.value = false;
        }
    }

    async function deleteEstimate(id: number) {
        await apiStore.api.delete(`/api/v1/estimates/${id}`);
    }

    async function fetchRevisions(id: number) {
        const { data } = await apiStore.api.get(`/api/v1/estimates/${id}/revisions`);
        revisions.value = data;
    }

    function addLaborRow() {
        laborRows.value.push({
            position: '',
            laborType: 'Direct',
            shift: header.value.shift,
            billStRate: 0,
            billOtRate: 0,
            billDtRate: 0,
            stHours: 0,
            otHours: 0,
            dtHours: 0,
            subtotal: 0,
        });
        markDirty();
    }

    // Called by AI sidecar: adds qty copies of position+shift with rates from loaded rate book
    function addLaborRowFromAi(opts: {
        position: string;
        shift: string;
        qty: number;
        billStRate?: number;
        billOtRate?: number;
        billDtRate?: number;
    }) {
        const count = Math.max(1, opts.qty ?? 1);
        for (let i = 0; i < count; i++) {
            laborRows.value.push({
                position: opts.position,
                laborType: 'Direct',
                shift: opts.shift || header.value.shift,
                billStRate: opts.billStRate ?? 0,
                billOtRate: opts.billOtRate ?? 0,
                billDtRate: opts.billDtRate ?? 0,
                stHours: 0,
                otHours: 0,
                dtHours: 0,
                subtotal: 0,
            });
        }
        markDirty();
    }

    function removeLaborRow(index: number) {
        laborRows.value.splice(index, 1);
        recalcSummary();
        markDirty();
    }

    function addEquipmentRow() {
        equipmentRows.value.push({
            name: '',
            rateType: 'Daily',
            rate: 0,
            qty: 1,
            days: header.value.days || 1,
            subtotal: 0,
        });
        markDirty();
    }

    function removeEquipmentRow(index: number) {
        equipmentRows.value.splice(index, 1);
        recalcSummary();
        markDirty();
    }

    function addExpenseRow() {
        expenseRows.value.push({
            category: 'PerDiem',
            description: '',
            rate: 0,
            unit: 'Day',
            daysOrQty: header.value.days || 1,
            people: 1,
            billable: true,
            subtotal: 0,
        });
        markDirty();
    }

    function removeExpenseRow(index: number) {
        expenseRows.value.splice(index, 1);
        recalcSummary();
        markDirty();
    }

    return {
        header,
        laborRows,
        equipmentRows,
        expenseRows,
        summary,
        revisions,
        rateBookRates,
        rateBookName,
        isDirty,
        isLoading,
        isSaving,
        isNew,
        reset,
        markDirty,
        loadRateBook,
        clearRateBook,
        fetchEstimate,
        fetchNextNumber,
        saveEstimate,
        deleteEstimate,
        fetchRevisions,
        addLaborRow,
        addLaborRowFromAi,
        removeLaborRow,
        addEquipmentRow,
        removeEquipmentRow,
        addExpenseRow,
        removeExpenseRow,
        recalcLaborRow,
        recalcAllLaborRows,
        recalcEquipmentRow,
        recalcExpenseRow,
        recalcSummary,
    };
});
