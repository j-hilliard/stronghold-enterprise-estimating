using System.Text.Json.Serialization;

namespace Stronghold.EnterpriseEstimating.Data.Models;

public class CostBookLaborRate
{
    public int CostBookLaborRateId { get; set; }
    public int CostBookId { get; set; }
    [JsonIgnore] public CostBook CostBook { get; set; } = null!;

    public string Position { get; set; } = string.Empty;
    public string LaborType { get; set; } = "Direct"; // Direct, Indirect
    public string? CraftCode { get; set; }
    public string? NavCode { get; set; }

    public decimal StRate { get; set; }
    public decimal OtRate { get; set; }
    public decimal DtRate { get; set; }

    public int SortOrder { get; set; }
}
