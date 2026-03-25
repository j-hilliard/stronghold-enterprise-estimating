namespace Stronghold.EnterpriseEstimating.Data.Models;

public class FcoEntry
{
    public int FcoEntryId { get; set; }
    public int EstimateId { get; set; }
    public Estimate Estimate { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public decimal DollarAdjustment { get; set; }
    public string? Reason { get; set; }

    public string Author { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
