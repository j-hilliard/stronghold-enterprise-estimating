namespace Stronghold.EnterpriseEstimating.Data.Models;

public class Estimate
{
    public int EstimateId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string EstimateNumber { get; set; } = string.Empty;
    public int? StaffingPlanId { get; set; }
    public StaffingPlan? StaffingPlan { get; set; }

    // Header fields
    public string Name { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;
    public string? ClientCode { get; set; }
    public string? MsaNumber { get; set; }
    public string? JobType { get; set; }
    public string? Branch { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Site { get; set; }
    public string? JobLetter { get; set; }
    public string? VP { get; set; }
    public string? Director { get; set; }
    public string? Region { get; set; }

    // Schedule
    public string Shift { get; set; } = "Day"; // Day, Night, Both
    public decimal HoursPerShift { get; set; } = 10;
    public int Days { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // OT/DT rules
    public string OtMethod { get; set; } = "daily8_weekly40"; // daily8_weekly40, daily8, weekly40, california
    public bool DtWeekends { get; set; } = false;

    // Status & meta
    public string Status { get; set; } = "Draft"; // Draft, Pending, Awarded, Lost, Canceled
    public decimal ConfidencePct { get; set; } = 50;
    public string? LostReason { get; set; }
    public string? LostNotes { get; set; }
    public bool IsScenario { get; set; } = false;

    public string CreatedBy { get; set; } = string.Empty;
    public string? UpdatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public int? RateBookId { get; set; }
    public RateBook? RateBook { get; set; }

    public ICollection<LaborRow> LaborRows { get; set; } = new List<LaborRow>();
    public ICollection<EquipmentRow> EquipmentRows { get; set; } = new List<EquipmentRow>();
    public ICollection<ExpenseRow> ExpenseRows { get; set; } = new List<ExpenseRow>();
    public ICollection<FcoEntry> FcoEntries { get; set; } = new List<FcoEntry>();
    public ICollection<EstimateRevision> Revisions { get; set; } = new List<EstimateRevision>();
    public EstimateSummary? Summary { get; set; }
}
