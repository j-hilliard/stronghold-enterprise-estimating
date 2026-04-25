export type ForecastPeriod = 'month' | 'quarter' | 'year';

export interface EstimateListItem {
    estimateId: number;
    estimateNumber: string;
    name: string;
    client: string;
    status: string;
    branch?: string | null;
    city?: string | null;
    state?: string | null;
    startDate?: string | null;
    endDate?: string | null;
    confidencePct?: number | null;
    grandTotal?: number | null;
    updatedAt?: string | null;
}

export interface StaffingListItem {
    staffingPlanId: number;
    staffingPlanNumber: string;
    name: string;
    client: string;
    status: string;
    branch?: string | null;
    city?: string | null;
    state?: string | null;
    startDate?: string | null;
    endDate?: string | null;
    roughLaborTotal?: number | null;
    convertedEstimateId?: number | null;
    updatedAt?: string | null;
}

export interface RevenueRow {
    id: string;
    sourceType: 'estimate' | 'staffing';
    sourceId: number;
    number: string;
    name: string;
    client: string;
    status: string;
    startDate?: string | null;
    endDate?: string | null;
    bucketDate: string;
    confidencePct: number;
    baseAmount: number;
    weightedAmount: number;
    awardedAmount: number;
    pipelineAmount: number;
    staffingAmount: number;
    lostAmount: number;
    fcoAmount: number;
}

export interface RevenuePeriodRow {
    key: string;
    label: string;
    awardedAmount: number;
    pipelineAmount: number;
    staffingAmount: number;
    lostAmount: number;
    fcoAmount: number;
    totalForecast: number;
}

export interface LaborScheduleRow {
    position?: string | null;
    craftCode?: string | null;
    scheduleJson?: string | null;
}

export interface ManpowerMonthlyRow {
    monthKey: string;
    monthLabel: string;
    totalPeakHeadcount: number;
    baselineHeadcount: number;
    delta: number;
}

export interface CraftMonthlyDemandRow {
    craft: string;
    monthKey: string;
    peakHeadcount: number;
}

export function normalizeStatus(raw?: string | null): string {
    const normalized = (raw ?? '').trim().toLowerCase();
    if (!normalized) return 'Draft';
    if (normalized === 'proposed') return 'Pending';
    if (normalized === 'award' || normalized === 'won') return 'Awarded';
    if (normalized === 'cancelled') return 'Canceled';
    return normalized.charAt(0).toUpperCase() + normalized.slice(1);
}

export function clampPercent(value?: number | null, fallback = 50): number {
    const n = Number(value ?? fallback);
    if (Number.isNaN(n)) return fallback;
    return Math.max(0, Math.min(100, n));
}

export function asAmount(value?: number | null): number {
    const n = Number(value ?? 0);
    if (Number.isNaN(n)) return 0;
    return round2(n);
}

export function round2(value: number): number {
    return Math.round(value * 100) / 100;
}

export function safeDate(value?: string | null): Date | null {
    if (!value) return null;
    if (/^\d{4}-\d{2}-\d{2}$/.test(value)) {
        // Date-only values are local business dates, not UTC midnights.
        const d = new Date(`${value}T12:00:00`);
        if (!Number.isNaN(d.getTime())) return d;
    }
    const d = new Date(value);
    if (Number.isNaN(d.getTime())) return null;
    return d;
}

export function toIsoDate(value: Date): string {
    return value.toISOString().slice(0, 10);
}

export function monthKey(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    return `${y}-${m}`;
}

export function quarterKey(d: Date): string {
    const y = d.getFullYear();
    const q = Math.floor(d.getMonth() / 3) + 1;
    return `${y}-Q${q}`;
}

export function yearKey(d: Date): string {
    return `${d.getFullYear()}`;
}

export function formatMonthLabel(key: string): string {
    const [y, m] = key.split('-');
    const date = new Date(Number(y), Number(m) - 1, 1);
    return date.toLocaleDateString('en-US', { month: 'short', year: 'numeric' });
}

export function formatQuarterLabel(key: string): string {
    const [y, q] = key.split('-');
    return `${q} ${y}`;
}

export function formatYearLabel(key: string): string {
    return key;
}

export function estimateCategory(status: string): 'awarded' | 'pipeline' | 'lost' | 'excluded' {
    const s = normalizeStatus(status);
    if (s === 'Awarded') return 'awarded';
    if (s === 'Lost') return 'lost';
    if (s === 'Draft' || s === 'Pending' || s === 'Proposed') return 'pipeline';
    return 'excluded';
}

export function resolveEstimateConfidence(status: string, confidencePct?: number | null): number {
    const category = estimateCategory(status);
    if (category === 'awarded') return 100;
    if (category === 'lost') return 0;
    return clampPercent(confidencePct, 50);
}

export function resolveBucketDate(
    startDate?: string | null,
    endDate?: string | null,
    updatedAt?: string | null,
): Date {
    const s = safeDate(startDate);
    if (s) return s;
    const e = safeDate(endDate);
    if (e) return e;
    const u = safeDate(updatedAt);
    if (u) return u;
    return new Date();
}

