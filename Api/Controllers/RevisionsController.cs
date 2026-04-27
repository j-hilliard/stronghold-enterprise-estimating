using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Data.Models;
using System.Text.Json;

namespace Stronghold.EnterpriseEstimating.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/estimates/{estimateId:int}/revisions")]
[Authorize]
public class RevisionsController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public RevisionsController(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    private string CompanyCode => User.FindFirst("company_code")?.Value ?? string.Empty;
    private string Username => User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                            ?? User.FindFirst("username")?.Value ?? string.Empty;

    // ── List ──────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> List(int estimateId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (!await db.Estimates.AnyAsync(e => e.EstimateId == estimateId && e.CompanyCode == CompanyCode, ct))
            return NotFound();

        var revisions = await db.EstimateRevisions
            .Where(r => r.EstimateId == estimateId)
            .OrderBy(r => r.RevisionNumber)
            .Select(r => new
            {
                r.EstimateRevisionId,
                r.RevisionNumber,
                r.IsCurrent,
                r.Description,
                r.SavedBy,
                r.SavedAt,
                r.LaborCount,
                r.EquipCount,
                r.LaborTotal,
                r.EquipTotal,
                r.GrandTotal
            })
            .ToListAsync(ct);

        return Ok(revisions);
    }

    // ── Create Snapshot ───────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create(int estimateId, [FromBody] CreateRevisionRequestDto dto, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var estimate = await db.Estimates
            .Include(e => e.LaborRows.OrderBy(r => r.SortOrder))
            .Include(e => e.EquipmentRows.OrderBy(r => r.SortOrder))
            .Include(e => e.ExpenseRows.OrderBy(r => r.SortOrder))
            .Include(e => e.Summary)
            .FirstOrDefaultAsync(e => e.EstimateId == estimateId && e.CompanyCode == CompanyCode, ct);

        if (estimate == null) return NotFound();

        // Build snapshot DTO (avoids circular refs and navigation props)
        var snapshot = EstimateSnapshotDto.FromEstimate(estimate);
        var snapshotJson = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Calculate quick-display totals
        var laborTotal = estimate.LaborRows.Sum(r => r.Subtotal);
        var equipTotal = estimate.EquipmentRows.Sum(r => r.Subtotal);
        var grandTotal = estimate.Summary?.GrandTotal ?? 0m;

        // Determine next revision number (Rev 0 is implicit, so first snapshot = Rev 1)
        var nextRevNum = (await db.EstimateRevisions
            .Where(r => r.EstimateId == estimateId)
            .MaxAsync(r => (int?)r.RevisionNumber, ct) ?? 0) + 1;

        // Clear current flag on all existing revisions
        await db.EstimateRevisions
            .Where(r => r.EstimateId == estimateId && r.IsCurrent)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsCurrent, false), ct);

        var revision = new EstimateRevision
        {
            EstimateId = estimateId,
            RevisionNumber = nextRevNum,
            IsCurrent = true,
            Description = dto.Description,
            SnapshotJson = snapshotJson,
            SavedBy = Username,
            SavedAt = DateTimeOffset.UtcNow,
            LaborCount = estimate.LaborRows.Count,
            EquipCount = estimate.EquipmentRows.Count,
            LaborTotal = laborTotal,
            EquipTotal = equipTotal,
            GrandTotal = grandTotal
        };

        db.EstimateRevisions.Add(revision);
        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            revision.EstimateRevisionId,
            revision.RevisionNumber,
            revision.IsCurrent,
            revision.Description,
            revision.SavedBy,
            revision.SavedAt,
            revision.LaborCount,
            revision.EquipCount,
            revision.LaborTotal,
            revision.EquipTotal,
            revision.GrandTotal
        });
    }

    // ── Restore ───────────────────────────────────────────────────────────
    [HttpPost("{revId:int}/restore")]
    public async Task<IActionResult> Restore(int estimateId, int revId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var estimate = await db.Estimates
            .Include(e => e.LaborRows)
            .Include(e => e.EquipmentRows)
            .Include(e => e.ExpenseRows)
            .Include(e => e.Summary)
            .FirstOrDefaultAsync(e => e.EstimateId == estimateId && e.CompanyCode == CompanyCode, ct);

        if (estimate == null) return NotFound();

        var revision = await db.EstimateRevisions
            .FirstOrDefaultAsync(r => r.EstimateRevisionId == revId && r.EstimateId == estimateId, ct);

        if (revision == null) return NotFound();

        var snapshot = JsonSerializer.Deserialize<EstimateSnapshotDto>(revision.SnapshotJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (snapshot == null) return BadRequest(new { message = "Revision snapshot is corrupt or unreadable." });

        // Replace header fields
        estimate.Name = snapshot.Name;
        estimate.Client = snapshot.Client;
        estimate.ClientCode = snapshot.ClientCode;
        estimate.MsaNumber = snapshot.MsaNumber;
        estimate.JobType = snapshot.JobType;
        estimate.Branch = snapshot.Branch;
        estimate.City = snapshot.City;
        estimate.State = snapshot.State;
        estimate.Site = snapshot.Site;
        estimate.JobLetter = snapshot.JobLetter;
        estimate.Shift = snapshot.Shift;
        estimate.HoursPerShift = snapshot.HoursPerShift;
        estimate.Days = snapshot.Days;
        estimate.StartDate = snapshot.StartDate;
        estimate.EndDate = snapshot.EndDate;
        estimate.OtMethod = snapshot.OtMethod;
        estimate.DtWeekends = snapshot.DtWeekends;
        estimate.Status = snapshot.Status;
        estimate.ConfidencePct = snapshot.ConfidencePct;
        estimate.LostReason = snapshot.LostReason;
        estimate.UpdatedBy = Username;
        estimate.UpdatedAt = DateTimeOffset.UtcNow;

        // Replace labor rows
        db.LaborRows.RemoveRange(estimate.LaborRows);
        for (int i = 0; i < (snapshot.LaborRows?.Count ?? 0); i++)
        {
            var src = snapshot.LaborRows![i];
            db.LaborRows.Add(new LaborRow
            {
                EstimateId = estimateId,
                Position = src.Position,
                LaborType = src.LaborType,
                Shift = src.Shift,
                CraftCode = src.CraftCode,
                NavCode = src.NavCode,
                BillStRate = src.BillStRate,
                BillOtRate = src.BillOtRate,
                BillDtRate = src.BillDtRate,
                ScheduleJson = src.ScheduleJson,
                StHours = src.StHours,
                OtHours = src.OtHours,
                DtHours = src.DtHours,
                Subtotal = src.Subtotal,
                SortOrder = i
            });
        }

        // Replace equipment rows
        db.EquipmentRows.RemoveRange(estimate.EquipmentRows);
        for (int i = 0; i < (snapshot.EquipmentRows?.Count ?? 0); i++)
        {
            var src = snapshot.EquipmentRows![i];
            db.EquipmentRows.Add(new EquipmentRow
            {
                EstimateId = estimateId,
                Name = src.Name,
                RateType = src.RateType,
                Rate = src.Rate,
                Qty = src.Qty,
                Days = src.Days,
                Subtotal = src.Subtotal,
                SortOrder = i
            });
        }

        // Replace expense rows
        db.ExpenseRows.RemoveRange(estimate.ExpenseRows);
        for (int i = 0; i < (snapshot.ExpenseRows?.Count ?? 0); i++)
        {
            var src = snapshot.ExpenseRows![i];
            db.ExpenseRows.Add(new ExpenseRow
            {
                EstimateId = estimateId,
                Category = src.Category,
                Description = src.Description,
                Rate = src.Rate,
                Unit = src.Unit,
                DaysOrQty = src.DaysOrQty,
                People = src.People,
                Billable = src.Billable,
                Subtotal = src.Subtotal,
                SortOrder = i
            });
        }

        // Replace summary
        if (snapshot.Summary != null)
        {
            if (estimate.Summary == null)
            {
                estimate.Summary = new EstimateSummary { EstimateId = estimateId };
                db.EstimateSummaries.Add(estimate.Summary);
            }
            estimate.Summary.BillSubtotal = snapshot.Summary.BillSubtotal;
            estimate.Summary.DiscountType = snapshot.Summary.DiscountType;
            estimate.Summary.DiscountValue = snapshot.Summary.DiscountValue;
            estimate.Summary.DiscountAmount = snapshot.Summary.DiscountAmount;
            estimate.Summary.TaxRate = snapshot.Summary.TaxRate;
            estimate.Summary.TaxAmount = snapshot.Summary.TaxAmount;
            estimate.Summary.GrandTotal = snapshot.Summary.GrandTotal;
            estimate.Summary.InternalCostTotal = snapshot.Summary.InternalCostTotal;
            estimate.Summary.GrossProfit = snapshot.Summary.GrossProfit;
            estimate.Summary.GrossMarginPct = snapshot.Summary.GrossMarginPct;
            estimate.Summary.UpdatedAt = DateTimeOffset.UtcNow;
        }

        // Mark this revision as current, others as not
        await db.EstimateRevisions
            .Where(r => r.EstimateId == estimateId && r.IsCurrent)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsCurrent, false), ct);

        revision.IsCurrent = true;

        await db.SaveChangesAsync(ct);

        // Return refreshed estimate
        var updated = await db.Estimates
            .Include(e => e.LaborRows.OrderBy(r => r.SortOrder))
            .Include(e => e.EquipmentRows.OrderBy(r => r.SortOrder))
            .Include(e => e.ExpenseRows.OrderBy(r => r.SortOrder))
            .Include(e => e.Revisions.OrderBy(r => r.RevisionNumber))
            .Include(e => e.StaffingPlan)
            .Include(e => e.Summary)
            .FirstOrDefaultAsync(e => e.EstimateId == estimateId, ct);

        return Ok(updated);
    }

    // ── Delete ────────────────────────────────────────────────────────────
    [HttpDelete("{revId:int}")]
    public async Task<IActionResult> Delete(int estimateId, int revId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (!await db.Estimates.AnyAsync(e => e.EstimateId == estimateId && e.CompanyCode == CompanyCode, ct))
            return NotFound();

        var count = await db.EstimateRevisions.CountAsync(r => r.EstimateId == estimateId, ct);
        if (count <= 1)
            return BadRequest(new { message = "Cannot delete the only revision." });

        var revision = await db.EstimateRevisions
            .FirstOrDefaultAsync(r => r.EstimateRevisionId == revId && r.EstimateId == estimateId, ct);

        if (revision == null) return NotFound();

        db.EstimateRevisions.Remove(revision);
        await db.SaveChangesAsync(ct);

        // If we just deleted the current revision, promote the highest remaining one
        var wasCurrentDeleted = revision.IsCurrent;
        if (wasCurrentDeleted)
        {
            await using var db2 = await _dbFactory.CreateDbContextAsync(ct);
            var latest = await db2.EstimateRevisions
                .Where(r => r.EstimateId == estimateId)
                .OrderByDescending(r => r.RevisionNumber)
                .FirstOrDefaultAsync(ct);
            if (latest != null)
            {
                latest.IsCurrent = true;
                await db2.SaveChangesAsync(ct);
            }
        }

        return NoContent();
    }
}

