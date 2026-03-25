namespace Stronghold.EnterpriseEstimating.Data.Models;

public class Settings
{
    public int SettingsId { get; set; }
    public string AppName { get; set; } = "Stronghold Enterprise Estimating";
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ModifiedOn { get; set; } = DateTimeOffset.UtcNow;
}
