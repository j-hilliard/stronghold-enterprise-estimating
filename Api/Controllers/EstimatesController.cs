using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Api.Services;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/estimates")]
[Authorize]
public class EstimatesController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly EstimateNumberService _numberService;
    private readonly ProposalPdfService _proposalPdf;

    public EstimatesController(IDbContextFactory<AppDbContext> dbFactory, EstimateNumberService numberService, ProposalPdfService proposalPdf)
    {
        _dbFactory = dbFactory;
        _numberService = numberService;
        _proposalPdf = proposalPdf;
    }

    private string CompanyCode => User.FindFirst("company_code")?.Value ?? string.Empty;
    private string Username => User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                            ?? User.FindFirst("username")?.Value ?? string.Empty;

    // ── List ──────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? client,
        [FromQuery] DateTime? startAfter,
        [FromQuery] DateTime? startBefore,
        [FromQuery] int? year,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var q = db.Estimates
            .Include(e => e.Summary)
            .Where(e => e.CompanyCode == CompanyCode && !e.IsScenario)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(e => e.EstimateNumber.Contains(search)
                           || e.Name.Contains(search)
                           || e.Client.Contains(search));

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(e => e.Status == status);

        if (!string.IsNullOrWhiteSpace(client))
            q = q.Where(e => e.Client.Contains(client));

        if (startAfter.HasValue)
            q = q.Where(e => e.StartDate >= startAfter);

        if (startBefore.HasValue)
            q = q.Where(e => e.StartDate <= startBefore);

        if (year.HasValue)
            q = q.Where(e => e.StartDate.HasValue && e.StartDate.Value.Year == year.Value);

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(e => e.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                e.EstimateId,
                e.EstimateNumber,
                e.Name,
                e.Client,
                e.Status,
                e.Branch,
                e.City,
                e.State,
                e.StartDate,
                e.EndDate,
                e.ConfidencePct,
                e.StaffingPlanId,
                e.IsScenario,
                e.LostReason,
                GrandTotal = e.Summary != null ? e.Summary.GrandTotal : 0m,
                e.CreatedBy,
                e.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new { total, page, pageSize, items });
    }

    // ── Get single ────────────────────────────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var estimate = await db.Estimates
            .Include(e => e.LaborRows.OrderBy(r => r.SortOrder))
            .Include(e => e.EquipmentRows.OrderBy(r => r.SortOrder))
            .Include(e => e.ExpenseRows.OrderBy(r => r.SortOrder))
            .Include(e => e.FcoEntries.OrderBy(f => f.CreatedAt))
            .Include(e => e.Revisions.OrderBy(r => r.RevisionNumber))
            .Include(e => e.StaffingPlan)
            .Include(e => e.Summary)
            .FirstOrDefaultAsync(e => e.EstimateId == id && e.CompanyCode == CompanyCode, ct);

        if (estimate == null) return NotFound();

        return Ok(estimate);
    }

    // ── Next number ───────────────────────────────────────────────────────
    [HttpGet("next-number")]
    public async Task<IActionResult> NextNumber(
        [FromQuery] string? jobLetter,
        [FromQuery] string? clientCode,
        CancellationToken ct = default)
    {
        var number = await _numberService.NextEstimateNumberAsync(CompanyCode, jobLetter, clientCode, ct);
        return Ok(new { number });
    }

    // ── Create ────────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EstimateUpsertDto dto, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var number = await _numberService.NextEstimateNumberAsync(
            CompanyCode, dto.JobLetter, dto.ClientCode, ct);

        var estimate = new Estimate
        {
            CompanyCode = CompanyCode,
            EstimateNumber = number,
            Name = dto.Name,
            Client = dto.Client,
            ClientCode = dto.ClientCode,
            MsaNumber = dto.MsaNumber,
            JobType = dto.JobType,
            Branch = dto.Branch,
            City = dto.City,
            State = dto.State,
            Site = dto.Site,
            JobLetter = dto.JobLetter,
            Shift = dto.Shift,
            HoursPerShift = dto.HoursPerShift,
            Days = dto.Days,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            OtMethod = dto.OtMethod,
            DtWeekends = dto.DtWeekends,
            Status = dto.Status ?? "Draft",
            ConfidencePct = dto.ConfidencePct,
            StaffingPlanId = dto.StaffingPlanId,
            VP = dto.VP,
            Director = dto.Director,
            Region = dto.Region,
            RateBookId = dto.RateBookId,
            CreatedBy = Username,
            UpdatedBy = Username,
        };

        db.Estimates.Add(estimate);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = estimate.EstimateId, version = "1.0" }, new { estimate.EstimateId, estimate.EstimateNumber });
    }

    // ── Update ────────────────────────────────────────────────────────────
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] EstimateUpsertDto dto, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var estimate = await db.Estimates
            .FirstOrDefaultAsync(e => e.EstimateId == id && e.CompanyCode == CompanyCode, ct);

        if (estimate == null) return NotFound();

        estimate.Name = dto.Name;
        estimate.Client = dto.Client;
        estimate.ClientCode = dto.ClientCode;
        estimate.MsaNumber = dto.MsaNumber;
        estimate.JobType = dto.JobType;
        estimate.Branch = dto.Branch;
        estimate.City = dto.City;
        estimate.State = dto.State;
        estimate.Site = dto.Site;
        estimate.JobLetter = dto.JobLetter;
        estimate.Shift = dto.Shift;
        estimate.HoursPerShift = dto.HoursPerShift;
        estimate.Days = dto.Days;
        estimate.StartDate = dto.StartDate;
        estimate.EndDate = dto.EndDate;
        estimate.OtMethod = dto.OtMethod;
        estimate.DtWeekends = dto.DtWeekends;
        estimate.Status = dto.Status ?? estimate.Status;
        estimate.ConfidencePct = dto.ConfidencePct;
        estimate.LostReason = dto.LostReason;
        estimate.LostNotes = dto.LostNotes;
        estimate.IsScenario = dto.IsScenario;
        estimate.VP = dto.VP;
        estimate.Director = dto.Director;
        estimate.Region = dto.Region;
        estimate.RateBookId = dto.RateBookId;
        estimate.UpdatedBy = Username;
        estimate.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Delete single ─────────────────────────────────────────────────────
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var estimate = await db.Estimates
            .FirstOrDefaultAsync(e => e.EstimateId == id && e.CompanyCode == CompanyCode, ct);

        if (estimate == null) return NotFound();

        db.Estimates.Remove(estimate);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Bulk delete ───────────────────────────────────────────────────────
    [HttpDelete]
    public async Task<IActionResult> BulkDelete([FromBody] List<int> ids, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var estimates = await db.Estimates
            .Where(e => ids.Contains(e.EstimateId) && e.CompanyCode == CompanyCode)
            .ToListAsync(ct);

        db.Estimates.RemoveRange(estimates);
        await db.SaveChangesAsync(ct);
        return Ok(new { deleted = estimates.Count });
    }

    // ── Labor rows ────────────────────────────────────────────────────────
    [HttpPost("{id:int}/labor")]
    public async Task<IActionResult> UpsertLaborRows(int id, [FromBody] List<LaborRowDto> rows, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (!await db.Estimates.AnyAsync(e => e.EstimateId == id && e.CompanyCode == CompanyCode, ct))
            return NotFound();

        var existing = await db.LaborRows.Where(r => r.EstimateId == id).ToListAsync(ct);
        db.LaborRows.RemoveRange(existing);

        for (int i = 0; i < rows.Count; i++)
        {
            var dto = rows[i];
            db.LaborRows.Add(new LaborRow
            {
                EstimateId = id,
                Position = dto.Position,
                LaborType = dto.LaborType,
                Shift = dto.Shift,
                CraftCode = dto.CraftCode,
                NavCode = dto.NavCode,
                BillStRate = dto.BillStRate,
                BillOtRate = dto.BillOtRate,
                BillDtRate = dto.BillDtRate,
                ScheduleJson = dto.ScheduleJson,
                StHours = dto.StHours,
                OtHours = dto.OtHours,
                DtHours = dto.DtHours,
                Subtotal = dto.Subtotal,
                SortOrder = i
            });
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Equipment rows ────────────────────────────────────────────────────
    [HttpPost("{id:int}/equipment")]
    public async Task<IActionResult> UpsertEquipmentRows(int id, [FromBody] List<EquipmentRowDto> rows, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (!await db.Estimates.AnyAsync(e => e.EstimateId == id && e.CompanyCode == CompanyCode, ct))
            return NotFound();

        var existing = await db.EquipmentRows.Where(r => r.EstimateId == id).ToListAsync(ct);
        db.EquipmentRows.RemoveRange(existing);

        for (int i = 0; i < rows.Count; i++)
        {
            var dto = rows[i];
            db.EquipmentRows.Add(new EquipmentRow
            {
                EstimateId = id,
                Name = dto.Name,
                RateType = dto.RateType,
                Rate = dto.Rate,
                Qty = dto.Qty,
                Days = dto.Days,
                Subtotal = dto.Subtotal,
                SortOrder = i
            });
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Expense rows ──────────────────────────────────────────────────────
    [HttpPost("{id:int}/expenses")]
    public async Task<IActionResult> UpsertExpenseRows(int id, [FromBody] List<ExpenseRowDto> rows, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (!await db.Estimates.AnyAsync(e => e.EstimateId == id && e.CompanyCode == CompanyCode, ct))
            return NotFound();

        var existing = await db.ExpenseRows.Where(r => r.EstimateId == id).ToListAsync(ct);
        db.ExpenseRows.RemoveRange(existing);

        for (int i = 0; i < rows.Count; i++)
        {
            var dto = rows[i];
            db.ExpenseRows.Add(new ExpenseRow
            {
                EstimateId = id,
                Category = dto.Category,
                Description = dto.Description,
                Rate = dto.Rate,
                Unit = dto.Unit,
                DaysOrQty = dto.DaysOrQty,
                People = dto.People,
                Billable = dto.Billable,
                Subtotal = dto.Subtotal,
                SortOrder = i
            });
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Summary ───────────────────────────────────────────────────────────
    [HttpPut("{id:int}/summary")]
    public async Task<IActionResult> UpsertSummary(int id, [FromBody] SummaryDto dto, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (!await db.Estimates.AnyAsync(e => e.EstimateId == id && e.CompanyCode == CompanyCode, ct))
            return NotFound();

        var summary = await db.EstimateSummaries.FirstOrDefaultAsync(s => s.EstimateId == id, ct);
        if (summary == null)
        {
            summary = new EstimateSummary { EstimateId = id };
            db.EstimateSummaries.Add(summary);
        }

        summary.BillSubtotal = dto.BillSubtotal;
        summary.DiscountType = dto.DiscountType;
        summary.DiscountValue = dto.DiscountValue;
        summary.DiscountAmount = dto.DiscountAmount;
        summary.TaxRate = dto.TaxRate;
        summary.TaxAmount = dto.TaxAmount;
        summary.GrandTotal = dto.GrandTotal;
        summary.InternalCostTotal = dto.InternalCostTotal;
        summary.GrossProfit = dto.GrossProfit;
        summary.GrossMarginPct = dto.GrossMarginPct;
        summary.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Clone ─────────────────────────────────────────────────────────────
    [HttpPost("{id:int}/clone")]
    public async Task<IActionResult> Clone(int id, [FromQuery] bool asScenario = false, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var src = await db.Estimates
            .Include(e => e.LaborRows)
            .Include(e => e.EquipmentRows)
            .Include(e => e.ExpenseRows)
            .Include(e => e.Summary)
            .FirstOrDefaultAsync(e => e.EstimateId == id && e.CompanyCode == CompanyCode, ct);

        if (src == null) return NotFound();

        var newNumber = await _numberService.NextEstimateNumberAsync(CompanyCode, src.JobLetter, src.ClientCode, ct);

        var copy = new Estimate
        {
            CompanyCode   = CompanyCode,
            EstimateNumber = newNumber,
            Name          = $"{src.Name} (Copy)",
            Client        = src.Client,
            ClientCode    = src.ClientCode,
            MsaNumber     = src.MsaNumber,
            JobType       = src.JobType,
            Branch        = src.Branch,
            City          = src.City,
            State         = src.State,
            Site          = src.Site,
            JobLetter     = src.JobLetter,
            Shift         = src.Shift,
            HoursPerShift = src.HoursPerShift,
            Days          = src.Days,
            StartDate     = src.StartDate,
            EndDate       = src.EndDate,
            OtMethod      = src.OtMethod,
            DtWeekends    = src.DtWeekends,
            Status        = "Draft",
            ConfidencePct = src.ConfidencePct,
            IsScenario    = asScenario,
            CreatedBy     = Username,
            UpdatedBy     = Username,
        };

        db.Estimates.Add(copy);
        await db.SaveChangesAsync(ct);

        foreach (var r in src.LaborRows)
            db.LaborRows.Add(new LaborRow { EstimateId = copy.EstimateId, Position = r.Position, LaborType = r.LaborType, Shift = r.Shift, CraftCode = r.CraftCode, NavCode = r.NavCode, BillStRate = r.BillStRate, BillOtRate = r.BillOtRate, BillDtRate = r.BillDtRate, ScheduleJson = r.ScheduleJson, StHours = r.StHours, OtHours = r.OtHours, DtHours = r.DtHours, Subtotal = r.Subtotal, SortOrder = r.SortOrder });

        foreach (var r in src.EquipmentRows)
            db.EquipmentRows.Add(new EquipmentRow { EstimateId = copy.EstimateId, Name = r.Name, RateType = r.RateType, Rate = r.Rate, Qty = r.Qty, Days = r.Days, Subtotal = r.Subtotal, SortOrder = r.SortOrder });

        foreach (var r in src.ExpenseRows)
            db.ExpenseRows.Add(new ExpenseRow { EstimateId = copy.EstimateId, Category = r.Category, Description = r.Description, Rate = r.Rate, Unit = r.Unit, DaysOrQty = r.DaysOrQty, People = r.People, Billable = r.Billable, Subtotal = r.Subtotal, SortOrder = r.SortOrder });

        if (src.Summary != null)
            db.EstimateSummaries.Add(new EstimateSummary { EstimateId = copy.EstimateId, BillSubtotal = src.Summary.BillSubtotal, DiscountType = src.Summary.DiscountType, DiscountValue = src.Summary.DiscountValue, DiscountAmount = src.Summary.DiscountAmount, TaxRate = src.Summary.TaxRate, TaxAmount = src.Summary.TaxAmount, GrandTotal = src.Summary.GrandTotal, InternalCostTotal = src.Summary.InternalCostTotal, GrossProfit = src.Summary.GrossProfit, GrossMarginPct = src.Summary.GrossMarginPct });

        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = copy.EstimateId, version = "1.0" },
            new { copy.EstimateId, copy.EstimateNumber, copy.IsScenario });
    }

    // ── Proposal PDF ──────────────────────────────────────────────────────
    [HttpGet("{id:int}/proposal.pdf")]
    public async Task<IActionResult> DownloadProposal(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var estimate = await db.Estimates
            .Include(e => e.LaborRows)
            .Include(e => e.EquipmentRows)
            .Include(e => e.ExpenseRows)
            .Include(e => e.Summary)
            .FirstOrDefaultAsync(e => e.EstimateId == id && e.CompanyCode == CompanyCode, ct);

        if (estimate == null) return NotFound();
        if (estimate.Status is "Lost" or "Archived")
            return BadRequest(new { message = "Cannot generate proposal for Lost or Archived estimates." });

        var bytes = _proposalPdf.GeneratePdf(estimate);
        return File(bytes, "application/pdf", $"{estimate.EstimateNumber}-proposal.pdf");
    }

    // ── Patch Status (used by AI agent) ──────────────────────────────────────
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> PatchStatus(int id, [FromBody] PatchStatusDto dto, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var estimate = await db.Estimates
            .FirstOrDefaultAsync(e => e.EstimateId == id && e.CompanyCode == CompanyCode, ct);
        if (estimate == null) return NotFound();

        var validStatuses = new[] { "Draft", "Pending", "Submitted", "Submitted for Approval", "Awarded", "Lost", "Active", "Archived" };
        if (!validStatuses.Contains(dto.Status))
            return BadRequest(new { message = $"Invalid status '{dto.Status}'. Valid: {string.Join(", ", validStatuses)}" });

        if (dto.Status == "Lost" && string.IsNullOrWhiteSpace(dto.LostReason))
            return BadRequest(new { message = "A lost reason is required when marking an estimate as Lost." });

        estimate.Status = dto.Status;
        if (!string.IsNullOrWhiteSpace(dto.LostReason))
            estimate.LostReason = dto.LostReason;
        await db.SaveChangesAsync(ct);
        return Ok(new { estimateId = id, estimateNumber = estimate.EstimateNumber, status = estimate.Status });
    }

}

// ── DTOs ──────────────────────────────────────────────────────────────────
public record EstimateUpsertDto(
    string Name, string Client, string? ClientCode, string? MsaNumber,
    string? JobType, string? Branch, string? City, string? State, string? Site, string? JobLetter,
    string Shift, decimal HoursPerShift, int Days,
    DateTime? StartDate, DateTime? EndDate,
    string OtMethod, string DtWeekends,
    string? Status, decimal ConfidencePct, string? LostReason, string? LostNotes,
    bool IsScenario,
    int? StaffingPlanId,
    string? VP, string? Director, string? Region,
    int? RateBookId = null);

public record LaborRowDto(
    string Position, string LaborType, string Shift,
    string? CraftCode, string? NavCode,
    decimal BillStRate, decimal BillOtRate, decimal BillDtRate,
    string? ScheduleJson,
    decimal StHours, decimal OtHours, decimal DtHours, decimal Subtotal);

public record EquipmentRowDto(
    string Name, string RateType, decimal Rate, int Qty, int Days, decimal Subtotal);

public record ExpenseRowDto(
    string Category, string Description, decimal Rate, string Unit,
    int DaysOrQty, int People, bool Billable, decimal Subtotal);

public record SummaryDto(
    decimal BillSubtotal, string DiscountType, decimal DiscountValue, decimal DiscountAmount,
    decimal TaxRate, decimal TaxAmount, decimal GrandTotal,
    decimal InternalCostTotal, decimal GrossProfit, decimal GrossMarginPct);

public record PatchStatusDto(string Status, string? LostReason = null);

