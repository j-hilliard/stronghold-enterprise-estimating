namespace Stronghold.EnterpriseEstimating.Data.Models;

public class CostBook
{
    public int CostBookId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;

    public string? UpdatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<CostBookLaborRate> LaborRates { get; set; } = new List<CostBookLaborRate>();
    public ICollection<CostBookEquipmentRate> EquipmentRates { get; set; } = new List<CostBookEquipmentRate>();
    public ICollection<CostBookExpense> Expenses { get; set; } = new List<CostBookExpense>();
    public ICollection<CostBookOverheadItem> OverheadItems { get; set; } = new List<CostBookOverheadItem>();
}
