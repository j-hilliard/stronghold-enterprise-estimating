using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/cost-books")]
[Authorize]
public class CostBooksController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public CostBooksController(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    private string CompanyCode => User.FindFirst("company_code")?.Value ?? string.Empty;
    private string Username => User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                            ?? User.FindFirst("username")?.Value ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var books = await db.CostBooks
            .Where(cb => cb.CompanyCode == CompanyCode)
            .OrderByDescending(cb => cb.IsDefault)
            .ThenBy(cb => cb.Name)
            .Select(cb => new { cb.CostBookId, cb.Name, cb.IsDefault, cb.UpdatedAt })
            .ToListAsync(ct);
        return Ok(books);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var book = await db.CostBooks
            .Include(cb => cb.LaborRates.OrderBy(r => r.SortOrder))
            .Include(cb => cb.EquipmentRates.OrderBy(r => r.SortOrder))
            .Include(cb => cb.Expenses.OrderBy(r => r.SortOrder))
            .Include(cb => cb.OverheadItems.OrderBy(r => r.SortOrder))
            .FirstOrDefaultAsync(cb => cb.CostBookId == id && cb.CompanyCode == CompanyCode, ct);

        if (book == null) return NotFound();
        return Ok(book);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CostBookUpsertDto dto, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var book = await db.CostBooks
            .Include(cb => cb.LaborRates)
            .Include(cb => cb.EquipmentRates)
            .Include(cb => cb.Expenses)
            .Include(cb => cb.OverheadItems)
            .FirstOrDefaultAsync(cb => cb.CostBookId == id && cb.CompanyCode == CompanyCode, ct);

        if (book == null) return NotFound();

        book.Name = dto.Name;
        book.IsDefault = dto.IsDefault;
        book.UpdatedBy = Username;
        book.UpdatedAt = DateTimeOffset.UtcNow;

        if (dto.LaborRates != null)
        {
            db.CostBookLaborRates.RemoveRange(book.LaborRates);
            for (int i = 0; i < dto.LaborRates.Count; i++)
            {
                var r = dto.LaborRates[i];
                book.LaborRates.Add(new CostBookLaborRate
                {
                    Position = r.Position, LaborType = r.LaborType,
                    CraftCode = r.CraftCode, NavCode = r.NavCode,
                    StRate = r.StRate, OtRate = r.OtRate, DtRate = r.DtRate,
                    SortOrder = i
                });
            }
        }

        if (dto.EquipmentRates != null)
        {
            db.CostBookEquipmentRates.RemoveRange(book.EquipmentRates);
            for (int i = 0; i < dto.EquipmentRates.Count; i++)
            {
                var r = dto.EquipmentRates[i];
                book.EquipmentRates.Add(new CostBookEquipmentRate
                {
                    Name = r.Name,
                    Hourly = r.Hourly, Daily = r.Daily,
                    Weekly = r.Weekly, Monthly = r.Monthly,
                    SortOrder = i
                });
            }
        }

        if (dto.Expenses != null)
        {
            db.CostBookExpenses.RemoveRange(book.Expenses);
            for (int i = 0; i < dto.Expenses.Count; i++)
            {
                var r = dto.Expenses[i];
                book.Expenses.Add(new CostBookExpense
                {
                    Category = r.Category, Description = r.Description,
                    Rate = r.Rate, Unit = r.Unit,
                    SortOrder = i
                });
            }
        }

        if (dto.OverheadItems != null)
        {
            db.CostBookOverheadItems.RemoveRange(book.OverheadItems);
            for (int i = 0; i < dto.OverheadItems.Count; i++)
            {
                var r = dto.OverheadItems[i];
                book.OverheadItems.Add(new CostBookOverheadItem
                {
                    Category = r.Category, Code = r.Code, Name = r.Name,
                    BurdenType = r.BurdenType, Value = r.Value,
                    SortOrder = i
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // Seed/reset the standard cost book for this company
    [HttpPost("reset-standard")]
    public async Task<IActionResult> ResetStandard(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var existing = await db.CostBooks
            .Include(cb => cb.LaborRates)
            .Include(cb => cb.EquipmentRates)
            .Include(cb => cb.Expenses)
            .Include(cb => cb.OverheadItems)
            .FirstOrDefaultAsync(cb => cb.CompanyCode == CompanyCode && cb.IsDefault, ct);

        if (existing == null)
        {
            existing = new CostBook
            {
                CompanyCode = CompanyCode,
                Name = "Standard Cost Book",
                IsDefault = true,
                UpdatedBy = Username,
            };
            db.CostBooks.Add(existing);
        }
        else
        {
            db.CostBookLaborRates.RemoveRange(existing.LaborRates);
            db.CostBookEquipmentRates.RemoveRange(existing.EquipmentRates);
            db.CostBookExpenses.RemoveRange(existing.Expenses);
            db.CostBookOverheadItems.RemoveRange(existing.OverheadItems);
            existing.UpdatedBy = Username;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        // Standard labor burden items (from legacy app)
        var burdenItems = new[]
        {
            new { Category = "Burden", Code = "FICA",      Name = "FICA",                  BurdenType = "dollar_per_hour", Value = 7.00m },
            new { Category = "Burden", Code = "FUTA",      Name = "FUTA",                  BurdenType = "percentage",      Value = 0.60m },
            new { Category = "Burden", Code = "SUTA",      Name = "SUTA",                  BurdenType = "percentage",      Value = 2.70m },
            new { Category = "Burden", Code = "WC",        Name = "Workers Comp",          BurdenType = "percentage",      Value = 8.50m },
            new { Category = "Burden", Code = "GL",        Name = "General Liability",     BurdenType = "percentage",      Value = 2.50m },
            new { Category = "Burden", Code = "AUTO",      Name = "Auto",                  BurdenType = "percentage",      Value = 1.00m },
            new { Category = "Burden", Code = "UMBRELLA",  Name = "Umbrella",              BurdenType = "percentage",      Value = 0.75m },
            new { Category = "Burden", Code = "BOND",      Name = "Bonding",               BurdenType = "percentage",      Value = 1.50m },
            new { Category = "Burden", Code = "HEALTH",    Name = "Health",                BurdenType = "percentage",      Value = 6.00m },
            new { Category = "Burden", Code = "401K",      Name = "401(k)",                BurdenType = "percentage",      Value = 3.00m },
            new { Category = "Burden", Code = "TRAINING",  Name = "Training",              BurdenType = "percentage",      Value = 1.00m },
            new { Category = "Burden", Code = "GA",        Name = "G&A",                   BurdenType = "percentage",      Value = 5.00m },
        };

        for (int i = 0; i < burdenItems.Length; i++)
        {
            var b = burdenItems[i];
            existing.OverheadItems.Add(new CostBookOverheadItem
            {
                Category = b.Category, Code = b.Code, Name = b.Name,
                BurdenType = b.BurdenType, Value = b.Value,
                SortOrder = i
            });
        }

        // Standard expenses
        var expenses = new[]
        {
            new { Category = "PerDiem",   Description = "Per Diem",    Rate = 75.00m,  Unit = "Day" },
            new { Category = "Travel",    Description = "Travel",       Rate = 0.67m,   Unit = "Mile" },
            new { Category = "Lodging",   Description = "Lodging",      Rate = 150.00m, Unit = "Day" },
        };

        for (int i = 0; i < expenses.Length; i++)
        {
            var e = expenses[i];
            existing.Expenses.Add(new CostBookExpense
            {
                Category = e.Category, Description = e.Description,
                Rate = e.Rate, Unit = e.Unit,
                SortOrder = i
            });
        }

        await db.SaveChangesAsync(ct);
        return Ok(new { costBookId = existing.CostBookId, name = existing.Name });
    }
}

public record CostBookUpsertDto(
    string Name,
    bool IsDefault,
    List<CostBookLaborRateDto>? LaborRates,
    List<CostBookEquipmentRateDto>? EquipmentRates,
    List<CostBookExpenseDto>? Expenses,
    List<CostBookOverheadItemDto>? OverheadItems);

public record CostBookLaborRateDto(
    string Position, string LaborType,
    string? CraftCode, string? NavCode,
    decimal StRate, decimal OtRate, decimal DtRate);

public record CostBookEquipmentRateDto(
    string Name,
    decimal? Hourly, decimal? Daily, decimal? Weekly, decimal? Monthly);

public record CostBookExpenseDto(
    string Category, string Description, decimal Rate, string Unit);

public record CostBookOverheadItemDto(
    string Category, string Code, string Name,
    string BurdenType, decimal Value);
