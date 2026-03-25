namespace Stronghold.EnterpriseEstimating.Data.Models;

public class EquipmentRow
{
    public int EquipmentRowId { get; set; }
    public int EstimateId { get; set; }
    public Estimate Estimate { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string RateType { get; set; } = "Daily"; // Hourly, Daily, Weekly, Monthly
    public decimal Rate { get; set; }
    public int Qty { get; set; } = 1;
    public int Days { get; set; }
    public int SortOrder { get; set; }
    public decimal Subtotal { get; set; }
}
