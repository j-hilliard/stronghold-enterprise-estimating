/**
 * useOtCalculator — Pure OT/DT calculation engine
 *
 * Matches legacy calculations.js logic exactly.
 * Per-person-slot tracking: headcount changes mid-week don't inflate OT for existing workers.
 *
 * OT Methods:
 *   daily8          — OT after 8 hrs/day
 *   daily8_weekly40 — higher of daily-8 OT or weekly-40 OT
 *   weekly40        — OT after 40 cumulative hrs/week
 *   california      — ST ≤ 8, OT 8–12, DT > 12 per day
 *
 * Weekend rules:
 *   Sundays always DT
 *   Saturdays DT when dtWeekends = true
 */

export interface OtResult {
    stHours: number;
    otHours: number;
    dtHours: number;
}

/** Returns the ISO Monday date string for the week containing `d` */
function getMondayKey(d: Date): string {
    const copy = new Date(d);
    const dow = copy.getDay(); // 0 = Sun
    const offset = dow === 0 ? -6 : 1 - dow;
    copy.setDate(copy.getDate() + offset);
    return copy.toISOString().slice(0, 10);
}

interface Slot {
    weeklyHours: number;
}

function calcDayHours(
    hrs: number,
    otMethod: string,
    dtWeekends: string,
    dow: number,          // 0 = Sun, 6 = Sat
    weeklyHours: number,  // hours accumulated this week BEFORE this day
): OtResult {
    // Sundays DT only when setting includes Sunday
    if (dow === 0 && (dtWeekends === 'sun_only' || dtWeekends === 'sat_sun'))
        return { stHours: 0, otHours: 0, dtHours: hrs };
    // Saturdays DT only when sat_sun
    if (dow === 6 && dtWeekends === 'sat_sun')
        return { stHours: 0, otHours: 0, dtHours: hrs };

    switch (otMethod) {
        case 'daily8': {
            const st = Math.min(hrs, 8);
            const ot = Math.max(0, hrs - 8);
            return { stHours: st, otHours: ot, dtHours: 0 };
        }
        case 'california': {
            const st = Math.min(hrs, 8);
            const ot = Math.min(Math.max(0, hrs - 8), 4);
            const dt = Math.max(0, hrs - 12);
            return { stHours: st, otHours: ot, dtHours: dt };
        }
        case 'weekly40': {
            const stAvail = Math.max(0, 40 - weeklyHours);
            const st = Math.min(hrs, stAvail);
            const ot = hrs - st;
            return { stHours: st, otHours: ot, dtHours: 0 };
        }
        case 'daily8_weekly40': {
            // Higher of daily-8 OT or weekly-40 OT
            const daily8Ot = Math.max(0, hrs - 8);
            const w40Avail = Math.max(0, 40 - weeklyHours);
            const w40Ot = hrs - Math.min(hrs, w40Avail);
            const finalOt = Math.max(daily8Ot, w40Ot);
            const finalSt = hrs - finalOt;
            return { stHours: finalSt, otHours: finalOt, dtHours: 0 };
        }
        default: // no OT method / straight time
            return { stHours: hrs, otHours: 0, dtHours: 0 };
    }
}

export function calcOtHours(
    scheduleJson: string | null | undefined,
    hoursPerShift: number,
    otMethod: string,
    dtWeekends: string,
): OtResult {
    if (!scheduleJson) return { stHours: 0, otHours: 0, dtHours: 0 };

    let schedule: Record<string, number>;
    try {
        schedule = JSON.parse(scheduleJson);
    } catch {
        return { stHours: 0, otHours: 0, dtHours: 0 };
    }

    const dates = Object.keys(schedule).sort();
    if (dates.length === 0) return { stHours: 0, otHours: 0, dtHours: 0 };

    const slots: Slot[] = [];
    let totalSt = 0, totalOt = 0, totalDt = 0;
    let currentWeekKey = '';

    for (const dateStr of dates) {
        const headcount = Math.max(0, Math.round(schedule[dateStr] ?? 0));
        const date = new Date(dateStr + 'T12:00:00');
        const dow = date.getDay();

        // Reset weekly hours on new week
        const weekKey = getMondayKey(date);
        if (weekKey !== currentWeekKey) {
            currentWeekKey = weekKey;
            for (const slot of slots) slot.weeklyHours = 0;
        }

        // Adjust active slots — LIFO removal
        while (slots.length < headcount) slots.push({ weeklyHours: 0 });
        if (slots.length > headcount) slots.splice(headcount);

        // Calculate per-slot
        for (const slot of slots) {
            const result = calcDayHours(hoursPerShift, otMethod, dtWeekends, dow, slot.weeklyHours);
            totalSt += result.stHours;
            totalOt += result.otHours;
            totalDt += result.dtHours;
            slot.weeklyHours += result.stHours + result.otHours + result.dtHours;
        }
    }

    return {
        stHours: Math.round(totalSt * 100) / 100,
        otHours: Math.round(totalOt * 100) / 100,
        dtHours: Math.round(totalDt * 100) / 100,
    };
}

/** Sum all headcount across schedule days → total person-days */
export function totalDays(scheduleJson: string | null | undefined): number {
    if (!scheduleJson) return 0;
    try {
        const s = JSON.parse(scheduleJson) as Record<string, number>;
        return Object.keys(s).length;
    } catch {
        return 0;
    }
}
