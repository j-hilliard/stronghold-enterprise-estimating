namespace Stronghold.EnterpriseEstimating.Data.Models;

public class StaffingLaborRow
{
    public int StaffingLaborRowId { get; set; }
    public int StaffingPlanId { get; set; }
    public StaffingPlan StaffingPlan { get; set; } = null!;

    public string Position { get; set; } = string.Empty;
    public string LaborType { get; set; } = "Direct"; // Direct, Indirect
    public string Shift { get; set; } = "Day";
    public string? CraftCode { get; set; }
    public string? NavCode { get; set; }

    public decimal StRate { get; set; }
    public decimal OtRate { get; set; }
    public decimal DtRate { get; set; }

    public int SortOrder { get; set; }
    public string? ScheduleJson { get; set; }

    public decimal StHours { get; set; }
    public decimal OtHours { get; set; }
    public decimal DtHours { get; set; }
    public decimal Subtotal { get; set; }
}