// ── Snapshot DTOs ─────────────────────────────────────────────────────────────

public class EstimateSnapshotDto
{
    public string Name { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;
    public string? ClientCode { get; set; }
    public string? MsaNumber { get; set; }
    public string? JobType { get; set; }
    public string? Branch { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Site { get; set; }
    public string? JobLetter { get; set; }
    public string Shift { get; set; } = "Day";
    public decimal HoursPerShift { get; set; }
    public int Days { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string OtMethod { get; set; } = "daily8_weekly40";
    public string DtWeekends { get; set; } = "none";
    public string Status { get; set; } = "Draft";
    public decimal ConfidencePct { get; set; }
    public string? LostReason { get; set; }
    public List<LaborRowSnapshotDto>? LaborRows { get; set; }
    public List<EquipmentRowSnapshotDto>? EquipmentRows { get; set; }
    public List<ExpenseRowSnapshotDto>? ExpenseRows { get; set; }
    public SummarySnapshotDto? Summary { get; set; }

    public static EstimateSnapshotDto FromEstimate(Estimate e) => new()
    {
        Name = e.Name,
        Client = e.Client,
        ClientCode = e.ClientCode,
        MsaNumber = e.MsaNumber,
        JobType = e.JobType,
        Branch = e.Branch,
        City = e.City,
        State = e.State,
        Site = e.Site,
        JobLetter = e.JobLetter,
        Shift = e.Shift,
        HoursPerShift = e.HoursPerShift,
        Days = e.Days,
        StartDate = e.StartDate,
        EndDate = e.EndDate,
        OtMethod = e.OtMethod,
        DtWeekends = e.DtWeekends,
        Status = e.Status,
        ConfidencePct = e.ConfidencePct,
        LostReason = e.LostReason,
        LaborRows = e.LaborRows.Select(r => new LaborRowSnapshotDto
        {
            Position = r.Position,
            LaborType = r.LaborType,
            Shift = r.Shift,
            CraftCode = r.CraftCode,
            NavCode = r.NavCode,
            BillStRate = r.BillStRate,
            BillOtRate = r.BillOtRate,
            BillDtRate = r.BillDtRate,
            ScheduleJson = r.ScheduleJson,
            StHours = r.StHours,
            OtHours = r.OtHours,
            DtHours = r.DtHours,
            Subtotal = r.Subtotal
        }).ToList(),
        EquipmentRows = e.EquipmentRows.Select(r => new EquipmentRowSnapshotDto
        {
            Name = r.Name,
            RateType = r.RateType,
            Rate = r.Rate,
            Qty = r.Qty,
            Days = r.Days,
            Subtotal = r.Subtotal
        }).ToList(),
        ExpenseRows = e.ExpenseRows.Select(r => new ExpenseRowSnapshotDto
        {
            Category = r.Category,
            Description = r.Description,
            Rate = r.Rate,
            Unit = r.Unit,
            DaysOrQty = r.DaysOrQty,
            People = r.People,
            Billable = r.Billable,
            Subtotal = r.Subtotal
        }).ToList(),
        Summary = e.Summary == null ? null : new SummarySnapshotDto
        {
            BillSubtotal = e.Summary.BillSubtotal,
            DiscountType = e.Summary.DiscountType,
            DiscountValue = e.Summary.DiscountValue,
            DiscountAmount = e.Summary.DiscountAmount,
            TaxRate = e.Summary.TaxRate,
            TaxAmount = e.Summary.TaxAmount,
            GrandTotal = e.Summary.GrandTotal,
            InternalCostTotal = e.Summary.InternalCostTotal,
            GrossProfit = e.Summary.GrossProfit,
            GrossMarginPct = e.Summary.GrossMarginPct
        }
    };
}

public class LaborRowSnapshotDto
{
    public string Position { get; set; } = string.Empty;
    public string LaborType { get; set; } = "Direct";
    public string Shift { get; set; } = "Day";
    public string? CraftCode { get; set; }
    public string? NavCode { get; set; }
    public decimal BillStRate { get; set; }
    public decimal BillOtRate { get; set; }
    public decimal BillDtRate { get; set; }
    public string? ScheduleJson { get; set; }
    public decimal StHours { get; set; }
    public decimal OtHours { get; set; }
    public decimal DtHours { get; set; }
    public decimal Subtotal { get; set; }
}

public class EquipmentRowSnapshotDto
{
    public string Name { get; set; } = string.Empty;
    public string RateType { get; set; } = "Daily";
    public decimal Rate { get; set; }
    public int Qty { get; set; }
    public int Days { get; set; }
    public decimal Subtotal { get; set; }
}

public class ExpenseRowSnapshotDto
{
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public string Unit { get; set; } = "Day";
    public int DaysOrQty { get; set; }
    public int People { get; set; }
    public bool Billable { get; set; }
    public decimal Subtotal { get; set; }
}

public class SummarySnapshotDto
{
    public decimal BillSubtotal { get; set; }
    public string DiscountType { get; set; } = "None";
    public decimal DiscountValue { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal InternalCostTotal { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal GrossMarginPct { get; set; }
}

public record CreateRevisionRequestDto(string? Description);
