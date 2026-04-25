using System.Text.Json.Serialization;

namespace Stronghold.EnterpriseEstimating.Data.Models;

public class CostBookExpense
{
    public int CostBookExpenseId { get; set; }
    public int CostBookId { get; set; }
    [JsonIgnore] public CostBook CostBook { get; set; } = null!;

    public string Category { get; set; } = string.Empty; // PerDiem, Travel, Lodging
    public string Description { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public string Unit { get; set; } = "Day";
    public int SortOrder { get; set; }
}
