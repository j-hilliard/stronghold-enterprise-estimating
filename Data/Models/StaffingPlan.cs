namespace Stronghold.EnterpriseEstimating.Data.Models;

public class StaffingPlan
{
    public int StaffingPlanId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string StaffingPlanNumber { get; set; } = string.Empty;
    public int? ConvertedEstimateId { get; set; }
    public Estimate? ConvertedEstimate { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;
    public string? ClientCode { get; set; }
    public string? Branch { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? JobLetter { get; set; }

    public string Status { get; set; } = "Draft"; // Draft, Active, Approved, Converted, Archived
    public string Shift { get; set; } = "Day";
    public decimal HoursPerShift { get; set; } = 10;
    public int Days { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string OtMethod { get; set; } = "daily8_weekly40";
    public string DtWeekends { get; set; } = "none"; // none, sun_only, sat_sun

    public decimal RoughLaborTotal { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public string? UpdatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<StaffingLaborRow> LaborRows { get; set; } = new List<StaffingLaborRow>();
    public ICollection<Estimate> Estimates { get; set; } = new List<Estimate>();
}
