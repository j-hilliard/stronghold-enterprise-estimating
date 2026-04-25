import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import { useApiStore } from '@/stores/apiStore';
import { calcOtHours } from '../composables/useOtCalculator';
import type { LaborRow, RateBookRate } from './estimateStore';

export interface StaffingPlanHeader {
    staffingPlanId?: number;
    staffingPlanNumber?: string;
    convertedEstimateId?: number | null;
    name: string;
    client: string;
    clientCode?: string;
    branch?: string;
    city?: string;
    state?: string;
    jobLetter?: string;
    status: string;
    shift: string;
    hoursPerShift: number;
    days: number;
    startDate?: string;
    endDate?: string;
    otMethod: string;
    dtWeekends: boolean;
    roughLaborTotal?: number;
}

export function defaultSpHeader(): StaffingPlanHeader {
    return {
        name: '',
        client: '',
        shift: 'Day',
        hoursPerShift: 10,
        days: 0,
        status: 'Draft',
        otMethod: 'daily8',
        dtWeekends: false,
    };
}

export const useStaffingStore = defineStore('staffing', () => {
    const apiStore = useApiStore();

    const header = ref<StaffingPlanHeader>(defaultSpHeader());
    const laborRows = ref<LaborRow[]>([]);
    const rateBookRates = ref<RateBookRate[]>([]);
    const rateBookName = ref<string>('');
    const isDirty = ref(false);
    const isLoading = ref(false);
    const isSaving = ref(false);

    const isNew = computed(() => !header.value.staffingPlanId);
    const isConverted = computed(() => !!header.value.convertedEstimateId);

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

    function reset() {
        header.value = defaultSpHeader();
        laborRows.value = [];
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
    }

    function clearRateBook() {
        rateBookRates.value = [];
        rateBookName.value = '';
    }

    async function fetchPlan(id: number) {
        isLoading.value = true;
        try {
            const { data } = await apiStore.api.get(`/api/v1/staffing-plans/${id}`);
            header.value = {
                staffingPlanId: data.staffingPlanId,
                staffingPlanNumber: data.staffingPlanNumber,
                convertedEstimateId: data.convertedEstimateId,
                name: data.name,
                client: data.client,
                clientCode: data.clientCode,
                branch: data.branch,
                city: data.city,
                state: data.state,
                jobLetter: data.jobLetter,
                status: data.status,
                shift: data.shift,
                hoursPerShift: data.hoursPerShift,
                days: data.days,
                startDate: data.startDate?.slice(0, 10),
                endDate: data.endDate?.slice(0, 10),
                otMethod: data.otMethod,
                dtWeekends: data.dtWeekends,
                roughLaborTotal: data.roughLaborTotal,
            };
            laborRows.value = data.laborRows?.map((r: any) => ({
                ...r,
                billStRate: r.stRate ?? 0,
                billOtRate: r.otRate ?? 0,
                billDtRate: r.dtRate ?? 0,
            })) ?? [];
            isDirty.value = false;
        } finally {
            isLoading.value = false;
        }
    }

    async function savePlan(): Promise<number> {
        isSaving.value = true;
        try {
            const id = header.value.staffingPlanId;
            const payload = {
                name: header.value.name,
                client: header.value.client,
                clientCode: header.value.clientCode,
                branch: header.value.branch,
                city: header.value.city,
                state: header.value.state,
                jobLetter: header.value.jobLetter,
                status: header.value.status,
                shift: header.value.shift,
                hoursPerShift: header.value.hoursPerShift,
                days: header.value.days,
                startDate: header.value.startDate || null,
                endDate: header.value.endDate || null,
                otMethod: header.value.otMethod,
                dtWeekends: header.value.dtWeekends,
            };

            let planId: number;
            if (!id) {
                const { data } = await apiStore.api.post('/api/v1/staffing-plans', payload);
                planId = data.staffingPlanId;
                header.value.staffingPlanId = planId;
                header.value.staffingPlanNumber = data.staffingPlanNumber;
            } else {
                await apiStore.api.put(`/api/v1/staffing-plans/${id}`, payload);
                planId = id;
            }

            await apiStore.api.post(`/api/v1/staffing-plans/${planId}/labor`, laborRows.value.map(r => ({
                position: r.position,
                laborType: r.laborType,
                shift: r.shift,
                craftCode: r.craftCode,
                navCode: r.navCode,
                stRate: r.billStRate,
                otRate: r.billOtRate,
                dtRate: r.billDtRate,
                scheduleJson: r.scheduleJson,
                stHours: r.stHours,
                otHours: r.otHours,
                dtHours: r.dtHours,
                subtotal: r.subtotal,
            })));

            isDirty.value = false;
            return planId;
        } finally {
            isSaving.value = false;
        }
    }

    async function convertToEstimate(): Promise<{ estimateId: number; estimateNumber: string }> {
        const id = header.value.staffingPlanId!;
        const { data } = await apiStore.api.post(`/api/v1/staffing-plans/${id}/convert`);
        header.value.convertedEstimateId = data.estimateId;
        header.value.status = 'Converted';
        return data;
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

    function addLaborRowFromAi(opts: { position: string; shift: string; qty: number }) {
        const count = Math.max(1, opts.qty ?? 1);
        for (let i = 0; i < count; i++) {
            laborRows.value.push({
                position: opts.position,
                laborType: 'Direct',
                shift: opts.shift || header.value.shift,
                billStRate: 0,
                billOtRate: 0,
                billDtRate: 0,
                stHours: 0,
                otHours: 0,
                dtHours: 0,
                subtotal: 0,
            });
        }
        markDirty();
    }

    function removeLaborRow(idx: number) {
        laborRows.value.splice(idx, 1);
        markDirty();
    }

    return {
        header, laborRows, rateBookRates, rateBookName,
        isDirty, isLoading, isSaving, isNew, isConverted,
        reset, markDirty, fetchPlan, savePlan, convertToEstimate,
        loadRateBook, clearRateBook,
        addLaborRow, addLaborRowFromAi, removeLaborRow, recalcLaborRow,
    };
});
