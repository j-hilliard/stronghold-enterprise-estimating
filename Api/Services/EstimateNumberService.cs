using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Api.Services;

/// <summary>
/// Generates collision-safe estimate and staffing plan numbers.
/// Format:  {jobLetter?}-{YY}-{seq:D4}-{clientCode}   e.g. H-26-0001-BP
/// SP format: SP-{jobLetter?}-{YY}-{seq:D4}-{clientCode}  e.g. SP-H-26-0001-BP
/// </summary>
public class EstimateNumberService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public EstimateNumberService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<string> NextEstimateNumberAsync(
        string companyCode,
        string? jobLetter,
        string? clientCode,
        CancellationToken ct = default)
    {
        return await GenerateAsync(companyCode, "Estimate", jobLetter, clientCode, prefix: null, ct);
    }

    public async Task<string> NextStaffingPlanNumberAsync(
        string companyCode,
        string? jobLetter,
        string? clientCode,
        CancellationToken ct = default)
    {
        return await GenerateAsync(companyCode, "StaffingPlan", jobLetter, clientCode, prefix: "SP", ct);
    }

    private async Task<string> GenerateAsync(
        string companyCode,
        string sequenceType,
        string? jobLetter,
        string? clientCode,
        string? prefix,
        CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var yy = (year % 100).ToString("D2");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Use optimistic retry loop for collision safety
        for (int attempt = 0; attempt < 5; attempt++)
        {
            var seq = await db.EstimateSequences
                .FirstOrDefaultAsync(s => s.CompanyCode == companyCode
                    && s.Year == year
                    && s.SequenceType == sequenceType, ct);

            if (seq == null)
            {
                seq = new EstimateSequence
                {
                    CompanyCode = companyCode,
                    Year = year,
                    SequenceType = sequenceType,
                    LastSequence = 0
                };
                db.EstimateSequences.Add(seq);
            }

            seq.LastSequence++;
            var seqNum = seq.LastSequence.ToString("D4");

            try
            {
                await db.SaveChangesAsync(ct);

                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(prefix)) parts.Add(prefix);
                if (!string.IsNullOrWhiteSpace(jobLetter)) parts.Add(jobLetter.ToUpper());
                parts.Add(yy);
                parts.Add(seqNum);
                if (!string.IsNullOrWhiteSpace(clientCode)) parts.Add(clientCode.ToUpper());

                return string.Join("-", parts);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another request grabbed the same sequence — reload and retry
                db.ChangeTracker.Clear();
            }
        }

        throw new InvalidOperationException("Could not generate a unique estimate number after 5 attempts.");
    }
}
