using System.Text.Json.Serialization;

namespace Stronghold.EnterpriseEstimating.Data.Models;

// Category: "PerDiem" | "Travel" | "Lodging"
public class RateBookExpenseItem
{
    public int RateBookExpenseItemId { get; set; }
    public int RateBookId { get; set; }
    [JsonIgnore] public RateBook RateBook { get; set; } = null!;

    public string Category { get; set; } = "PerDiem";
    public string Description { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public string Unit { get; set; } = string.Empty; // "day", "mile", "trip", "night"
    public int SortOrder { get; set; }
}
