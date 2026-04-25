using System.Text.Json.Serialization;

namespace Stronghold.EnterpriseEstimating.Data.Models;

public class RateBookEquipmentRate
{
    public int RateBookEquipmentRateId { get; set; }
    public int RateBookId { get; set; }
    [JsonIgnore] public RateBook RateBook { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public decimal? Hourly { get; set; }
    public decimal? Daily { get; set; }
    public decimal? Weekly { get; set; }
    public decimal? Monthly { get; set; }
    public int SortOrder { get; set; }
}
