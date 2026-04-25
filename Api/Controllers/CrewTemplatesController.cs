using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/crew-templates")]
[Authorize]
public class CrewTemplatesController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public CrewTemplatesController(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    private string CompanyCode => User.FindFirst("company_code")?.Value ?? string.Empty;
    private string Username => User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                            ?? User.FindFirst("username")?.Value ?? "unknown";

    // GET /api/v1/crew-templates
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var templates = await db.CrewTemplates
            .Where(t => t.CompanyCode == CompanyCode)
            .Select(t => new
            {
                t.CrewTemplateId,
                t.Name,
                t.Description,
                RowCount = t.Rows.Count,
                Positions = t.Rows
                    .OrderBy(r => r.SortOrder)
                    .Select(r => new { r.Position, r.Qty, r.Shift })
                    .ToList(),
            })
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        return Ok(templates);
    }

    // GET /api/v1/crew-templates/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var template = await db.CrewTemplates
            .Include(t => t.Rows)
            .FirstOrDefaultAsync(t => t.CrewTemplateId == id && t.CompanyCode == CompanyCode, ct);

        if (template == null) return NotFound();

        return Ok(new
        {
            template.CrewTemplateId,
            template.Name,
            template.Description,
            template.CreatedBy,
            template.CreatedAt,
            Rows = template.Rows.OrderBy(r => r.SortOrder).Select(r => new
            {
                r.CrewTemplateRowId,
                r.Position,
                r.LaborType,
                r.CraftCode,
                r.Qty,
                r.Shift,
                r.SortOrder,
            }),
        });
    }

    // POST /api/v1/crew-templates
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CrewTemplateDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "Name is required." });

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var template = new CrewTemplate
        {
            CompanyCode = CompanyCode,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            CreatedBy = Username,
            Rows = dto.Rows.Select((r, i) => new CrewTemplateRow
            {
                Position = r.Position,
                LaborType = r.LaborType ?? "Direct",
                CraftCode = r.CraftCode,
                Qty = r.Qty > 0 ? r.Qty : 1,
                Shift = r.Shift ?? "Day",
                SortOrder = i,
            }).ToList(),
        };

        db.CrewTemplates.Add(template);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = template.CrewTemplateId }, new { template.CrewTemplateId });
    }

    // PUT /api/v1/crew-templates/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CrewTemplateDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "Name is required." });

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var template = await db.CrewTemplates
            .Include(t => t.Rows)
            .FirstOrDefaultAsync(t => t.CrewTemplateId == id && t.CompanyCode == CompanyCode, ct);

        if (template == null) return NotFound();

        template.Name = dto.Name.Trim();
        template.Description = dto.Description?.Trim();

        // Replace rows
        db.CrewTemplateRows.RemoveRange(template.Rows);
        template.Rows = dto.Rows.Select((r, i) => new CrewTemplateRow
        {
            Position = r.Position,
            LaborType = r.LaborType ?? "Direct",
            CraftCode = r.CraftCode,
            Qty = r.Qty > 0 ? r.Qty : 1,
            Shift = r.Shift ?? "Day",
            SortOrder = i,
        }).ToList();

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE /api/v1/crew-templates/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var template = await db.CrewTemplates
            .FirstOrDefaultAsync(t => t.CrewTemplateId == id && t.CompanyCode == CompanyCode, ct);

        if (template == null) return NotFound();

        db.CrewTemplates.Remove(template);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // POST /api/v1/crew-templates/{id}/apply
    [HttpPost("{id:int}/apply")]
    public async Task<IActionResult> Apply(int id, [FromBody] ApplyTemplateDto dto, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var template = await db.CrewTemplates
            .Include(t => t.Rows)
            .FirstOrDefaultAsync(t => t.CrewTemplateId == id && t.CompanyCode == CompanyCode, ct);

        if (template == null) return NotFound(new { message = "Template not found." });

        var estimate = await db.Estimates
            .FirstOrDefaultAsync(e => e.EstimateId == dto.EstimateId && e.CompanyCode == CompanyCode, ct);

        if (estimate == null) return NotFound(new { message = "Estimate not found." });

        // Load rate book rates for position lookups (rateBookId passed from frontend — not stored on Estimate)
        var rateMap = new Dictionary<string, RateBookLaborRate>(StringComparer.OrdinalIgnoreCase);
        if (dto.RateBookId.HasValue)
        {
            var rates = await db.RateBookLaborRates
                .Where(r => r.RateBookId == dto.RateBookId.Value)
                .ToListAsync(ct);
            foreach (var r in rates)
                rateMap[r.Position] = r;
        }

        // Determine next sort order
        var maxSort = await db.LaborRows
            .Where(r => r.EstimateId == estimate.EstimateId)
            .Select(r => (int?)r.SortOrder)
            .MaxAsync(ct) ?? -1;

        var addedCount = 0;
        foreach (var row in template.Rows.OrderBy(r => r.SortOrder))
        {
            rateMap.TryGetValue(row.Position, out var rate);

            db.LaborRows.Add(new LaborRow
            {
                EstimateId = estimate.EstimateId,
                Position = row.Position,
                LaborType = row.LaborType,
                CraftCode = row.CraftCode ?? rate?.CraftCode,
                NavCode = rate?.NavCode,
                Shift = row.Shift,
                BillStRate = rate?.StRate ?? 0,
                BillOtRate = rate?.OtRate ?? 0,
                BillDtRate = rate?.DtRate ?? 0,
                SortOrder = ++maxSort,
            });
            addedCount++;
        }

        await db.SaveChangesAsync(ct);

        return Ok(new { addedCount, message = $"Added {addedCount} labor row(s) from template '{template.Name}'." });
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record CrewTemplateDto(
    string Name,
    string? Description,
    List<CrewTemplateRowDto> Rows);

public record CrewTemplateRowDto(
    string Position,
    string? LaborType,
    string? CraftCode,
    int Qty,
    string? Shift);

public record ApplyTemplateDto(int EstimateId, int? RateBookId);
