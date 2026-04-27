using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;

namespace Stronghold.EnterpriseEstimating.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/global-analytics")]
[Authorize(Roles = "Administrator,Analytics")]
public class GlobalAnalyticsController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public GlobalAnalyticsController(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(
        [FromQuery] string? companies,
        [FromQuery] string? statuses,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] decimal? minConfidence,
        [FromQuery] string? vp,
        [FromQuery] string? director,
        [FromQuery] string? region,
        [FromQuery] string? jobType,
        [FromQuery] string? client,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var companyCodes = companies?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];
        var statusList   = statuses?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];

        var query = db.Estimates
            .Where(e => !e.IsScenario)
            .AsQueryable();

        if (companyCodes.Length > 0)
            query = query.Where(e => companyCodes.Contains(e.CompanyCode));
        if (statusList.Length > 0)
            query = query.Where(e => statusList.Contains(e.Status));
        if (startDate.HasValue)
            query = query.Where(e => e.EndDate == null || e.EndDate >= startDate);
        if (endDate.HasValue)
            query = query.Where(e => e.StartDate == null || e.StartDate <= endDate);
        if (minConfidence.HasValue)
            query = query.Where(e => e.ConfidencePct >= minConfidence.Value);
        if (!string.IsNullOrWhiteSpace(vp))
            query = query.Where(e => e.VP == vp);
        if (!string.IsNullOrWhiteSpace(director))
            query = query.Where(e => e.Director == director);
        if (!string.IsNullOrWhiteSpace(region))
            query = query.Where(e => e.Region == region);
        if (!string.IsNullOrWhiteSpace(jobType))
            query = query.Where(e => e.JobType == jobType);
        if (!string.IsNullOrWhiteSpace(client))
            query = query.Where(e => e.Client.Contains(client));

        var estimates = await query
            .Select(e => new
            {
                e.EstimateId,
                e.CompanyCode,
                e.EstimateNumber,
                e.Name,
                e.Client,
                e.ClientCode,
                e.JobType,
                e.Status,
                e.ConfidencePct,
                e.StartDate,
                e.EndDate,
                e.VP,
                e.Director,
                e.Region,
                GrandTotal     = e.Summary != null ? e.Summary.GrandTotal : 0m,
                GrossMarginPct = e.Summary != null ? e.Summary.GrossMarginPct : 0m,
            })
            .ToListAsync(ct);

        // ── KPIs ─────────────────────────────────────────────────────────────
        var awarded   = estimates.Where(e => e.Status == "Awarded").ToList();
        var pendingEs = estimates.Where(e => e.Status is "Pending" or "Submitted").ToList();

        var totalForecast        = estimates
            .Where(e => e.Status is not ("Lost" or "Canceled" or "Draft"))
            .Sum(e => e.GrandTotal);
        var confidenceWeighted   = estimates.Sum(e => e.GrandTotal * e.ConfidencePct / 100);
        var awardedTotal         = awarded.Sum(e => e.GrandTotal);
        var pendingTotal         = pendingEs.Sum(e => e.GrandTotal);

        var today      = DateTime.UtcNow.Date;
        var jobsInRange = estimates.Count(e =>
            e.Status == "Awarded" &&
            e.StartDate.HasValue && e.EndDate.HasValue &&
            e.StartDate.Value.Date <= today && e.EndDate.Value.Date >= today);

        // ── Monthly Revenue (next 12 months, by start month) ─────────────────
        var monthlyRevenue = Enumerable.Range(0, 12)
            .Select(i =>
            {
                var month = new DateTime(today.Year, today.Month, 1).AddMonths(i);
                var inMonth = estimates.Where(e =>
                    e.StartDate.HasValue &&
                    e.StartDate.Value.Year == month.Year &&
                    e.StartDate.Value.Month == month.Month);

                return new
                {
                    month    = month.ToString("yyyy-MM"),
                    label    = month.ToString("MMM yy"),
                    awarded  = inMonth.Where(e => e.Status == "Awarded").Sum(e => e.GrandTotal),
                    pending  = inMonth.Where(e => e.Status is "Pending" or "Submitted").Sum(e => e.GrandTotal),
                    csl      = inMonth.Where(e => e.CompanyCode == "CSL").Sum(e => e.GrandTotal),
                    ets      = inMonth.Where(e => e.CompanyCode == "ETS").Sum(e => e.GrandTotal),
                    sts      = inMonth.Where(e => e.CompanyCode == "STS").Sum(e => e.GrandTotal),
                    stg      = inMonth.Where(e => e.CompanyCode == "STG").Sum(e => e.GrandTotal),
                };
            })
            .ToList();

        // ── Top Clients ───────────────────────────────────────────────────────
        var topClients = estimates
            .GroupBy(e => e.Client)
            .Select(g => new
            {
                client   = g.Key,
                total    = g.Sum(e => e.GrandTotal),
                jobCount = g.Count(),
                awarded  = g.Count(e => e.Status == "Awarded"),
            })
            .OrderByDescending(c => c.total)
            .Take(10)
            .ToList();

        // ── By Region ─────────────────────────────────────────────────────────
        var byRegion = estimates
            .GroupBy(e => e.Region ?? "Unknown")
            .Select(g => new
            {
                region   = g.Key,
                total    = g.Sum(e => e.GrandTotal),
                jobCount = g.Count(),
            })
            .OrderByDescending(r => r.total)
            .ToList();

        // ── By Company ────────────────────────────────────────────────────────
        var byCompany = estimates
            .GroupBy(e => e.CompanyCode)
            .Select(g => new
            {
                company  = g.Key,
                total    = g.Sum(e => e.GrandTotal),
                jobCount = g.Count(),
                awarded  = g.Count(e => e.Status == "Awarded"),
            })
            .OrderBy(c => c.company)
            .ToList();

        // ── Estimate rows (for job table) ─────────────────────────────────────
        var estimateRows = estimates
            .OrderBy(e => e.CompanyCode)
            .ThenByDescending(e => e.GrandTotal)
            .Select(e => new
            {
                e.EstimateId,
                e.CompanyCode,
                e.EstimateNumber,
                e.Name,
                e.Client,
                e.ClientCode,
                e.JobType,
                e.Status,
                confidencePct  = e.ConfidencePct,
                grandTotal     = e.GrandTotal,
                grossMarginPct = e.GrossMarginPct,
                startDate      = e.StartDate,
                endDate        = e.EndDate,
                e.VP,
                e.Director,
                e.Region,
            })
            .ToList();

        // ── Filter options (for dropdowns) ───────────────────────────────────
        var allEstimates = await db.Estimates.Where(e => !e.IsScenario).ToListAsync(ct);
        var filterOptions = new
        {
            vps       = allEstimates.Select(e => e.VP).Where(v => !string.IsNullOrEmpty(v)).Distinct().OrderBy(v => v).ToList(),
            directors = allEstimates.Select(e => e.Director).Where(d => !string.IsNullOrEmpty(d)).Distinct().OrderBy(d => d).ToList(),
            regions   = allEstimates.Select(e => e.Region).Where(r => !string.IsNullOrEmpty(r)).Distinct().OrderBy(r => r).ToList(),
            jobTypes  = allEstimates.Select(e => e.JobType).Where(j => !string.IsNullOrEmpty(j)).Distinct().OrderBy(j => j).ToList(),
        };

        return Ok(new
        {
            kpis = new
            {
                totalForecast        = Math.Round(totalForecast, 2),
                confidenceWeighted   = Math.Round(confidenceWeighted, 2),
                awarded              = Math.Round(awardedTotal, 2),
                pending              = Math.Round(pendingTotal, 2),
                jobsInRange,
                totalJobs            = estimates.Count,
            },
            monthlyRevenue,
            topClients,
            byRegion,
            byCompany,
            estimates            = estimateRows,
            filterOptions,
        });
    }
}
