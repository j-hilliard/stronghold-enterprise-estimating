namespace Stronghold.EnterpriseEstimating.Data.Models;

public class UserProfileSettings
{
    public int ProfileId { get; set; }
    public int UserId { get; set; }
    public string? DefaultCompanyCode { get; set; }
    public string? DefaultCustomer { get; set; }
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ModifiedOn { get; set; } = DateTimeOffset.UtcNow;
    public User? User { get; set; }
}
