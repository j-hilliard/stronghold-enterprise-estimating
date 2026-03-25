using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext dbContext)
    {
        SeedRoles(dbContext);
        SeedDefaultUsers(dbContext);
    }

    private static readonly List<(string Name, string Description)> DefaultRoles = new()
    {
        ("Administrator", "Full access to all features and settings."),
        ("Estimator", "Can create, edit, and submit estimates and staffing plans."),
        ("Viewer", "Read-only access to estimates, reports, and analytics."),
        ("Analytics", "Access to analytics and global financial dashboards."),
    };

    private static void SeedRoles(AppDbContext dbContext)
    {
        if (dbContext.Roles.Any()) return;

        foreach (var (name, description) in DefaultRoles)
        {
            dbContext.Roles.Add(new Role { Name = name, Description = description });
        }
        dbContext.SaveChanges();
    }

    private static void SeedDefaultUsers(AppDbContext dbContext)
    {
        if (dbContext.Users.Any()) return;

        // Local dev user — password: DevPassword123!
        // Hash generated with BCrypt cost factor 11
        var devUser = new User
        {
            Username = "dev.user",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("DevPassword123!"),
            CompanyCode = "CSL",
            FirstName = "Dev",
            LastName = "User",
            Email = "dev.user@stronghold.local",
            Active = true,
        };

        dbContext.Users.Add(devUser);
        dbContext.SaveChanges();

        var adminRole = dbContext.Roles.First(r => r.Name == "Administrator");
        dbContext.UserRoles.Add(new UserRole { UserId = devUser.UserId, RoleId = adminRole.RoleId });
        dbContext.SaveChanges();
    }
}
