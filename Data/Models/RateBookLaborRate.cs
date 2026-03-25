namespace Stronghold.EnterpriseEstimating.Data.Models;

public class RateBookLaborRate
{
    public int RateBookLaborRateId { get; set; }
    public int RateBookId { get; set; }
    public RateBook RateBook { get; set; } = null!;

    public string Position { get; set; } = string.Empty;
    public string LaborType { get; set; } = "Direct"; // Direct, Indirect
    public string? CraftCode { get; set; }
    public string? NavCode { get; set; }

    public decimal StRate { get; set; }
    public decimal OtRate { get; set; }
    public decimal DtRate { get; set; }

    public int SortOrder { get; set; }
}
