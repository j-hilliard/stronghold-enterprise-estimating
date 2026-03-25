namespace Stronghold.EnterpriseEstimating.Data.Models;

public class ExpenseRow
{
    public int ExpenseRowId { get; set; }
    public int EstimateId { get; set; }
    public Estimate Estimate { get; set; } = null!;

    public string Category { get; set; } = string.Empty; // PerDiem, Travel, Lodging, Other
    public string Description { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public string Unit { get; set; } = "Day"; // Day, Week, Month, Each
    public int DaysOrQty { get; set; } = 1;
    public int People { get; set; } = 1;
    public bool Billable { get; set; } = true;
    public int SortOrder { get; set; }
    public decimal Subtotal { get; set; }
}
