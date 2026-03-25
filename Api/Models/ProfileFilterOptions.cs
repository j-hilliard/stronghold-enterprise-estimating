namespace Stronghold.EnterpriseEstimating.Api.Models;

public class ProfileFilterOptions
{
    public List<SafetyIncidentRegisterOption> Companies { get; set; } = new();
    public List<SafetyIncidentRegisterOption> Customers { get; set; } = new();
    public List<SafetyIncidentRegisterOption> Statuses { get; set; } = new();
}