export function withinRange(
    d: Date,
    fromDate?: Date | null,
    toDate?: Date | null,
): boolean {
    if (fromDate && d < fromDate) return false;
    if (toDate && d > toDate) return false;
    return true;
}

export function buildRevenueRows(
    estimates: EstimateListItem[],
    staffing: StaffingListItem[],
    estimateFcoById: Record<number, number>,
    fromDate?: Date | null,
    toDate?: Date | null,
): RevenueRow[] {
    const rows: RevenueRow[] = [];

    for (const e of estimates) {
        const bucket = resolveBucketDate(e.startDate, e.endDate, e.updatedAt);
        if (!withinRange(bucket, fromDate, toDate)) continue;

        const status = normalizeStatus(e.status);
        const base = asAmount(e.grandTotal);
        const fco = asAmount(estimateFcoById[e.estimateId]);
        const category = estimateCategory(status);
        const confidence = resolveEstimateConfidence(status, e.confidencePct);

        const awarded = category === 'awarded' ? base : 0;
        const lost = category === 'lost' ? base : 0;
        const pipeline = category === 'pipeline' ? round2(base * (confidence / 100)) : 0;

        rows.push({
            id: `estimate-${e.estimateId}`,
            sourceType: 'estimate',
            sourceId: e.estimateId,
            number: e.estimateNumber,
            name: e.name,
            client: e.client,
            status,
            startDate: e.startDate,
            endDate: e.endDate,
            bucketDate: toIsoDate(bucket),
            confidencePct: confidence,
            baseAmount: base,
            weightedAmount: round2(awarded + pipeline),
            awardedAmount: awarded,
            pipelineAmount: pipeline,
            staffingAmount: 0,
            lostAmount: lost,
            fcoAmount: fco,
        });
    }

    for (const sp of staffing) {
        // Converted plans are retained records but must be deduped from forecast.
        if (sp.convertedEstimateId) continue;

        const status = normalizeStatus(sp.status);
        if (status === 'Archived') continue;

        const bucket = resolveBucketDate(sp.startDate, sp.endDate, sp.updatedAt);
        if (!withinRange(bucket, fromDate, toDate)) continue;

        const base = asAmount(sp.roughLaborTotal);
        rows.push({
            id: `staffing-${sp.staffingPlanId}`,
            sourceType: 'staffing',
            sourceId: sp.staffingPlanId,
            number: sp.staffingPlanNumber,
            name: sp.name,
            client: sp.client,
            status,
            startDate: sp.startDate,
            endDate: sp.endDate,
            bucketDate: toIsoDate(bucket),
            confidencePct: 100,
            baseAmount: base,
            weightedAmount: base,
            awardedAmount: 0,
            pipelineAmount: 0,
            staffingAmount: base,
            lostAmount: 0,
            fcoAmount: 0,
        });
    }

    return rows.sort((a, b) => a.bucketDate.localeCompare(b.bucketDate));
}

export function groupRevenueByPeriod(rows: RevenueRow[], period: ForecastPeriod): RevenuePeriodRow[] {
    const byKey = new Map<string, RevenuePeriodRow>();

    for (const row of rows) {
        const d = safeDate(row.bucketDate) ?? new Date(row.bucketDate);
        if (Number.isNaN(d.getTime())) continue;

        const key = period === 'month' ? monthKey(d) : period === 'quarter' ? quarterKey(d) : yearKey(d);
        const label = period === 'month' ? formatMonthLabel(key) : period === 'quarter' ? formatQuarterLabel(key) : formatYearLabel(key);

        const existing = byKey.get(key) ?? {
            key,
            label,
            awardedAmount: 0,
            pipelineAmount: 0,
            staffingAmount: 0,
            lostAmount: 0,
            fcoAmount: 0,
            totalForecast: 0,
        };

        existing.awardedAmount = round2(existing.awardedAmount + row.awardedAmount);
        existing.pipelineAmount = round2(existing.pipelineAmount + row.pipelineAmount);
        existing.staffingAmount = round2(existing.staffingAmount + row.staffingAmount);
        existing.lostAmount = round2(existing.lostAmount + row.lostAmount);
        existing.fcoAmount = round2(existing.fcoAmount + row.fcoAmount);
        existing.totalForecast = round2(
            existing.awardedAmount + existing.pipelineAmount + existing.staffingAmount + existing.fcoAmount,
        );

        byKey.set(key, existing);
    }

    return [...byKey.values()].sort((a, b) => a.key.localeCompare(b.key));
}

export function parseScheduleJson(scheduleJson?: string | null): Record<string, number> {
    if (!scheduleJson) return {};
    try {
        const parsed = JSON.parse(scheduleJson) as Record<string, unknown>;
        const out: Record<string, number> = {};
        for (const [k, v] of Object.entries(parsed)) {
            const n = Number(v);
            if (!Number.isNaN(n) && n > 0) out[k] = n;
        }
        return out;
    } catch {
        return {};
    }
}

