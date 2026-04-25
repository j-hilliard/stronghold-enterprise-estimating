using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Api.Services;

public class ToolExecutorService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ToolExecutorService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<string> ExecuteAsync(string toolName, JsonElement arguments, string companyCode, CancellationToken ct = default)
    {
        return toolName switch
        {
            "get_pipeline_summary"  => await GetPipelineSummaryAsync(companyCode, ct),
            "get_active_jobs"       => await GetActiveJobsAsync(companyCode, ct),
            "search_estimates"      => await SearchEstimatesAsync(arguments, companyCode, ct),
            "get_win_loss_stats"    => await GetWinLossStatsAsync(arguments, companyCode, ct),
            "get_labor_utilization" => await GetLaborUtilizationAsync(companyCode, ct),
            _ => JsonSerializer.Serialize(new { error = $"Unknown tool: {toolName}" })
        };
    }

    private async Task<string> GetPipelineSummaryAsync(string companyCode, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var estimates = await db.Estimates
            .Where(e => e.CompanyCode == companyCode && !e.IsScenario)
            .Include(e => e.Summary)
            .ToListAsync(ct);

        var byStatus = estimates
            .GroupBy(e => e.Status)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    count = g.Count(),
                    totalValue = g.Sum(e => e.Summary?.GrandTotal ?? 0)
                });

        var today = DateTime.UtcNow.Date;
        var activeJobs = estimates
            .Where(e => e.Status == "Awarded"
                && e.StartDate.HasValue && e.EndDate.HasValue
                && e.StartDate.Value.Date <= today
                && e.EndDate.Value.Date >= today)
            .ToList();

        return JsonSerializer.Serialize(new
        {
            companyCode,
            asOf = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            totalEstimates = estimates.Count,
            byStatus,
            activeJobsToday = activeJobs.Count,
            totalPipelineValue = estimates
                .Where(e => e.Status is "Pending" or "Submitted" or "Awarded")
                .Sum(e => e.Summary?.GrandTotal ?? 0),
            awardedValue = byStatus.GetValueOrDefault("Awarded")?.totalValue ?? 0,
            submittedValue = byStatus.GetValueOrDefault("Submitted")?.totalValue ?? 0,
            pendingValue = byStatus.GetValueOrDefault("Pending")?.totalValue ?? 0
        });
    }

    private async Task<string> GetActiveJobsAsync(string companyCode, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var today = DateTime.UtcNow.Date;

        var jobs = await db.Estimates
            .Where(e => e.CompanyCode == companyCode
                && e.Status == "Awarded"
                && !e.IsScenario
                && e.StartDate.HasValue && e.EndDate.HasValue
                && e.StartDate.Value.Date <= today
                && e.EndDate.Value.Date >= today)
            .Include(e => e.LaborRows)
            .OrderBy(e => e.StartDate)
            .ToListAsync(ct);

        var result = jobs.Select(e => new
        {
            estimateId = e.EstimateId,
            number = e.EstimateNumber,
            name = e.Name,
            client = e.Client,
            startDate = e.StartDate?.ToString("yyyy-MM-dd"),
            endDate = e.EndDate?.ToString("yyyy-MM-dd"),
            headcount = e.LaborRows.GroupBy(r => r.Position).Count(),
            totalPositions = e.LaborRows.Count
        }).ToList();

        return JsonSerializer.Serialize(new
        {
            date = today.ToString("yyyy-MM-dd"),
            activeJobs = result,
            totalHeadcount = result.Sum(j => j.headcount)
        });
    }

    private async Task<string> SearchEstimatesAsync(JsonElement args, string companyCode, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var q = db.Estimates
            .Where(e => e.CompanyCode == companyCode && !e.IsScenario)
            .Include(e => e.Summary)
            .AsQueryable();

        if (args.TryGetProperty("status", out var statusEl) && statusEl.ValueKind == JsonValueKind.String)
        {
            var status = statusEl.GetString()!;
            q = q.Where(e => e.Status == status);
        }
        if (args.TryGetProperty("client", out var clientEl) && clientEl.ValueKind == JsonValueKind.String)
        {
            var client = clientEl.GetString()!;
            q = q.Where(e => e.Client != null && e.Client.Contains(client));
        }
        if (args.TryGetProperty("startAfter", out var afterEl) && afterEl.ValueKind == JsonValueKind.String
            && DateTime.TryParse(afterEl.GetString(), out var afterDate))
        {
            q = q.Where(e => e.StartDate >= afterDate);
        }
        if (args.TryGetProperty("startBefore", out var beforeEl) && beforeEl.ValueKind == JsonValueKind.String
            && DateTime.TryParse(beforeEl.GetString(), out var beforeDate))
        {
            q = q.Where(e => e.StartDate <= beforeDate);
        }

        var estimates = await q.OrderByDescending(e => e.StartDate).Take(20).ToListAsync(ct);

        return JsonSerializer.Serialize(new
        {
            count = estimates.Count,
            estimates = estimates.Select(e => new
            {
                estimateId = e.EstimateId,
                number = e.EstimateNumber,
                name = e.Name,
                client = e.Client,
                status = e.Status,
                startDate = e.StartDate?.ToString("yyyy-MM-dd"),
                endDate = e.EndDate?.ToString("yyyy-MM-dd"),
                grandTotal = e.Summary?.GrandTotal ?? 0,
                confidencePct = e.ConfidencePct
            })
        });
    }

    private async Task<string> GetWinLossStatsAsync(JsonElement args, string companyCode, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var q = db.Estimates
            .Where(e => e.CompanyCode == companyCode && !e.IsScenario)
            .AsQueryable();

        if (args.TryGetProperty("client", out var clientEl) && clientEl.ValueKind == JsonValueKind.String)
        {
            var client = clientEl.GetString()!;
            if (!string.IsNullOrWhiteSpace(client))
                q = q.Where(e => e.Client != null && e.Client.Contains(client));
        }

        var estimates = await q.ToListAsync(ct);
        var awarded = estimates.Count(e => e.Status == "Awarded");
        var lost = estimates.Count(e => e.Status == "Lost");
        var total = awarded + lost;

        return JsonSerializer.Serialize(new
        {
            companyCode,
            awarded,
            lost,
            total,
            winRate = total > 0 ? Math.Round((double)awarded / total * 100, 1) : 0,
            pending = estimates.Count(e => e.Status is "Pending" or "Submitted")
        });
    }

    private async Task<string> GetLaborUtilizationAsync(string companyCode, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var today = DateTime.UtcNow.Date;

        var activeJobs = await db.Estimates
            .Where(e => e.CompanyCode == companyCode
                && e.Status == "Awarded"
                && !e.IsScenario
                && e.StartDate.HasValue && e.EndDate.HasValue
                && e.StartDate.Value.Date <= today
                && e.EndDate.Value.Date >= today)
            .Include(e => e.LaborRows)
            .ToListAsync(ct);

        var byPosition = activeJobs
            .SelectMany(e => e.LaborRows)
            .GroupBy(r => r.Position)
            .Select(g => new { position = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .ToList();

        return JsonSerializer.Serialize(new
        {
            date = today.ToString("yyyy-MM-dd"),
            totalHeadcount = byPosition.Sum(x => x.count),
            activeJobs = activeJobs.Count,
            byPosition
        });
    }

    // DB-queryable tool definitions for injection into Groq request
    public static object[] BuildDbTools() => new object[]
    {
        new
        {
            type = "function",
            function = new
            {
                name = "get_pipeline_summary",
                description = "Returns total pipeline value, count and value by status (Awarded/Submitted/Pending/Draft/Lost), and number of active jobs running today.",
                parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
            }
        },
        new
        {
            type = "function",
            function = new
            {
                name = "get_active_jobs",
                description = "Returns all jobs currently active (Awarded status, start <= today <= end) with headcount per job.",
                parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
            }
        },
        new
        {
            type = "function",
            function = new
            {
                name = "search_estimates",
                description = "Search and filter estimates by status, client, or date range. Returns up to 20 results with totals.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        status = new { type = "string", description = "Filter by status: Draft, Pending, Submitted, Awarded, Lost" },
                        client = new { type = "string", description = "Filter by client name (substring match)" },
                        startAfter = new { type = "string", description = "Filter jobs starting after this date (YYYY-MM-DD)" },
                        startBefore = new { type = "string", description = "Filter jobs starting before this date (YYYY-MM-DD)" }
                    },
                    required = Array.Empty<string>()
                }
            }
        },
        new
        {
            type = "function",
            function = new
            {
                name = "get_win_loss_stats",
                description = "Returns win rate, awarded count, and lost count. Optionally filtered by client name.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        client = new { type = "string", description = "Optional client name to filter stats" }
                    },
                    required = Array.Empty<string>()
                }
            }
        },
        new
        {
            type = "function",
            function = new
            {
                name = "get_labor_utilization",
                description = "Returns today's headcount by position across all active jobs.",
                parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
            }
        }
    };
}
