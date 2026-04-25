namespace Stronghold.EnterpriseEstimating.Data.Models;

public class RateBook
{
    public int RateBookId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Client { get; set; }
    public string? ClientCode { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public bool IsStandardBaseline { get; set; } = false;
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpiresDate { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public string? UpdatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<RateBookLaborRate> LaborRates { get; set; } = new List<RateBookLaborRate>();
    public ICollection<RateBookEquipmentRate> EquipmentRates { get; set; } = new List<RateBookEquipmentRate>();
    public ICollection<RateBookExpenseItem> ExpenseItems { get; set; } = new List<RateBookExpenseItem>();
}
