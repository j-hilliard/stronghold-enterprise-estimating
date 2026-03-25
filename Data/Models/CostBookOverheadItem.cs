namespace Stronghold.EnterpriseEstimating.Data.Models;

public class CostBookOverheadItem
{
    public int CostBookOverheadItemId { get; set; }
    public int CostBookId { get; set; }
    public CostBook CostBook { get; set; } = null!;

    public string Category { get; set; } = string.Empty; // Burden, Insurance, Other
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    // BurdenType: "percentage" = applied as % of base wages; "dollar_per_hour" = fixed $/hr added
    public string BurdenType { get; set; } = "percentage";
    public decimal Value { get; set; }

    public int SortOrder { get; set; }
}
