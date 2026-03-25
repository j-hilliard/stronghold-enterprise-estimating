namespace Stronghold.EnterpriseEstimating.Data.Models;

public class CrewTemplateRow
{
    public int CrewTemplateRowId { get; set; }
    public int CrewTemplateId { get; set; }
    public CrewTemplate CrewTemplate { get; set; } = null!;

    public string Position { get; set; } = string.Empty;
    public string LaborType { get; set; } = "Direct";
    public string? CraftCode { get; set; }
    public int Qty { get; set; } = 1;
    public string Shift { get; set; } = "Day";
    public int SortOrder { get; set; }
}
