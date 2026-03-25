namespace Stronghold.EnterpriseEstimating.Data.Models;

public class LaborRow
{
    public int LaborRowId { get; set; }
    public int EstimateId { get; set; }
    public Estimate Estimate { get; set; } = null!;

    public string Position { get; set; } = string.Empty;
    public string LaborType { get; set; } = "Direct"; // Direct, Indirect
    public string Shift { get; set; } = "Day";
    public string? CraftCode { get; set; }
    public string? NavCode { get; set; }

    public decimal BillStRate { get; set; }
    public decimal BillOtRate { get; set; }
    public decimal BillDtRate { get; set; }

    public int SortOrder { get; set; }

    // Per-day schedule stored as JSON: { "2026-06-01": 10, "2026-06-02": 10, ... }
    public string? ScheduleJson { get; set; }

    // Computed totals (persisted for reporting)
    public decimal StHours { get; set; }
    public decimal OtHours { get; set; }
    public decimal DtHours { get; set; }
    public decimal Subtotal { get; set; }
}
