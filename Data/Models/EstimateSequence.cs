namespace Stronghold.EnterpriseEstimating.Data.Models;

/// <summary>
/// Tracks the last-used sequence number per company per year for estimate/SP number generation.
/// Locked during number generation to prevent collisions.
/// </summary>
public class EstimateSequence
{
    public int EstimateSequenceId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public int Year { get; set; }
    public string SequenceType { get; set; } = "Estimate"; // Estimate, StaffingPlan
    public int LastSequence { get; set; } = 0;
}
