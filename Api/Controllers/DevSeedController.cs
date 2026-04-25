using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Api.Services;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Data.Models;
using System.Text.Json;

namespace Stronghold.EnterpriseEstimating.Api.Controllers;

/// <summary>
/// Development-only endpoints for resetting and seeding test data.
/// Only available in Development environment.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dev")]
[AllowAnonymous]
public class DevSeedController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly EstimateNumberService _numberService;

    public DevSeedController(
        IWebHostEnvironment env,
        IDbContextFactory<AppDbContext> dbFactory,
        EstimateNumberService numberService)
    {
        _env = env;
        _dbFactory = dbFactory;
        _numberService = numberService;
    }

    // ── POST /api/v1/dev/reset ────────────────────────────────────────────────

    [HttpPost("reset")]
    public async Task<IActionResult> Reset(CancellationToken ct = default)
    {
        if (!_env.IsDevelopment() && !_env.IsEnvironment("Local"))
            return StatusCode(403, new { message = "Reset endpoint is only available in Development environment." });

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Null out circular cross-references first
        await db.Estimates
            .Where(e => e.CompanyCode == "CSL" && e.StaffingPlanId != null)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.StaffingPlanId, (int?)null), ct);

        await db.StaffingPlans
            .Where(p => p.CompanyCode == "CSL" && p.ConvertedEstimateId != null)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ConvertedEstimateId, (int?)null), ct);

        // Delete child tables first (cascades not always reliable across ExecuteDelete chains)
        var estimateIds = await db.Estimates.Where(e => e.CompanyCode == "CSL").Select(e => e.EstimateId).ToListAsync(ct);
        if (estimateIds.Count > 0)
        {
            await db.EstimateRevisions.Where(r => estimateIds.Contains(r.EstimateId)).ExecuteDeleteAsync(ct);
            await db.EstimateSummaries.Where(s => estimateIds.Contains(s.EstimateId)).ExecuteDeleteAsync(ct);
            await db.LaborRows.Where(r => estimateIds.Contains(r.EstimateId)).ExecuteDeleteAsync(ct);
            await db.EquipmentRows.Where(r => estimateIds.Contains(r.EstimateId)).ExecuteDeleteAsync(ct);
            await db.ExpenseRows.Where(r => estimateIds.Contains(r.EstimateId)).ExecuteDeleteAsync(ct);
        }
        var deletedEstimates = await db.Estimates.Where(e => e.CompanyCode == "CSL").ExecuteDeleteAsync(ct);

        var spIds = await db.StaffingPlans.Where(p => p.CompanyCode == "CSL").Select(p => p.StaffingPlanId).ToListAsync(ct);
        if (spIds.Count > 0)
            await db.StaffingLaborRows.Where(r => spIds.Contains(r.StaffingPlanId)).ExecuteDeleteAsync(ct);
        var deletedPlans = await db.StaffingPlans.Where(p => p.CompanyCode == "CSL").ExecuteDeleteAsync(ct);

        var deletedTemplates = await db.CrewTemplates.Where(t => t.CompanyCode == "CSL").ExecuteDeleteAsync(ct);

        var rbIds = await db.RateBooks.Where(r => r.CompanyCode == "CSL").Select(r => r.RateBookId).ToListAsync(ct);
        if (rbIds.Count > 0)
        {
            await db.RateBookLaborRates.Where(r => rbIds.Contains(r.RateBookId)).ExecuteDeleteAsync(ct);
            await db.RateBookEquipmentRates.Where(r => rbIds.Contains(r.RateBookId)).ExecuteDeleteAsync(ct);
            await db.RateBookExpenseItems.Where(r => rbIds.Contains(r.RateBookId)).ExecuteDeleteAsync(ct);
        }
        var deletedRateBooks = await db.RateBooks.Where(r => r.CompanyCode == "CSL").ExecuteDeleteAsync(ct);

        var cbIds = await db.CostBooks.Where(c => c.CompanyCode == "CSL").Select(c => c.CostBookId).ToListAsync(ct);
        if (cbIds.Count > 0)
        {
            await db.CostBookLaborRates.Where(r => cbIds.Contains(r.CostBookId)).ExecuteDeleteAsync(ct);
            await db.CostBookEquipmentRates.Where(r => cbIds.Contains(r.CostBookId)).ExecuteDeleteAsync(ct);
            await db.CostBookExpenses.Where(r => cbIds.Contains(r.CostBookId)).ExecuteDeleteAsync(ct);
            await db.CostBookOverheadItems.Where(r => cbIds.Contains(r.CostBookId)).ExecuteDeleteAsync(ct);
        }
        var deletedCostBooks = await db.CostBooks.Where(c => c.CompanyCode == "CSL").ExecuteDeleteAsync(ct);

        return Ok(new
        {
            message = "All CSL dev data deleted.",
            estimates = deletedEstimates,
            staffingPlans = deletedPlans,
            crewTemplates = deletedTemplates,
            rateBooks = deletedRateBooks,
            costBooks = deletedCostBooks
        });
    }

    // ── POST /api/v1/dev/seed ─────────────────────────────────────────────────

    [HttpPost("seed")]
    public async Task<IActionResult> Seed(CancellationToken ct = default)
    {
        if (!_env.IsDevelopment() && !_env.IsEnvironment("Local"))
            return StatusCode(403, new { message = "Seed endpoint is only available in Development environment." });

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (await db.RateBooks.AnyAsync(r => r.CompanyCode == "CSL" && r.Name == "Standard (Baseline)", ct))
            return Conflict(new { message = "Dev seed data already exists. Use the Reset button first." });

        var positions = BasePositions();
        var now = DateTimeOffset.UtcNow;

        // ── 1. RATE BOOKS ─────────────────────────────────────────────────────

        var standardRb = MakeRateBook("Standard (Baseline)", null, "STD", null, null, true, null, positions, 1.0m, now, true);
        var vccRb      = MakeRateBook("Valero Corpus Christi", "Valero Energy Corporation", "VCC", "Corpus Christi", "TX", false, null, positions, 1.0m, now);
        var vpaRb      = MakeRateBook("Valero Port Arthur", "Valero Energy Corporation", "VPA", "Port Arthur", "TX", false, null, positions, 1.03m, now);
        var btxRb      = MakeRateBook("BP Texas City", "BP America Inc.", "BTX", "Texas City", "TX", false, null, positions, 1.06m, now);
        var sdpRb      = MakeRateBook("Shell Deer Park", "Shell Oil Company", "SDP", "Deer Park", "TX", false, null, positions, 1.04m, now);
        var exbRb      = MakeRateBook("ExxonMobil Baytown", "ExxonMobil Corporation", "EXB", "Baytown", "TX", false, null, positions, 1.05m, now);
        var lccRb      = MakeRateBook("LyondellBasell Channelview", "LyondellBasell Industries", "LCC", "Channelview", "TX", false, null, positions, 1.02m, now);
        var berRb      = MakeRateBook("BP El Reno", "BP America Inc.", "BER", "El Reno", "OK", false, new DateTime(2026, 2, 27), positions, 0.95m, now);

        db.RateBooks.AddRange(standardRb, vccRb, vpaRb, btxRb, sdpRb, exbRb, lccRb, berRb);

        // ── 2. COST BOOK ──────────────────────────────────────────────────────

        var costBook = new CostBook
        {
            CompanyCode = "CSL",
            Name = "Default Cost Book",
            IsDefault = true,
            UpdatedAt = now,
            OverheadItems = CbOverhead(),
            LaborRates = CbLaborRates(),
            EquipmentRates = CbEquipmentRates(),
            Expenses = CbExpenses()
        };
        db.CostBooks.Add(costBook);

        // ── 3. CREW TEMPLATES ─────────────────────────────────────────────────

        var t1 = new CrewTemplate
        {
            CompanyCode = "CSL", Name = "Standard 8-Man Piping Crew",
            Description = "Standard day shift piping crew for routine maintenance",
            CreatedBy = "seed", CreatedAt = now,
            Rows = new List<CrewTemplateRow>
            {
                new() { Position="Foreman",               LaborType="Indirect", CraftCode="SUP", Qty=1, Shift="Day", SortOrder=0 },
                new() { Position="Pipefitter Journeyman", LaborType="Direct",   CraftCode="PF",  Qty=4, Shift="Day", SortOrder=1 },
                new() { Position="Pipefitter Helper",     LaborType="Direct",   CraftCode="PFH", Qty=2, Shift="Day", SortOrder=2 },
                new() { Position="Safety Watch",          LaborType="Indirect", CraftCode="SAF", Qty=1, Shift="Day", SortOrder=3 },
            }
        };

        var t2 = new CrewTemplate
        {
            CompanyCode = "CSL", Name = "Full T/A Package",
            Description = "Complete turnaround crew — day shift — all disciplines",
            CreatedBy = "seed", CreatedAt = now,
            Rows = new List<CrewTemplateRow>
            {
                new() { Position="Project Manager",        LaborType="Indirect", CraftCode="MGT", Qty=1,  Shift="Day", SortOrder=0 },
                new() { Position="General Foreman",        LaborType="Indirect", CraftCode="SUP", Qty=1,  Shift="Day", SortOrder=1 },
                new() { Position="Foreman",                LaborType="Indirect", CraftCode="SUP", Qty=2,  Shift="Day", SortOrder=2 },
                new() { Position="Pipefitter Journeyman",  LaborType="Direct",   CraftCode="PF",  Qty=6,  Shift="Day", SortOrder=3 },
                new() { Position="Pipefitter Helper",      LaborType="Direct",   CraftCode="PFH", Qty=4,  Shift="Day", SortOrder=4 },
                new() { Position="Welder Journeyman",      LaborType="Direct",   CraftCode="WD",  Qty=2,  Shift="Day", SortOrder=5 },
                new() { Position="Welder Helper",          LaborType="Direct",   CraftCode="WDH", Qty=2,  Shift="Day", SortOrder=6 },
                new() { Position="NDT Technician",         LaborType="Direct",   CraftCode="NDT", Qty=1,  Shift="Day", SortOrder=7 },
                new() { Position="Crane Operator",         LaborType="Direct",   CraftCode="OPR", Qty=1,  Shift="Day", SortOrder=8 },
                new() { Position="Safety Watch",           LaborType="Indirect", CraftCode="SAF", Qty=2,  Shift="Day", SortOrder=9 },
                new() { Position="Fire Watch",             LaborType="Indirect", CraftCode="SAF", Qty=1,  Shift="Day", SortOrder=10 },
            }
        };

        db.CrewTemplates.AddRange(t1, t2);
        await db.SaveChangesAsync(ct);

        // ── 4. ESTIMATES ──────────────────────────────────────────────────────

        var e1Start = new DateTime(2026, 4, 1);
        var e1Num   = await _numberService.NextEstimateNumberAsync("CSL", null, "VCC", ct);
        var e1 = new Estimate
        {
            CompanyCode = "CSL", EstimateNumber = e1Num,
            Name = "Valero CC Spring T/A 2026", Client = "Valero Energy Corporation",
            ClientCode = "VCC", JobType = "T&M", Branch = "100",
            City = "Corpus Christi", State = "TX",
            Shift = "Day", HoursPerShift = 10, Days = 14,
            StartDate = e1Start, EndDate = e1Start.AddDays(13),
            OtMethod = "daily8_weekly40", DtWeekends = false,
            Status = "Awarded", ConfidencePct = 100,
            CreatedBy = "seed", CreatedAt = now, UpdatedAt = now
        };

        var e2Start = new DateTime(2026, 5, 1);
        var e2Num   = await _numberService.NextEstimateNumberAsync("CSL", null, "BTX", ct);
        var e2 = new Estimate
        {
            CompanyCode = "CSL", EstimateNumber = e2Num,
            Name = "BP Texas City Q2 Maintenance", Client = "BP America Inc.",
            ClientCode = "BTX", JobType = "T&M", Branch = "100",
            City = "Texas City", State = "TX",
            Shift = "Both", HoursPerShift = 10, Days = 21,
            StartDate = e2Start, EndDate = e2Start.AddDays(20),
            OtMethod = "daily8_weekly40", DtWeekends = false,
            Status = "Pending", ConfidencePct = 65,
            CreatedBy = "seed", CreatedAt = now, UpdatedAt = now
        };

        var e3Start = new DateTime(2026, 6, 1);
        var e3Num   = await _numberService.NextEstimateNumberAsync("CSL", null, "SDP", ct);
        var e3 = new Estimate
        {
            CompanyCode = "CSL", EstimateNumber = e3Num,
            Name = "Shell Deer Park Inspection 2026", Client = "Shell Oil Company",
            ClientCode = "SDP", JobType = "Inspection", Branch = "100",
            City = "Deer Park", State = "TX",
            Shift = "Day", HoursPerShift = 10, Days = 7,
            StartDate = e3Start, EndDate = e3Start.AddDays(6),
            OtMethod = "daily8_weekly40", DtWeekends = false,
            Status = "Awarded", ConfidencePct = 100,
            CreatedBy = "seed", CreatedAt = now, UpdatedAt = now
        };

        var e4Start = new DateTime(2026, 3, 15);
        var e4Num   = await _numberService.NextEstimateNumberAsync("CSL", null, "EXB", ct);
        var e4 = new Estimate
        {
            CompanyCode = "CSL", EstimateNumber = e4Num,
            Name = "ExxonMobil Baytown Piping Repair", Client = "ExxonMobil Corporation",
            ClientCode = "EXB", JobType = "Repair", Branch = "100",
            City = "Baytown", State = "TX",
            Shift = "Day", HoursPerShift = 10, Days = 10,
            StartDate = e4Start, EndDate = e4Start.AddDays(9),
            OtMethod = "daily8_weekly40", DtWeekends = false,
            Status = "Lost", ConfidencePct = 0,
            LostReason = "Price — competitor underbid by ~8%",
            CreatedBy = "seed", CreatedAt = now, UpdatedAt = now
        };

        var e5Start = new DateTime(2026, 7, 1);
        var e5Num   = await _numberService.NextEstimateNumberAsync("CSL", null, "LCC", ct);
        var e5 = new Estimate
        {
            CompanyCode = "CSL", EstimateNumber = e5Num,
            Name = "LyondellBasell Channelview Outage", Client = "LyondellBasell Industries",
            ClientCode = "LCC", JobType = "T/A", Branch = "100",
            City = "Channelview", State = "TX",
            Shift = "Both", HoursPerShift = 10, Days = 28,
            StartDate = e5Start, EndDate = e5Start.AddDays(27),
            OtMethod = "daily8_weekly40", DtWeekends = false,
            Status = "Draft", ConfidencePct = 40,
            CreatedBy = "seed", CreatedAt = now, UpdatedAt = now
        };

        db.Estimates.AddRange(e1, e2, e3, e4, e5);
        await db.SaveChangesAsync(ct);

        // ── 5. LABOR ROWS ─────────────────────────────────────────────────────

        // E1 — Valero CC, VCC rates (Standard × 1.0)
        var e1Rows = BuildLaborRows(e1.EstimateId, e1Start, e1.Days, 1.0m,
            ("Project Manager",       "Indirect", "Day", 1),
            ("General Foreman",       "Indirect", "Day", 1),
            ("Foreman",               "Indirect", "Day", 2),
            ("Pipefitter Journeyman", "Direct",   "Day", 6),
            ("Pipefitter Helper",     "Direct",   "Day", 4),
            ("Welder Journeyman",     "Direct",   "Day", 2),
            ("Welder Helper",         "Direct",   "Day", 2),
            ("Safety Watch",          "Indirect", "Day", 1),
            ("Fire Watch",            "Indirect", "Day", 1));
        db.LaborRows.AddRange(e1Rows);

        // E2 — BP Texas City, BTX rates (Standard × 1.06)
        var e2DayRows = BuildLaborRows(e2.EstimateId, e2Start, e2.Days, 1.06m,
            ("Project Manager",       "Indirect", "Day",   1),
            ("General Foreman",       "Indirect", "Day",   1),
            ("Foreman",               "Indirect", "Day",   2),
            ("Pipefitter Journeyman", "Direct",   "Day",   4),
            ("Pipefitter Helper",     "Direct",   "Day",   2),
            ("Welder Journeyman",     "Direct",   "Day",   2),
            ("Safety Watch",          "Indirect", "Day",   1));
        var e2NightRows = BuildLaborRows(e2.EstimateId, e2Start, e2.Days, 1.06m,
            ("Foreman",               "Indirect", "Night", 1),
            ("Pipefitter Journeyman", "Direct",   "Night", 4),
            ("Pipefitter Helper",     "Direct",   "Night", 2),
            ("Welder Journeyman",     "Direct",   "Night", 1),
            ("Fire Watch",            "Indirect", "Night", 1));
        // Fix sort order for night rows
        for (int i = 0; i < e2NightRows.Count; i++) e2NightRows[i].SortOrder = e2DayRows.Count + i;
        db.LaborRows.AddRange(e2DayRows);
        db.LaborRows.AddRange(e2NightRows);

        // E3 — Shell Deer Park, SDP rates (Standard × 1.04)
        var e3Rows = BuildLaborRows(e3.EstimateId, e3Start, e3.Days, 1.04m,
            ("Project Manager",       "Indirect", "Day", 1),
            ("Foreman",               "Indirect", "Day", 1),
            ("Pipefitter Journeyman", "Direct",   "Day", 3),
            ("NDT Technician",        "Direct",   "Day", 1),
            ("Instrument Tech",       "Direct",   "Day", 1),
            ("Safety Watch",          "Indirect", "Day", 1));
        db.LaborRows.AddRange(e3Rows);

        // E4 — Exxon Baytown, EXB rates (Standard × 1.05)
        var e4Rows = BuildLaborRows(e4.EstimateId, e4Start, e4.Days, 1.05m,
            ("General Foreman",       "Indirect", "Day", 1),
            ("Foreman",               "Indirect", "Day", 2),
            ("Pipefitter Journeyman", "Direct",   "Day", 4),
            ("Pipefitter Helper",     "Direct",   "Day", 2),
            ("Welder Journeyman",     "Direct",   "Day", 2),
            ("Safety Watch",          "Indirect", "Day", 1));
        db.LaborRows.AddRange(e4Rows);

        // E5 — Lyondell Channelview, LCC rates (Standard × 1.02)
        var e5DayRows = BuildLaborRows(e5.EstimateId, e5Start, e5.Days, 1.02m,
            ("Project Manager",        "Indirect", "Day",   1),
            ("General Foreman",        "Indirect", "Day",   1),
            ("Foreman",                "Indirect", "Day",   2),
            ("Pipefitter Journeyman",  "Direct",   "Day",   6),
            ("Pipefitter Helper",      "Direct",   "Day",   4),
            ("Welder Journeyman",      "Direct",   "Day",   2),
            ("Welder Helper",          "Direct",   "Day",   2),
            ("Boilermaker Journeyman", "Direct",   "Day",   1),
            ("Electrician Journeyman", "Direct",   "Day",   1),
            ("Instrument Tech",        "Direct",   "Day",   1),
            ("NDT Technician",         "Direct",   "Day",   1),
            ("Safety Watch",           "Indirect", "Day",   2),
            ("Fire Watch",             "Indirect", "Day",   1));
        var e5NightRows = BuildLaborRows(e5.EstimateId, e5Start, e5.Days, 1.02m,
            ("General Foreman",        "Indirect", "Night", 1),
            ("Foreman",                "Indirect", "Night", 2),
            ("Pipefitter Journeyman",  "Direct",   "Night", 4),
            ("Pipefitter Helper",      "Direct",   "Night", 3),
            ("Welder Journeyman",      "Direct",   "Night", 2),
            ("Welder Helper",          "Direct",   "Night", 1),
            ("Boilermaker Journeyman", "Direct",   "Night", 1),
            ("Electrician Journeyman", "Direct",   "Night", 1),
            ("Safety Watch",           "Indirect", "Night", 1),
            ("Fire Watch",             "Indirect", "Night", 1));
        for (int i = 0; i < e5NightRows.Count; i++) e5NightRows[i].SortOrder = e5DayRows.Count + i;
        db.LaborRows.AddRange(e5DayRows);
        db.LaborRows.AddRange(e5NightRows);

        await db.SaveChangesAsync(ct);

        // ── 6. SUMMARIES ──────────────────────────────────────────────────────

        void AddSummary(AppDbContext ctx, Estimate est, List<LaborRow> rows, decimal targetMarginPct)
        {
            var bill = rows.Sum(r => r.Subtotal);
            var cost = Math.Round(bill * (1m - targetMarginPct / 100m), 2);
            var profit = bill - cost;
            ctx.EstimateSummaries.Add(new EstimateSummary
            {
                EstimateId = est.EstimateId,
                BillSubtotal = bill,
                DiscountType = "None", DiscountValue = 0, DiscountAmount = 0,
                TaxRate = 0, TaxAmount = 0,
                GrandTotal = bill,
                InternalCostTotal = cost,
                GrossProfit = profit,
                GrossMarginPct = Math.Round(profit / bill, 4),
                UpdatedAt = now
            });
        }

        var allE1 = e1Rows;
        var allE2 = e2DayRows.Concat(e2NightRows).ToList();
        var allE3 = e3Rows;
        var allE4 = e4Rows;
        var allE5 = e5DayRows.Concat(e5NightRows).ToList();

        AddSummary(db, e1, allE1, 28m);
        AddSummary(db, e2, allE2, 24m);
        AddSummary(db, e3, allE3, 30m);
        AddSummary(db, e4, allE4, 17m);
        AddSummary(db, e5, allE5, 22m);

        await db.SaveChangesAsync(ct);

        // Reload summaries for revision snapshots
        await db.Entry(e1).Reference(e => e.Summary).LoadAsync(ct);

        // ── 7. REVISIONS ON E1 ────────────────────────────────────────────────

        // Rev 1 — preliminary 5-worker estimate
        var rev1Rows = BuildLaborRows(-1, e1Start, 7, 1.0m,
            ("Project Manager",   "Indirect", "Day", 1),
            ("General Foreman",   "Indirect", "Day", 1),
            ("Foreman",           "Indirect", "Day", 2),
            ("Safety Watch",      "Indirect", "Day", 1));
        var rev1Bill = rev1Rows.Sum(r => r.Subtotal);
        db.EstimateRevisions.Add(MakeRevision(e1.EstimateId, 1, false, "Preliminary crew estimate",
            e1, rev1Rows, rev1Bill, Math.Round(rev1Bill * 0.72m, 2), now.AddDays(-14)));

        // Rev 2 — intermediate estimate
        var rev2Rows = BuildLaborRows(-1, e1Start, 10, 1.0m,
            ("Project Manager",       "Indirect", "Day", 1),
            ("General Foreman",       "Indirect", "Day", 1),
            ("Foreman",               "Indirect", "Day", 2),
            ("Pipefitter Journeyman", "Direct",   "Day", 3),
            ("Pipefitter Helper",     "Direct",   "Day", 2),
            ("Safety Watch",          "Indirect", "Day", 1));
        var rev2Bill = rev2Rows.Sum(r => r.Subtotal);
        db.EstimateRevisions.Add(MakeRevision(e1.EstimateId, 2, false, "Revised crew — scope clarified",
            e1, rev2Rows, rev2Bill, Math.Round(rev2Bill * 0.72m, 2), now.AddDays(-7)));

        // Rev 3 — current (full crew)
        var rev3Bill = e1.Summary!.GrandTotal;
        db.EstimateRevisions.Add(MakeRevision(e1.EstimateId, 3, true, "Final crew — approved for submission",
            e1, allE1, rev3Bill, e1.Summary.InternalCostTotal, now));

        await db.SaveChangesAsync(ct);

        // ── 8. STAFFING PLANS ─────────────────────────────────────────────────

        var sp1Num = await _numberService.NextStaffingPlanNumberAsync("CSL", null, "VCC", ct);
        var sp1 = new StaffingPlan
        {
            CompanyCode = "CSL", StaffingPlanNumber = sp1Num,
            Name = "Valero Summer 2026 Manpower Plan", Client = "Valero Energy Corporation",
            ClientCode = "VCC", Branch = "100", City = "Corpus Christi", State = "TX",
            Status = "Active", Shift = "Day", HoursPerShift = 10, Days = 90,
            StartDate = new DateTime(2026, 6, 1), EndDate = new DateTime(2026, 8, 29),
            OtMethod = "daily8_weekly40",
            CreatedBy = "seed", CreatedAt = now, UpdatedAt = now,
            LaborRows = SpLaborRows(1.0m,
                ("Foreman",               "Indirect", "Day", 2),
                ("Pipefitter Journeyman", "Direct",   "Day", 8),
                ("Pipefitter Helper",     "Direct",   "Day", 4),
                ("Safety Watch",          "Indirect", "Day", 1))
        };

        var sp2Num = await _numberService.NextStaffingPlanNumberAsync("CSL", null, "BTX", ct);
        var sp2 = new StaffingPlan
        {
            CompanyCode = "CSL", StaffingPlanNumber = sp2Num,
            Name = "BP Q3 Planned Outage Staffing", Client = "BP America Inc.",
            ClientCode = "BTX", Branch = "100", City = "Texas City", State = "TX",
            Status = "Draft", Shift = "Day", HoursPerShift = 10, Days = 45,
            StartDate = new DateTime(2026, 8, 1), EndDate = new DateTime(2026, 9, 14),
            OtMethod = "daily8_weekly40",
            CreatedBy = "seed", CreatedAt = now, UpdatedAt = now,
            LaborRows = SpLaborRows(1.06m,
                ("Foreman",               "Indirect", "Day", 1),
                ("Pipefitter Journeyman", "Direct",   "Day", 4),
                ("Pipefitter Helper",     "Direct",   "Day", 2))
        };

        var sp3Num = await _numberService.NextStaffingPlanNumberAsync("CSL", null, "SDP", ct);
        var sp3 = new StaffingPlan
        {
            CompanyCode = "CSL", StaffingPlanNumber = sp3Num,
            Name = "Shell Deer Park Inspection [CONVERTED]", Client = "Shell Oil Company",
            ClientCode = "SDP", Branch = "100", City = "Deer Park", State = "TX",
            Status = "Converted", Shift = "Day", HoursPerShift = 10, Days = 7,
            StartDate = e3Start, EndDate = e3Start.AddDays(6),
            OtMethod = "daily8_weekly40",
            CreatedBy = "seed", CreatedAt = now, UpdatedAt = now,
            LaborRows = SpLaborRows(1.04m,
                ("Project Manager",       "Indirect", "Day", 1),
                ("Foreman",               "Indirect", "Day", 1),
                ("Pipefitter Journeyman", "Direct",   "Day", 3),
                ("NDT Technician",        "Direct",   "Day", 1),
                ("Instrument Tech",       "Direct",   "Day", 1),
                ("Safety Watch",          "Indirect", "Day", 1))
        };

        db.StaffingPlans.AddRange(sp1, sp2, sp3);
        await db.SaveChangesAsync(ct);

        // ── 9. CROSS-LINK E3 ↔ SP3 ───────────────────────────────────────────

        e3.StaffingPlanId = sp3.StaffingPlanId;
        sp3.ConvertedEstimateId = e3.EstimateId;
        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            message = "Dev seed data created successfully.",
            rateBooks = 8,
            costBooks = 1,
            crewTemplates = 2,
            estimates = 5,
            staffingPlans = 3
        });
    }

    // ── HELPERS ───────────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions _camelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // Position record: navCode, craftCode, name, laborType, ST, OT, DT
    private record Pos(string Nav, string Craft, string Name, string Type, decimal St, decimal Ot, decimal Dt);

    private static List<Pos> BasePositions() => new()
    {
        // Direct
        new("PF001", "PF",  "Pipefitter Journeyman",  "Direct",   78m,  117m,    156m),
        new("PF002", "PFH", "Pipefitter Helper",       "Direct",   52m,   78m,    104m),
        new("BM001", "BM",  "Boilermaker Journeyman",  "Direct",   82m,  123m,    164m),
        new("BM002", "BMH", "Boilermaker Helper",      "Direct",   54m,   81m,    108m),
        new("WD001", "WD",  "Welder Journeyman",       "Direct",   85m,  127.50m, 170m),
        new("WD002", "WDH", "Welder Helper",           "Direct",   55m,   82.50m, 110m),
        new("MW001", "MW",  "Millwright Journeyman",   "Direct",   80m,  120m,    160m),
        new("EL001", "EL",  "Electrician Journeyman",  "Direct",   82m,  123m,    164m),
        new("IT001", "IE",  "Instrument Tech",         "Direct",   88m,  132m,    176m),
        new("CO001", "OPR", "Crane Operator",          "Direct",   95m,  142.50m, 190m),
        new("RG001", "RIG", "Rigger",                  "Direct",   72m,  108m,    144m),
        new("SC001", "SCF", "Scaffold Builder",        "Direct",   65m,   97.50m, 130m),
        new("ND001", "NDT", "NDT Technician",          "Direct",   95m,  142.50m, 190m),
        // Indirect
        new("PM001", "MGT", "Project Manager",         "Indirect", 125m, 187.50m, 250m),
        new("GF001", "SUP", "General Foreman",         "Indirect",  98m, 147m,    196m),
        new("FM001", "SUP", "Foreman",                 "Indirect",  85m, 127.50m, 170m),
        new("SW001", "SAF", "Safety Watch",            "Indirect",  48m,  72m,     96m),
        new("FW001", "SAF", "Fire Watch",              "Indirect",  45m,  67.50m,  90m),
        new("HW001", "SAF", "Hole Watch",              "Indirect",  45m,  67.50m,  90m),
        new("DR001", "DRV", "Driver/Teamster",         "Indirect",  58m,  87m,    116m),
    };

    private static decimal Rt(decimal v, decimal m) => Math.Round(v * m, 2);

    private static RateBook MakeRateBook(
        string name, string? client, string? clientCode, string? city, string? state,
        bool isBaseline, DateTime? expires, List<Pos> positions, decimal mult, DateTimeOffset now,
        bool skipEquip = false)
    {
        var rb = new RateBook
        {
            CompanyCode = "CSL", Name = name, Client = client, ClientCode = clientCode,
            City = city, State = state, IsStandardBaseline = isBaseline,
            EffectiveDate = new DateTime(2026, 1, 1),
            ExpiresDate = expires,
            CreatedBy = "seed", UpdatedAt = now,
            LaborRates = positions.Select((p, i) => new RateBookLaborRate
            {
                Position = p.Name, LaborType = p.Type, NavCode = p.Nav, CraftCode = p.Craft,
                StRate = Rt(p.St, mult), OtRate = Rt(p.Ot, mult), DtRate = Rt(p.Dt, mult),
                SortOrder = i
            }).ToList(),
            EquipmentRates = RbEquipmentRates(mult),
            ExpenseItems = name == "BP El Reno" ? RbExpensesElReno() : RbExpensesStandard()
        };
        return rb;
    }

    private static List<RateBookEquipmentRate> RbEquipmentRates(decimal mult) => new()
    {
        new() { Name="Crane - 50 Ton",         Hourly=0, Daily=Rt(1800,mult), Weekly=Rt(7500,mult),  Monthly=Rt(22000,mult), SortOrder=0 },
        new() { Name="Crane - 100 Ton",        Hourly=0, Daily=Rt(2800,mult), Weekly=Rt(12000,mult), Monthly=Rt(38000,mult), SortOrder=1 },
        new() { Name="Manlift 40ft",           Hourly=0, Daily=Rt(450,mult),  Weekly=Rt(1800,mult),  Monthly=Rt(5200,mult),  SortOrder=2 },
        new() { Name="Manlift 60ft",           Hourly=0, Daily=Rt(650,mult),  Weekly=Rt(2600,mult),  Monthly=Rt(7500,mult),  SortOrder=3 },
        new() { Name="Scissor Lift",           Hourly=0, Daily=Rt(285,mult),  Weekly=Rt(1100,mult),  Monthly=Rt(3200,mult),  SortOrder=4 },
        new() { Name="Forklift 5K",            Hourly=0, Daily=Rt(325,mult),  Weekly=Rt(1300,mult),  Monthly=Rt(3800,mult),  SortOrder=5 },
        new() { Name="Forklift 10K",           Hourly=0, Daily=Rt(425,mult),  Weekly=Rt(1700,mult),  Monthly=Rt(5000,mult),  SortOrder=6 },
        new() { Name="Welding Machine 400amp", Hourly=0, Daily=Rt(125,mult),  Weekly=Rt(500,mult),   Monthly=Rt(1400,mult),  SortOrder=7 },
        new() { Name="Air Compressor 185cfm",  Hourly=0, Daily=Rt(185,mult),  Weekly=Rt(740,mult),   Monthly=Rt(2100,mult),  SortOrder=8 },
        new() { Name="Light Tower",            Hourly=0, Daily=Rt(95,mult),   Weekly=Rt(380,mult),   Monthly=Rt(1100,mult),  SortOrder=9 },
        new() { Name="Generator 25KW",         Hourly=0, Daily=Rt(175,mult),  Weekly=Rt(700,mult),   Monthly=Rt(2000,mult),  SortOrder=10 },
        new() { Name="Pickup Truck",           Hourly=0, Daily=Rt(125,mult),  Weekly=Rt(500,mult),   Monthly=Rt(1500,mult),  SortOrder=11 },
    };

    private static List<RateBookExpenseItem> RbExpensesStandard() => new()
    {
        new() { Category="PerDiem",  Description="Standard Per Diem",       Rate=65m,   Unit="day",   SortOrder=0 },
        new() { Category="PerDiem",  Description="Per Diem - High Cost Area",Rate=85m,  Unit="day",   SortOrder=1 },
        new() { Category="PerDiem",  Description="Meals Only",              Rate=45m,   Unit="day",   SortOrder=2 },
        new() { Category="PerDiem",  Description="Direct",                  Rate=100m,  Unit="day",   SortOrder=3 },
        new() { Category="PerDiem",  Description="Indirect",                Rate=120m,  Unit="day",   SortOrder=4 },
        new() { Category="Travel",   Description="Mileage Reimbursement",   Rate=0.67m, Unit="mile",  SortOrder=5 },
        new() { Category="Travel",   Description="Airfare - Domestic",      Rate=500m,  Unit="trip",  SortOrder=6 },
        new() { Category="Travel",   Description="Rental Car",              Rate=75m,   Unit="day",   SortOrder=7 },
        new() { Category="Travel",   Description="Fuel Allowance",          Rate=50m,   Unit="day",   SortOrder=8 },
        new() { Category="Lodging",  Description="Standard Hotel",          Rate=150m,  Unit="night", SortOrder=9 },
        new() { Category="Lodging",  Description="Extended Stay",           Rate=125m,  Unit="night", SortOrder=10 },
        new() { Category="Lodging",  Description="Premium Hotel",           Rate=200m,  Unit="night", SortOrder=11 },
    };

    private static List<RateBookExpenseItem> RbExpensesElReno() => new()
    {
        new() { Category="PerDiem",  Description="Standard Per Diem",       Rate=65m,   Unit="day",   SortOrder=0 },
        new() { Category="PerDiem",  Description="Out of Town Per Diem",    Rate=125m,  Unit="day",   SortOrder=1 },
        new() { Category="PerDiem",  Description="High Cost Area",          Rate=150m,  Unit="day",   SortOrder=2 },
        new() { Category="PerDiem",  Description="Direct",                  Rate=100m,  Unit="day",   SortOrder=3 },
        new() { Category="PerDiem",  Description="Indirect",                Rate=90m,   Unit="day",   SortOrder=4 },
        new() { Category="Travel",   Description="Mileage Reimbursement",   Rate=0.67m, Unit="mile",  SortOrder=5 },
        new() { Category="Travel",   Description="Airfare - Domestic",      Rate=500m,  Unit="trip",  SortOrder=6 },
        new() { Category="Travel",   Description="Rental Car",              Rate=75m,   Unit="day",   SortOrder=7 },
        new() { Category="Lodging",  Description="Standard Hotel",          Rate=120m,  Unit="night", SortOrder=8 },
        new() { Category="Lodging",  Description="Extended Stay",           Rate=95m,   Unit="night", SortOrder=9 },
    };

    // ── Cost Book data ────────────────────────────────────────────────────────

    private static List<CostBookOverheadItem> CbOverhead() => new()
    {
        new() { Category="Burden",    Code="FICA",  Name="FICA / Social Security",    BurdenType="percentage",      Value=7.65m,  SortOrder=0 },
        new() { Category="Burden",    Code="FUTA",  Name="FUTA (Federal Unemployment)",BurdenType="percentage",     Value=0.60m,  SortOrder=1 },
        new() { Category="Burden",    Code="SUTA",  Name="SUTA (State Unemployment)",  BurdenType="percentage",     Value=2.70m,  SortOrder=2 },
        new() { Category="Burden",    Code="WC",    Name="Workers Comp",              BurdenType="percentage",     Value=8.50m,  SortOrder=3 },
        new() { Category="Insurance", Code="GL",    Name="General Liability",         BurdenType="percentage",     Value=2.50m,  SortOrder=4 },
        new() { Category="Insurance", Code="AUTO",  Name="Auto Insurance",            BurdenType="percentage",     Value=1.00m,  SortOrder=5 },
        new() { Category="Insurance", Code="UMB",   Name="Umbrella / Excess",         BurdenType="percentage",     Value=0.75m,  SortOrder=6 },
        new() { Category="Insurance", Code="BOND",  Name="Bonding",                   BurdenType="percentage",     Value=1.50m,  SortOrder=7 },
        new() { Category="Other",     Code="HW",    Name="Health Benefits",           BurdenType="percentage",     Value=6.00m,  SortOrder=8 },
        new() { Category="Other",     Code="401K",  Name="401k Match",                BurdenType="percentage",     Value=3.00m,  SortOrder=9 },
        new() { Category="Other",     Code="TRN",   Name="Training / Safety",         BurdenType="percentage",     Value=1.00m,  SortOrder=10 },
        new() { Category="Other",     Code="GA",    Name="G&A / Admin",               BurdenType="percentage",     Value=5.00m,  SortOrder=11 },
    };

    private static List<CostBookLaborRate> CbLaborRates() => new()
    {
        // Direct
        new() { Position="Pipefitter Journeyman",  LaborType="Direct",   NavCode="PF001", CraftCode="PF",  StRate=42m,  OtRate=63m,    DtRate=84m,   SortOrder=0 },
        new() { Position="Pipefitter Helper",       LaborType="Direct",   NavCode="PF002", CraftCode="PFH", StRate=28m,  OtRate=42m,    DtRate=56m,   SortOrder=1 },
        new() { Position="Boilermaker Journeyman",  LaborType="Direct",   NavCode="BM001", CraftCode="BM",  StRate=44m,  OtRate=66m,    DtRate=88m,   SortOrder=2 },
        new() { Position="Boilermaker Helper",      LaborType="Direct",   NavCode="BM002", CraftCode="BMH", StRate=29m,  OtRate=43.50m, DtRate=58m,   SortOrder=3 },
        new() { Position="Welder Journeyman",       LaborType="Direct",   NavCode="WD001", CraftCode="WD",  StRate=46m,  OtRate=69m,    DtRate=92m,   SortOrder=4 },
        new() { Position="Welder Helper",           LaborType="Direct",   NavCode="WD002", CraftCode="WDH", StRate=30m,  OtRate=45m,    DtRate=60m,   SortOrder=5 },
        new() { Position="Millwright Journeyman",   LaborType="Direct",   NavCode="MW001", CraftCode="MM",  StRate=43m,  OtRate=64.50m, DtRate=86m,   SortOrder=6 },
        new() { Position="Electrician Journeyman",  LaborType="Direct",   NavCode="EL001", CraftCode="EL",  StRate=44m,  OtRate=66m,    DtRate=88m,   SortOrder=7 },
        new() { Position="Instrument Tech",         LaborType="Direct",   NavCode="IT001", CraftCode="IE",  StRate=48m,  OtRate=72m,    DtRate=96m,   SortOrder=8 },
        new() { Position="Crane Operator",          LaborType="Direct",   NavCode="CO001", CraftCode="OPR", StRate=48m,  OtRate=72m,    DtRate=96m,   SortOrder=9 },
        new() { Position="Rigger",                  LaborType="Direct",   NavCode="RG001", CraftCode="RIG", StRate=40m,  OtRate=60m,    DtRate=80m,   SortOrder=10 },
        new() { Position="Scaffold Builder",        LaborType="Direct",   NavCode="SC001", CraftCode="SCF", StRate=36m,  OtRate=54m,    DtRate=72m,   SortOrder=11 },
        new() { Position="Driver/Teamster",         LaborType="Direct",   NavCode="DR001", CraftCode="DRV", StRate=32m,  OtRate=48m,    DtRate=64m,   SortOrder=12 },
        // Indirect
        new() { Position="Project Manager",         LaborType="Indirect", NavCode="PM001", CraftCode="MGT", StRate=65m,  OtRate=97.50m, DtRate=130m,  SortOrder=13 },
        new() { Position="General Foreman",         LaborType="Indirect", NavCode="GF001", CraftCode="SUP", StRate=52m,  OtRate=78m,    DtRate=104m,  SortOrder=14 },
        new() { Position="Foreman",                 LaborType="Indirect", NavCode="FM001", CraftCode="SUP", StRate=45m,  OtRate=67.50m, DtRate=90m,   SortOrder=15 },
        new() { Position="Safety Watch",            LaborType="Indirect", NavCode="SW001", CraftCode="SAF", StRate=26m,  OtRate=39m,    DtRate=52m,   SortOrder=16 },
        new() { Position="Fire Watch",              LaborType="Indirect", NavCode="FW001", CraftCode="SAF", StRate=24m,  OtRate=36m,    DtRate=48m,   SortOrder=17 },
        new() { Position="Hole Watch",              LaborType="Indirect", NavCode="HW001", CraftCode="SAF", StRate=24m,  OtRate=36m,    DtRate=48m,   SortOrder=18 },
    };

    private static List<CostBookEquipmentRate> CbEquipmentRates() => new()
    {
        new() { Name="Crane - 50 Ton",         Hourly=0, Daily=1200m,  Weekly=5000m,  Monthly=15000m, SortOrder=0 },
        new() { Name="Crane - 100 Ton",        Hourly=0, Daily=1900m,  Weekly=8000m,  Monthly=26000m, SortOrder=1 },
        new() { Name="Manlift 40ft",           Hourly=0, Daily=280m,   Weekly=1100m,  Monthly=3200m,  SortOrder=2 },
        new() { Name="Manlift 60ft",           Hourly=0, Daily=400m,   Weekly=1600m,  Monthly=4600m,  SortOrder=3 },
        new() { Name="Scissor Lift",           Hourly=0, Daily=175m,   Weekly=700m,   Monthly=2000m,  SortOrder=4 },
        new() { Name="Forklift 5K",            Hourly=0, Daily=200m,   Weekly=800m,   Monthly=2400m,  SortOrder=5 },
        new() { Name="Forklift 10K",           Hourly=0, Daily=275m,   Weekly=1100m,  Monthly=3200m,  SortOrder=6 },
        new() { Name="Welding Machine 400amp", Hourly=0, Daily=75m,    Weekly=300m,   Monthly=850m,   SortOrder=7 },
        new() { Name="Air Compressor 185cfm",  Hourly=0, Daily=110m,   Weekly=440m,   Monthly=1300m,  SortOrder=8 },
        new() { Name="Light Tower",            Hourly=0, Daily=55m,    Weekly=220m,   Monthly=650m,   SortOrder=9 },
        new() { Name="Generator 25KW",         Hourly=0, Daily=105m,   Weekly=420m,   Monthly=1200m,  SortOrder=10 },
        new() { Name="Hydro Blast Unit",       Hourly=0, Daily=550m,   Weekly=2200m,  Monthly=6400m,  SortOrder=11 },
    };

    private static List<CostBookExpense> CbExpenses() => new()
    {
        new() { Category="PerDiem", Description="Standard Per Diem (Local)",      Rate=65m,   Unit="day",   SortOrder=0 },
        new() { Category="PerDiem", Description="Standard Per Diem (Out of Town)",Rate=125m,  Unit="day",   SortOrder=1 },
        new() { Category="PerDiem", Description="Per Diem - High Cost Area",      Rate=150m,  Unit="day",   SortOrder=2 },
        new() { Category="PerDiem", Description="Meals Only",                     Rate=55m,   Unit="day",   SortOrder=3 },
        new() { Category="Travel",  Description="Mileage Reimbursement",          Rate=0.67m, Unit="mile",  SortOrder=4 },
        new() { Category="Travel",  Description="Company Vehicle",                Rate=85m,   Unit="day",   SortOrder=5 },
        new() { Category="Travel",  Description="Rental Car",                     Rate=75m,   Unit="day",   SortOrder=6 },
        new() { Category="Travel",  Description="Airfare (Average)",              Rate=450m,  Unit="trip",  SortOrder=7 },
        new() { Category="Lodging", Description="Standard Hotel",                 Rate=120m,  Unit="night", SortOrder=8 },
        new() { Category="Lodging", Description="Extended Stay",                  Rate=95m,   Unit="night", SortOrder=9 },
        new() { Category="Lodging", Description="Premium Hotel",                  Rate=175m,  Unit="night", SortOrder=10 },
        new() { Category="Lodging", Description="Man Camp",                       Rate=85m,   Unit="night", SortOrder=11 },
    };

    // ── Labor row builders ────────────────────────────────────────────────────

    private static List<LaborRow> BuildLaborRows(
        int estimateId,
        DateTime startDate,
        int days,
        decimal mult,
        params (string Position, string LaborType, string Shift, int Qty)[] specs)
    {
        var positions = BasePositions().ToDictionary(p => p.Name);
        var rows = new List<LaborRow>();
        for (int i = 0; i < specs.Length; i++)
        {
            var (pos, type, shift, qty) = specs[i];
            if (!positions.TryGetValue(pos, out var p)) continue;

            decimal st = qty * days * 8m;
            decimal ot = qty * days * 2m;
            decimal stRate = Rt(p.St, mult);
            decimal otRate = Rt(p.Ot, mult);
            decimal dtRate = Rt(p.Dt, mult);
            rows.Add(new LaborRow
            {
                EstimateId = estimateId == -1 ? 0 : estimateId,
                Position = pos, LaborType = type, Shift = shift,
                NavCode = p.Nav, CraftCode = p.Craft,
                BillStRate = stRate, BillOtRate = otRate, BillDtRate = dtRate,
                StHours = st, OtHours = ot, DtHours = 0,
                Subtotal = st * stRate + ot * otRate,
                SortOrder = i,
                ScheduleJson = estimateId == -1 ? null : BuildScheduleJson(startDate, days, qty)
            });
        }
        return rows;
    }

    private static List<StaffingLaborRow> SpLaborRows(
        decimal mult,
        params (string Position, string LaborType, string Shift, int Qty)[] specs)
    {
        var positions = BasePositions().ToDictionary(p => p.Name);
        var rows = new List<StaffingLaborRow>();
        for (int i = 0; i < specs.Length; i++)
        {
            var (pos, type, shift, qty) = specs[i];
            if (!positions.TryGetValue(pos, out var p)) continue;
            rows.Add(new StaffingLaborRow
            {
                Position = pos, LaborType = type, Shift = shift,
                NavCode = p.Nav, CraftCode = p.Craft,
                StRate = Rt(p.St, mult), OtRate = Rt(p.Ot, mult), DtRate = Rt(p.Dt, mult),
                SortOrder = i
            });
        }
        return rows;
    }

    private static string BuildScheduleJson(DateTime startDate, int days, int headcount)
    {
        var dict = new Dictionary<string, int>();
        for (int i = 0; i < days; i++)
            dict[startDate.AddDays(i).ToString("yyyy-MM-dd")] = headcount;
        return JsonSerializer.Serialize(dict);
    }

    private static EstimateRevision MakeRevision(
        int estimateId, int revNum, bool isCurrent, string description,
        Estimate e, List<LaborRow> rows, decimal grandTotal, decimal internalCost, DateTimeOffset savedAt)
    {
        var snap = new EstimateSnapshotDto
        {
            Name = e.Name, Client = e.Client, ClientCode = e.ClientCode,
            Branch = e.Branch, City = e.City, State = e.State,
            Shift = e.Shift, HoursPerShift = e.HoursPerShift, Days = e.Days,
            StartDate = e.StartDate, EndDate = e.EndDate,
            OtMethod = e.OtMethod, DtWeekends = e.DtWeekends,
            Status = e.Status, ConfidencePct = e.ConfidencePct,
            LaborRows = rows.Select(r => new LaborRowSnapshotDto
            {
                Position = r.Position, LaborType = r.LaborType, Shift = r.Shift,
                NavCode = r.NavCode, CraftCode = r.CraftCode,
                BillStRate = r.BillStRate, BillOtRate = r.BillOtRate, BillDtRate = r.BillDtRate,
                StHours = r.StHours, OtHours = r.OtHours, DtHours = r.DtHours, Subtotal = r.Subtotal
            }).ToList(),
            EquipmentRows = new(),
            ExpenseRows = new(),
            Summary = new SummarySnapshotDto
            {
                BillSubtotal = grandTotal, GrandTotal = grandTotal,
                InternalCostTotal = internalCost,
                GrossProfit = grandTotal - internalCost,
                GrossMarginPct = grandTotal > 0 ? Math.Round((grandTotal - internalCost) / grandTotal, 4) : 0,
                DiscountType = "None"
            }
        };

        return new EstimateRevision
        {
            EstimateId = estimateId,
            RevisionNumber = revNum,
            IsCurrent = isCurrent,
            Description = description,
            SnapshotJson = JsonSerializer.Serialize(snap, _camelCase),
            SavedBy = "seed",
            SavedAt = savedAt,
            LaborCount = rows.Count,
            EquipCount = 0,
            LaborTotal = rows.Sum(r => r.Subtotal),
            EquipTotal = 0,
            GrandTotal = grandTotal
        };
    }
}
