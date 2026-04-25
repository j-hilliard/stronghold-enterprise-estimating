namespace Stronghold.EnterpriseEstimating.Data.Models;

public class EstimateRevision
{
    public int EstimateRevisionId { get; set; }
    public int EstimateId { get; set; }
    public Estimate Estimate { get; set; } = null!;

    public int RevisionNumber { get; set; }
    public bool IsCurrent { get; set; } = false;
    public string? Description { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;

    public string SavedBy { get; set; } = string.Empty;
    public DateTimeOffset SavedAt { get; set; } = DateTimeOffset.UtcNow;

    // Quick-display totals (denormalized from snapshot)
    public int LaborCount { get; set; }
    public int EquipCount { get; set; }
    public decimal LaborTotal { get; set; }
    public decimal EquipTotal { get; set; }
    public decimal GrandTotal { get; set; }
}
