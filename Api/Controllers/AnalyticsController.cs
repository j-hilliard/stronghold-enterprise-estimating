using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;

namespace Stronghold.EnterpriseEstimating.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public AnalyticsController(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    private string CompanyCode => User.FindFirst("company_code")?.Value ?? string.Empty;

    [HttpGet("dashboard")]
    [Authorize(Roles = "Analytics,Administrator")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct = default)
    {
        var companyCode = CompanyCode;
        var thisYear = DateTime.UtcNow.Year;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var estimates = await db.Estimates
            .Where(e => e.CompanyCode == companyCode && !e.IsScenario)
            .Select(e => new
            {
                e.EstimateId,
                e.EstimateNumber,
                e.Name,
                e.Client,
                e.Status,
                e.ConfidencePct,
                e.StartDate,
                e.UpdatedAt,
                Summary = e.Summary == null ? null : new
                {
                    e.Summary.GrandTotal,
                    e.Summary.GrossMarginPct,
                }
            })
            .ToListAsync(ct);

        var awarded = estimates.Where(e => e.Status == "Awarded").ToList();
        var lost    = estimates.Where(e => e.Status == "Lost").ToList();
        var pending = estimates.Where(e => e.Status == "Pending").ToList();
        var draft   = estimates.Where(e => e.Status == "Draft").ToList();

        var pipelineValue = estimates
            .Where(e => e.Status is "Awarded" or "Pending")
            .Sum(e => e.Summary?.GrandTotal ?? 0);

        var wonYtd = awarded
            .Where(e => e.UpdatedAt.Year == thisYear)
            .Sum(e => e.Summary?.GrandTotal ?? 0);

        var awardedWithMargin = awarded.Where(e => e.Summary != null).ToList();

        var avgMarginPct = awardedWithMargin.Count > 0
            ? awardedWithMargin.Average(e => (double)e.Summary!.GrossMarginPct)
            : 0;

        var decisioned = awarded.Count + lost.Count;
        var winRatePct = decisioned > 0 ? (double)awarded.Count / decisioned * 100 : 0;

        var byStatus = estimates
            .GroupBy(e => e.Status)
            .Select(g => new { status = g.Key, count = g.Count(), totalValue = g.Sum(e => e.Summary?.GrandTotal ?? 0) })
            .OrderBy(x => x.status)
            .ToList();

        var marginBuckets = new[]
        {
            new { bucket = "<15%",   count = awardedWithMargin.Count(e => e.Summary!.GrossMarginPct < 15) },
            new { bucket = "15-25%", count = awardedWithMargin.Count(e => e.Summary!.GrossMarginPct >= 15 && e.Summary.GrossMarginPct < 25) },
            new { bucket = ">25%",   count = awardedWithMargin.Count(e => e.Summary!.GrossMarginPct >= 25) },
        };

        var estimateRows = estimates
            .Select(e => new
            {
                e.EstimateId, e.EstimateNumber, e.Name, e.Client, e.Status,
                grandTotal = e.Summary?.GrandTotal ?? 0,
                grossMarginPct = e.Summary?.GrossMarginPct ?? 0,
                e.ConfidencePct,
                startDate = e.StartDate,
                updatedAt = e.UpdatedAt,
            })
            .OrderByDescending(e => e.updatedAt)
            .ToList();

        return Ok(new { kpis = new { pipelineValue, wonYtd, avgMarginPct = Math.Round(avgMarginPct, 1), winRatePct = Math.Round(winRatePct, 1), activeJobCount = awarded.Count, draftCount = draft.Count, pendingCount = pending.Count, lostCount = lost.Count }, byStatus, marginBuckets, estimates = estimateRows });
    }

    [HttpGet("win-loss-by-margin")]
    [Authorize(Roles = "Analytics,Administrator")]
    public async Task<IActionResult> GetWinLossByMargin(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var decisioned = await db.Estimates
            .Where(e => e.CompanyCode == CompanyCode
                     && !e.IsScenario
                     && (e.Status == "Awarded" || e.Status == "Lost")
                     && e.Summary != null)
            .Select(e => new { e.Status, e.Summary!.GrossMarginPct })
            .ToListAsync(ct);

        var bands = new[]
        {
            new { Band = "<15%",   Min = decimal.MinValue, Max = 15m },
            new { Band = "15-25%", Min = 15m,              Max = 25m },
            new { Band = ">25%",   Min = 25m,              Max = decimal.MaxValue },
        };

        var result = bands.Select(b =>
        {
            var inBand = decisioned.Where(e => e.GrossMarginPct >= b.Min && e.GrossMarginPct < b.Max).ToList();
            var won  = inBand.Count(e => e.Status == "Awarded");
            var lostCount = inBand.Count(e => e.Status == "Lost");
            var total = won + lostCount;
            return new { band = b.Band, won, lost = lostCount, total, winRate = total > 0 ? Math.Round((double)won / total * 100, 1) : 0.0 };
        }).ToList();

        return Ok(new { byMarginBand = result });
    }

    [HttpGet("rate-benchmarks")]
    public async Task<IActionResult> GetRateBenchmarks(
        [FromQuery] string? position,
        [FromQuery] string? client,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var query = db.LaborRows
            .Where(r => r.Estimate.CompanyCode == CompanyCode
                     && r.Estimate.Status == "Awarded"
                     && !r.Estimate.IsScenario
                     && r.BillStRate > 0);

        if (!string.IsNullOrWhiteSpace(position))
            query = query.Where(r => r.Position == position);

        if (!string.IsNullOrWhiteSpace(client))
            query = query.Where(r => r.Estimate.Client.Contains(client));

        var rows = await query
            .Select(r => new { r.Position, r.Estimate.Client, r.BillStRate, r.BillOtRate, r.BillDtRate, r.EstimateId })
            .ToListAsync(ct);

        var benchmarks = rows
            .GroupBy(r => new { r.Position, r.Client })
            .Where(g => g.Select(r => r.EstimateId).Distinct().Count() >= 3)
            .Select(g => new
            {
                position   = g.Key.Position,
                client     = g.Key.Client,
                avgStRate  = Math.Round((double)g.Average(r => r.BillStRate), 2),
                avgOtRate  = Math.Round((double)g.Average(r => r.BillOtRate), 2),
                avgDtRate  = Math.Round((double)g.Average(r => r.BillDtRate), 2),
                dataPoints = g.Select(r => r.EstimateId).Distinct().Count(),
            })
            .OrderBy(b => b.position)
            .ToList();

        return Ok(benchmarks);
    }
}
