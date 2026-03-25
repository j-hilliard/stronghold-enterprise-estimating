namespace Stronghold.EnterpriseEstimating.Data.Models;

public class Permission
{
    public int PermissionId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ModifiedOn { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<RolePermission>? RolePermissions { get; set; } = new List<RolePermission>();
}
