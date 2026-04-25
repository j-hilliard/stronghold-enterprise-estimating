using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/rate-books")]
[Authorize]
public class RateBooksController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public RateBooksController(IDbContextFactory<AppDbContext> dbFactory)
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
        var books = await db.RateBooks
            .Where(rb => rb.CompanyCode == CompanyCode)
            .OrderBy(rb => rb.Name)
            .Select(rb => new
            {
                rb.RateBookId,
                rb.Name,
                rb.Client,
                rb.ClientCode,
                rb.City,
                rb.State,
                rb.IsStandardBaseline,
                rb.EffectiveDate,
                rb.ExpiresDate,
                rb.UpdatedAt,
                LaborCount = rb.LaborRates.Count,
                EquipmentCount = rb.EquipmentRates.Count,
                ExpenseCount = rb.ExpenseItems.Count,
            })
            .ToListAsync(ct);
        return Ok(books);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var book = await db.RateBooks
            .Include(rb => rb.LaborRates.OrderBy(r => r.SortOrder))
            .Include(rb => rb.EquipmentRates.OrderBy(r => r.SortOrder))
            .Include(rb => rb.ExpenseItems.OrderBy(r => r.SortOrder))
            .FirstOrDefaultAsync(rb => rb.RateBookId == id && rb.CompanyCode == CompanyCode, ct);
        if (book == null) return NotFound();
        return Ok(book);
    }

    // Find best matching rate book for a client/location
    [HttpGet("for-client")]
    public async Task<IActionResult> ForClient(
        [FromQuery] string client,
        [FromQuery] string? city,
        [FromQuery] string? state,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var books = await db.RateBooks
            .Where(rb => rb.CompanyCode == CompanyCode)
            .ToListAsync(ct);

        // Score: exact client+city+state = 3, client+state = 2, client only = 1
        var scored = books
            .Select(rb => new
            {
                rb.RateBookId,
                rb.Name,
                rb.Client,
                rb.ClientCode,
                rb.City,
                rb.State,
                Score = ScoreMatch(rb, client, city, state)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ToList();

        return Ok(scored);
    }

    private static int ScoreMatch(RateBook rb, string client, string? city, string? state)
    {
        if (string.IsNullOrEmpty(rb.Client)) return 0;

        bool clientMatch = rb.Client.Contains(client, StringComparison.OrdinalIgnoreCase)
                        || client.Contains(rb.Client, StringComparison.OrdinalIgnoreCase);
        if (!clientMatch) return 0;

        int score = 1;
        if (!string.IsNullOrEmpty(state) && string.Equals(rb.State, state, StringComparison.OrdinalIgnoreCase))
            score++;
        if (!string.IsNullOrEmpty(city) && string.Equals(rb.City, city, StringComparison.OrdinalIgnoreCase))
            score++;

        return score;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RateBookUpsertDto dto, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var book = new RateBook
        {
            CompanyCode = CompanyCode,
            Name = dto.Name,
            Client = dto.Client,
            ClientCode = dto.ClientCode,
            City = dto.City,
            State = dto.State,
            IsStandardBaseline = dto.IsStandardBaseline,
            EffectiveDate = dto.EffectiveDate,
            ExpiresDate = dto.ExpiresDate,
            CreatedBy = Username,
            UpdatedBy = Username,
        };

        if (dto.LaborRates != null)
        {
            for (int i = 0; i < dto.LaborRates.Count; i++)
            {
                var r = dto.LaborRates[i];
                book.LaborRates.Add(new RateBookLaborRate
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
            for (int i = 0; i < dto.EquipmentRates.Count; i++)
            {
                var r = dto.EquipmentRates[i];
                book.EquipmentRates.Add(new RateBookEquipmentRate
                {
                    Name = r.Name,
                    Hourly = r.Hourly, Daily = r.Daily,
                    Weekly = r.Weekly, Monthly = r.Monthly,
                    SortOrder = i
                });
            }
        }

        if (dto.ExpenseItems != null)
        {
            for (int i = 0; i < dto.ExpenseItems.Count; i++)
            {
                var r = dto.ExpenseItems[i];
                book.ExpenseItems.Add(new RateBookExpenseItem
                {
                    Category = r.Category,
                    Description = r.Description,
                    Rate = r.Rate,
                    Unit = r.Unit,
                    SortOrder = i
                });
            }
        }

        db.RateBooks.Add(book);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = book.RateBookId, version = "1.0" }, new { book.RateBookId });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] RateBookUpsertDto dto, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var book = await db.RateBooks
            .Include(rb => rb.LaborRates)
            .Include(rb => rb.EquipmentRates)
            .Include(rb => rb.ExpenseItems)
            .FirstOrDefaultAsync(rb => rb.RateBookId == id && rb.CompanyCode == CompanyCode, ct);

        if (book == null) return NotFound();

        book.Name = dto.Name;
        book.Client = dto.Client;
        book.ClientCode = dto.ClientCode;
        book.City = dto.City;
        book.State = dto.State;
        book.IsStandardBaseline = dto.IsStandardBaseline;
        book.EffectiveDate = dto.EffectiveDate;
        book.ExpiresDate = dto.ExpiresDate;
        book.UpdatedBy = Username;
        book.UpdatedAt = DateTimeOffset.UtcNow;

        if (dto.LaborRates != null)
        {
            db.RateBookLaborRates.RemoveRange(book.LaborRates);
            book.LaborRates.Clear();
            for (int i = 0; i < dto.LaborRates.Count; i++)
            {
                var r = dto.LaborRates[i];
                book.LaborRates.Add(new RateBookLaborRate
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
            db.RateBookEquipmentRates.RemoveRange(book.EquipmentRates);
            book.EquipmentRates.Clear();
            for (int i = 0; i < dto.EquipmentRates.Count; i++)
            {
                var r = dto.EquipmentRates[i];
                book.EquipmentRates.Add(new RateBookEquipmentRate
                {
                    Name = r.Name,
                    Hourly = r.Hourly, Daily = r.Daily,
                    Weekly = r.Weekly, Monthly = r.Monthly,
                    SortOrder = i
                });
            }
        }

        if (dto.ExpenseItems != null)
        {
            db.RateBookExpenseItems.RemoveRange(book.ExpenseItems);
            book.ExpenseItems.Clear();
            for (int i = 0; i < dto.ExpenseItems.Count; i++)
            {
                var r = dto.ExpenseItems[i];
                book.ExpenseItems.Add(new RateBookExpenseItem
                {
                    Category = r.Category,
                    Description = r.Description,
                    Rate = r.Rate,
                    Unit = r.Unit,
                    SortOrder = i
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var book = await db.RateBooks.FirstOrDefaultAsync(rb => rb.RateBookId == id && rb.CompanyCode == CompanyCode, ct);
        if (book == null) return NotFound();
        db.RateBooks.Remove(book);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:int}/clone")]
    public async Task<IActionResult> Clone(int id, [FromBody] CloneDto dto, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var source = await db.RateBooks
            .Include(rb => rb.LaborRates)
            .Include(rb => rb.EquipmentRates)
            .Include(rb => rb.ExpenseItems)
            .FirstOrDefaultAsync(rb => rb.RateBookId == id && rb.CompanyCode == CompanyCode, ct);

        if (source == null) return NotFound();

        var clone = new RateBook
        {
            CompanyCode = CompanyCode,
            Name = dto.Name,
            Client = dto.Client ?? source.Client,
            ClientCode = dto.ClientCode ?? source.ClientCode,
            City = dto.City ?? source.City,
            State = dto.State ?? source.State,
            IsStandardBaseline = false,
            EffectiveDate = source.EffectiveDate,
            ExpiresDate = source.ExpiresDate,
            CreatedBy = Username,
            UpdatedBy = Username,
        };

        foreach (var r in source.LaborRates)
        {
            clone.LaborRates.Add(new RateBookLaborRate
            {
                Position = r.Position, LaborType = r.LaborType,
                CraftCode = r.CraftCode, NavCode = r.NavCode,
                StRate = r.StRate, OtRate = r.OtRate, DtRate = r.DtRate,
                SortOrder = r.SortOrder
            });
        }

        foreach (var r in source.EquipmentRates)
        {
            clone.EquipmentRates.Add(new RateBookEquipmentRate
            {
                Name = r.Name,
                Hourly = r.Hourly, Daily = r.Daily,
                Weekly = r.Weekly, Monthly = r.Monthly,
                SortOrder = r.SortOrder
            });
        }

        foreach (var r in source.ExpenseItems)
        {
            clone.ExpenseItems.Add(new RateBookExpenseItem
            {
                Category = r.Category,
                Description = r.Description,
                Rate = r.Rate,
                Unit = r.Unit,
                SortOrder = r.SortOrder
            });
        }

        db.RateBooks.Add(clone);
        await db.SaveChangesAsync(ct);
        return Ok(new { rateBookId = clone.RateBookId, name = clone.Name });
    }
}

public record RateBookUpsertDto(
    string Name, string? Client, string? ClientCode,
    string? City, string? State, bool IsStandardBaseline,
    DateTime? EffectiveDate, DateTime? ExpiresDate,
    List<RateBookLaborRateDto>? LaborRates,
    List<RateBookEquipmentRateDto>? EquipmentRates,
    List<RateBookExpenseItemDto>? ExpenseItems);

public record RateBookLaborRateDto(
    string Position, string LaborType,
    string? CraftCode, string? NavCode,
    decimal StRate, decimal OtRate, decimal DtRate);

public record RateBookEquipmentRateDto(string Name, decimal? Hourly, decimal? Daily, decimal? Weekly, decimal? Monthly);

public record RateBookExpenseItemDto(string Category, string Description, decimal Rate, string Unit);

public record CloneDto(
    string Name, string? Client, string? ClientCode,
    string? City, string? State);