export function normalizeCraft(row: LaborScheduleRow): string {
    const position = row.position?.trim();
    if (position) return position;
    const craft = row.craftCode?.trim();
    if (craft) return craft.toUpperCase();
    return 'UNSPECIFIED';
}

export function scheduleDateInRange(dateKey: string, fromDate: Date, toDate: Date): boolean {
    const d = safeDate(dateKey);
    if (!d) return false;
    return d >= fromDate && d <= toDate;
}

export function computeManpowerDemand(
    rows: Array<{
        sourceType: 'estimate' | 'staffing';
        sourceStatus: string;
        rows: LaborScheduleRow[];
    }>,
    options: {
        fromDate: Date;
        toDate: Date;
        includePendingEstimates: boolean;
        includeStaffingPlans: boolean;
    },
): {
    monthRows: ManpowerMonthlyRow[];
    craftMonthlyRows: CraftMonthlyDemandRow[];
    currentByCraft: Record<string, number>;
    currentTotal: number;
    peakMonth?: string;
    peakMonthHeadcount: number;
} {
    const dailyTotalByDate = new Map<string, number>();
    const dailyCraftByDate = new Map<string, Map<string, number>>();
    const currentByCraft: Record<string, number> = {};

    const today = toIsoDate(new Date());

    const shouldIncludeSource = (sourceType: 'estimate' | 'staffing', status: string) => {
        const normalized = normalizeStatus(status);
        if (sourceType === 'estimate') {
            if (normalized === 'Awarded') return true;
            if (normalized === 'Draft' || normalized === 'Pending' || normalized === 'Proposed') {
                return options.includePendingEstimates;
            }
            return false;
        }

        if (normalized === 'Archived' || normalized === 'Converted') return false;
        return options.includeStaffingPlans;
    };

    for (const source of rows) {
        if (!shouldIncludeSource(source.sourceType, source.sourceStatus)) continue;

        for (const row of source.rows) {
            const craft = normalizeCraft(row);
            const schedule = parseScheduleJson(row.scheduleJson);

            for (const [dateKey, headcount] of Object.entries(schedule)) {
                if (!scheduleDateInRange(dateKey, options.fromDate, options.toDate)) continue;

                const dailyTotal = (dailyTotalByDate.get(dateKey) ?? 0) + headcount;
                dailyTotalByDate.set(dateKey, dailyTotal);

                const craftMap = dailyCraftByDate.get(dateKey) ?? new Map<string, number>();
                craftMap.set(craft, (craftMap.get(craft) ?? 0) + headcount);
                dailyCraftByDate.set(dateKey, craftMap);

                if (dateKey === today) {
                    currentByCraft[craft] = round2((currentByCraft[craft] ?? 0) + headcount);
                }
            }
        }
    }

    const monthTotalPeak = new Map<string, number>();
    const monthCraftPeak = new Map<string, Map<string, number>>();

    for (const [dateKey, totalHeadcount] of dailyTotalByDate.entries()) {
        const d = safeDate(dateKey);
        if (!d) continue;
        const mk = monthKey(d);
        monthTotalPeak.set(mk, Math.max(monthTotalPeak.get(mk) ?? 0, totalHeadcount));

        const craftMap = dailyCraftByDate.get(dateKey) ?? new Map<string, number>();
        const monthCraft = monthCraftPeak.get(mk) ?? new Map<string, number>();
        for (const [craft, count] of craftMap.entries()) {
            monthCraft.set(craft, Math.max(monthCraft.get(craft) ?? 0, count));
        }
        monthCraftPeak.set(mk, monthCraft);
    }

    const baselineTotal = Object.values(currentByCraft).reduce((sum, val) => sum + val, 0);
    const monthRows: ManpowerMonthlyRow[] = [...monthTotalPeak.entries()]
        .sort((a, b) => a[0].localeCompare(b[0]))
        .map(([mk, peak]) => ({
            monthKey: mk,
            monthLabel: formatMonthLabel(mk),
            totalPeakHeadcount: round2(peak),
            baselineHeadcount: round2(baselineTotal),
            delta: round2(peak - baselineTotal),
        }));

    const craftMonthlyRows: CraftMonthlyDemandRow[] = [];
    for (const [mk, craftMap] of monthCraftPeak.entries()) {
        for (const [craft, peak] of craftMap.entries()) {
            craftMonthlyRows.push({
                craft,
                monthKey: mk,
                peakHeadcount: round2(peak),
            });
        }
    }

    craftMonthlyRows.sort((a, b) => {
        const monthCompare = a.monthKey.localeCompare(b.monthKey);
        if (monthCompare !== 0) return monthCompare;
        return a.craft.localeCompare(b.craft);
    });

    const peakMonthRow = [...monthRows].sort((a, b) => b.totalPeakHeadcount - a.totalPeakHeadcount)[0];
    const currentTotal = round2(Object.values(currentByCraft).reduce((sum, val) => sum + val, 0));

    return {
        monthRows,
        craftMonthlyRows,
        currentByCraft,
        currentTotal,
        peakMonth: peakMonthRow?.monthLabel,
        peakMonthHeadcount: peakMonthRow?.totalPeakHeadcount ?? 0,
    };
}
