using System.Text.Json.Serialization;

namespace Stronghold.EnterpriseEstimating.Data.Models;

public class CostBookEquipmentRate
{
    public int CostBookEquipmentRateId { get; set; }
    public int CostBookId { get; set; }
    [JsonIgnore] public CostBook CostBook { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public decimal? Hourly { get; set; }
    public decimal? Daily { get; set; }
    public decimal? Weekly { get; set; }
    public decimal? Monthly { get; set; }
    public int SortOrder { get; set; }
}
