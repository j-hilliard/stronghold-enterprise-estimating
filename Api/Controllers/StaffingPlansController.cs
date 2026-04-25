using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Api.Services;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/staffing-plans")]
[Authorize]
public class StaffingPlansController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly EstimateNumberService _numberService;

    public StaffingPlansController(IDbContextFactory<AppDbContext> dbFactory, EstimateNumberService numberService)
    {
        _dbFactory = dbFactory;
        _numberService = numberService;
    }

    private string CompanyCode => User.FindFirst("company_code")?.Value ?? string.Empty;

    private string Username => User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                               ?? User.FindFirst("username")?.Value
                               ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? branch,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var q = db.StaffingPlans
            .Where(sp => sp.CompanyCode == CompanyCode)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(sp => sp.StaffingPlanNumber.Contains(search)
                              || sp.Name.Contains(search)
                              || sp.Client.Contains(search));

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(sp => sp.Status == status);

        if (!string.IsNullOrWhiteSpace(branch))
            q = q.Where(sp => sp.Branch != null && sp.Branch == branch);

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(sp => sp.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(sp => new
            {
                sp.StaffingPlanId,
                sp.StaffingPlanNumber,
                sp.Name,
                sp.Client,
                sp.Status,
                sp.Branch,
                sp.City,
                sp.State,
                sp.StartDate,
                sp.EndDate,
                sp.ConvertedEstimateId,
                sp.RoughLaborTotal,
                sp.UpdatedAt
            })
            .ToListAsync(ct);

        var ids = items.Select(x => x.StaffingPlanId).ToList();

        var allLaborRows = await db.StaffingLaborRows
            .Where(r => ids.Contains(r.StaffingPlanId))
            .OrderBy(r => r.SortOrder)
            .Select(r => new { r.StaffingPlanId, r.Position, r.Shift })
            .ToListAsync(ct);

        var laborPreviewByPlan = allLaborRows
            .GroupBy(r => r.StaffingPlanId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var grouped = g
                        .GroupBy(x => new { Position = x.Position.Trim(), Shift = x.Shift.Trim() })
                        .Select(x => new
                        {
                            x.Key.Position,
                            x.Key.Shift,
                            Count = x.Count()
                        })
                        .ToList();

                    var preview = grouped
                        .Take(4)
                        .Select(x => $"{x.Count}x {x.Position}" + (string.IsNullOrWhiteSpace(x.Shift) ? "" : $" ({x.Shift})"))
                        .ToList();

                    return new
                    {
                        LaborPreview = preview,
                        LaborMoreCount = Math.Max(0, grouped.Count - preview.Count)
                    };
                });

        var results = items.Select(sp =>
        {
            laborPreviewByPlan.TryGetValue(sp.StaffingPlanId, out var pills);
            return new
            {
                sp.StaffingPlanId,
                sp.StaffingPlanNumber,
                sp.Name,
                sp.Client,
                sp.Status,
                sp.Branch,
                sp.City,
                sp.State,
                sp.StartDate,
                sp.EndDate,
                sp.ConvertedEstimateId,
                sp.RoughLaborTotal,
                sp.UpdatedAt,
                laborPreview = pills?.LaborPreview ?? new List<string>(),
                laborMoreCount = pills?.LaborMoreCount ?? 0
            };
        });

        return Ok(new { total, page, pageSize, items = results });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var sp = await db.StaffingPlans
            .Include(s => s.LaborRows.OrderBy(r => r.SortOrder))
            .FirstOrDefaultAsync(s => s.StaffingPlanId == id && s.CompanyCode == CompanyCode, ct);

        if (sp == null) return NotFound();
        return Ok(sp);
    }

    [HttpGet("next-number")]
    public async Task<IActionResult> NextNumber(
        [FromQuery] string? jobLetter,
        [FromQuery] string? clientCode,
        CancellationToken ct = default)
    {
        var number = await _numberService.NextStaffingPlanNumberAsync(CompanyCode, jobLetter, clientCode, ct);
        return Ok(new { number });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] StaffingPlanUpsertDto dto, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var number = await _numberService.NextStaffingPlanNumberAsync(
            CompanyCode, dto.JobLetter, dto.ClientCode, ct);

        var sp = new StaffingPlan
        {
            CompanyCode = CompanyCode,
            StaffingPlanNumber = number,
            Name = dto.Name,
            Client = dto.Client,
            ClientCode = dto.ClientCode,
            Branch = dto.Branch,
            City = dto.City,
            State = dto.State,
            JobLetter = dto.JobLetter,
            Status = dto.Status ?? "Draft",
            Shift = dto.Shift,
            HoursPerShift = dto.HoursPerShift,
            Days = dto.Days,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            OtMethod = dto.OtMethod,
            DtWeekends = dto.DtWeekends,
            CreatedBy = Username,
            UpdatedBy = Username,
        };

        db.StaffingPlans.Add(sp);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = sp.StaffingPlanId, version = "1.0" }, new { sp.StaffingPlanId, sp.StaffingPlanNumber });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] StaffingPlanUpsertDto dto, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var sp = await db.StaffingPlans
            .FirstOrDefaultAsync(s => s.StaffingPlanId == id && s.CompanyCode == CompanyCode, ct);

        if (sp == null) return NotFound();

        sp.Name = dto.Name;
        sp.Client = dto.Client;
        sp.ClientCode = dto.ClientCode;
        sp.Branch = dto.Branch;
        sp.City = dto.City;
        sp.State = dto.State;
        sp.JobLetter = dto.JobLetter;
        sp.Status = dto.Status ?? sp.Status;
        sp.Shift = dto.Shift;
        sp.HoursPerShift = dto.HoursPerShift;
        sp.Days = dto.Days;
        sp.StartDate = dto.StartDate;
        sp.EndDate = dto.EndDate;
        sp.OtMethod = dto.OtMethod;
        sp.DtWeekends = dto.DtWeekends;
        sp.UpdatedBy = Username;
        sp.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var sp = await db.StaffingPlans
            .Include(s => s.LaborRows)
            .FirstOrDefaultAsync(s => s.StaffingPlanId == id && s.CompanyCode == CompanyCode, ct);

        if (sp == null) return NotFound();
        if (sp.ConvertedEstimateId.HasValue)
            return BadRequest(new { message = "Converted staffing plans cannot be deleted." });

        db.StaffingLaborRows.RemoveRange(sp.LaborRows);
        db.StaffingPlans.Remove(sp);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:int}/duplicate")]
    public async Task<IActionResult> Duplicate(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var source = await db.StaffingPlans
            .Include(s => s.LaborRows)
            .FirstOrDefaultAsync(s => s.StaffingPlanId == id && s.CompanyCode == CompanyCode, ct);

        if (source == null) return NotFound();

        var number = await _numberService.NextStaffingPlanNumberAsync(
            CompanyCode, source.JobLetter, source.ClientCode, ct);

        var duplicate = new StaffingPlan
        {
            CompanyCode = CompanyCode,
            StaffingPlanNumber = number,
            Name = string.IsNullOrWhiteSpace(source.Name) ? "Copy" : $"{source.Name} (Copy)",
            Client = source.Client,
            ClientCode = source.ClientCode,
            Branch = source.Branch,
            City = source.City,
            State = source.State,
            JobLetter = source.JobLetter,
            Status = "Draft",
            Shift = source.Shift,
            HoursPerShift = source.HoursPerShift,
            Days = source.Days,
            StartDate = source.StartDate,
            EndDate = source.EndDate,
            OtMethod = source.OtMethod,
            DtWeekends = source.DtWeekends,
            RoughLaborTotal = source.RoughLaborTotal,
            CreatedBy = Username,
            UpdatedBy = Username
        };

        foreach (var row in source.LaborRows.OrderBy(r => r.SortOrder))
        {
            duplicate.LaborRows.Add(new StaffingLaborRow
            {
                Position = row.Position,
                LaborType = row.LaborType,
                Shift = row.Shift,
                CraftCode = row.CraftCode,
                NavCode = row.NavCode,
                StRate = row.StRate,
                OtRate = row.OtRate,
                DtRate = row.DtRate,
                SortOrder = row.SortOrder,
                ScheduleJson = row.ScheduleJson,
                StHours = row.StHours,
                OtHours = row.OtHours,
                DtHours = row.DtHours,
                Subtotal = row.Subtotal
            });
        }

        db.StaffingPlans.Add(duplicate);
        await db.SaveChangesAsync(ct);

        return Ok(new { staffingPlanId = duplicate.StaffingPlanId, staffingPlanNumber = duplicate.StaffingPlanNumber });
    }

    [HttpPost("{id:int}/labor")]
    public async Task<IActionResult> UpsertLaborRows(int id, [FromBody] List<StaffingLaborRowDto> rows, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (!await db.StaffingPlans.AnyAsync(s => s.StaffingPlanId == id && s.CompanyCode == CompanyCode, ct))
            return NotFound();

        var existing = await db.StaffingLaborRows.Where(r => r.StaffingPlanId == id).ToListAsync(ct);
        db.StaffingLaborRows.RemoveRange(existing);

        for (int i = 0; i < rows.Count; i++)
        {
            var dto = rows[i];
            db.StaffingLaborRows.Add(new StaffingLaborRow
            {
                StaffingPlanId = id,
                Position = dto.Position,
                LaborType = dto.LaborType,
                Shift = dto.Shift,
                CraftCode = dto.CraftCode,
                NavCode = dto.NavCode,
                StRate = dto.StRate,
                OtRate = dto.OtRate,
                DtRate = dto.DtRate,
                ScheduleJson = dto.ScheduleJson,
                StHours = dto.StHours,
                OtHours = dto.OtHours,
                DtHours = dto.DtHours,
                Subtotal = dto.Subtotal,
                SortOrder = i
            });
        }

        var sp = (await db.StaffingPlans.FindAsync(new object[] { id }, ct))!;
        sp.RoughLaborTotal = rows.Sum(r => r.Subtotal);
        sp.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:int}/convert")]
    public async Task<IActionResult> Convert(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var sp = await db.StaffingPlans
            .Include(s => s.LaborRows)
            .FirstOrDefaultAsync(s => s.StaffingPlanId == id && s.CompanyCode == CompanyCode, ct);

        if (sp == null) return NotFound();
        if (sp.ConvertedEstimateId.HasValue)
            return BadRequest(new { message = "This staffing plan has already been converted." });

        var estNumber = await _numberService.NextEstimateNumberAsync(
            CompanyCode, sp.JobLetter, sp.ClientCode, ct);

        var estimate = new Estimate
        {
            CompanyCode = CompanyCode,
            EstimateNumber = estNumber,
            StaffingPlanId = sp.StaffingPlanId,
            Name = sp.Name,
            Client = sp.Client,
            ClientCode = sp.ClientCode,
            Branch = sp.Branch,
            City = sp.City,
            State = sp.State,
            JobLetter = sp.JobLetter,
            Shift = sp.Shift,
            HoursPerShift = sp.HoursPerShift,
            Days = sp.Days,
            StartDate = sp.StartDate,
            EndDate = sp.EndDate,
            OtMethod = sp.OtMethod,
            DtWeekends = sp.DtWeekends,
            Status = "Draft",
            ConfidencePct = 50,
            CreatedBy = Username,
            UpdatedBy = Username,
        };

        foreach (var row in sp.LaborRows)
        {
            estimate.LaborRows.Add(new LaborRow
            {
                Position = row.Position,
                LaborType = row.LaborType,
                Shift = row.Shift,
                CraftCode = row.CraftCode,
                NavCode = row.NavCode,
                BillStRate = row.StRate,
                BillOtRate = row.OtRate,
                BillDtRate = row.DtRate,
                ScheduleJson = row.ScheduleJson,
                StHours = row.StHours,
                OtHours = row.OtHours,
                DtHours = row.DtHours,
                Subtotal = row.Subtotal,
                SortOrder = row.SortOrder
            });
        }

        db.Estimates.Add(estimate);
        await db.SaveChangesAsync(ct);

        sp.ConvertedEstimateId = estimate.EstimateId;
        sp.Status = "Converted";
        sp.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Ok(new { estimateId = estimate.EstimateId, estimateNumber = estNumber });
    }
}

public record StaffingPlanUpsertDto(
    string Name, string Client, string? ClientCode,
    string? Branch, string? City, string? State, string? JobLetter,
    string? Status, string Shift, decimal HoursPerShift, int Days,
    DateTime? StartDate, DateTime? EndDate,
    string OtMethod, bool DtWeekends);

public record StaffingLaborRowDto(
    string Position, string LaborType, string Shift,
    string? CraftCode, string? NavCode,
    decimal StRate, decimal OtRate, decimal DtRate,
    string? ScheduleJson,
    decimal StHours, decimal OtHours, decimal DtHours, decimal Subtotal);
