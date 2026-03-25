namespace Stronghold.EnterpriseEstimating.Data.Models;

public class RolePermission
{
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;

    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
}
