namespace Stronghold.EnterpriseEstimating.Data.Models;

public class CrewTemplate
{
    public int CrewTemplateId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<CrewTemplateRow> Rows { get; set; } = new List<CrewTemplateRow>();
}
