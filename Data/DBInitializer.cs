using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext dbContext)
    {
        SeedRoles(dbContext);
        SeedCompanies(dbContext);
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
            dbContext.Roles.Add(new Role { Name = name, Description = description });

        dbContext.SaveChanges();
    }

    private static void SeedCompanies(AppDbContext dbContext)
    {
        if (dbContext.Companies.Any()) return;

        dbContext.Companies.AddRange(
            new Company
            {
                CompanyCode = "CSL",
                Name = "Cat-Spec, Ltd.",
                ShortName = "Cat-Spec",
                JobLetter = null,
                OpuNumber = "E464",
                Description = "Catalyst Handling & Specialty Industrial Services",
                Active = true
            },
            new Company
            {
                CompanyCode = "ETS",
                Name = "Elite TA Specialists, LLC",
                ShortName = "Elite TA",
                JobLetter = "H",
                OpuNumber = "E465",
                Description = "Turnaround & Maintenance Specialists – South Texas",
                Active = true
            },
            new Company
            {
                CompanyCode = "STS",
                Name = "Specialty Tank Services, Inc.",
                ShortName = "Specialty Tank",
                JobLetter = "S",
                OpuNumber = "E457",
                Description = "Aboveground Storage Tank Inspection & Repair",
                Active = true
            },
            new Company
            {
                CompanyCode = "STG",
                Name = "Stronghold Tower Group, LLC",
                ShortName = "Tower Group",
                JobLetter = "G",
                OpuNumber = "E466",
                Description = "Tower, Vessel & Reactor Internal Services",
                Active = true
            }
        );
        dbContext.SaveChanges();
    }

    private static void SeedDefaultUsers(AppDbContext dbContext)
    {
        if (dbContext.Users.Any()) return;

        var adminRole    = dbContext.Roles.First(r => r.Name == "Administrator");
        var estimatorRole = dbContext.Roles.First(r => r.Name == "Estimator");
        var analyticsRole = dbContext.Roles.First(r => r.Name == "Analytics");

        // dev.user — Administrator, access to CSL + ETS + GLOBAL
        var devUser = new User
        {
            Username     = "dev.user",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("StrongholdDev2024"),
            CompanyCode  = "CSL",
            FirstName    = "Dev",
            LastName     = "User",
            Email        = "dev.user@stronghold.local",
            Active       = true,
        };

        // estimator.csl — Estimator, CSL only
        var estimatorCsl = new User
        {
            Username     = "estimator.csl",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Stronghold2024"),
            CompanyCode  = "CSL",
            FirstName    = "James",
            LastName     = "Tanner",
            Email        = "james.tanner@catspec.com",
            Active       = true,
        };

        // estimator.ets — Estimator, ETS only
        var estimatorEts = new User
        {
            Username     = "estimator.ets",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Stronghold2024"),
            CompanyCode  = "ETS",
            FirstName    = "Maria",
            LastName     = "Delgado",
            Email        = "maria.delgado@eliteta.com",
            Active       = true,
        };

        // executive — Analytics, GLOBAL only (cross-company)
        var executive = new User
        {
            Username     = "executive",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Stronghold2024"),
            CompanyCode  = "CSL",
            FirstName    = "Robert",
            LastName     = "Callahan",
            Email        = "r.callahan@thestrongholdcompanies.com",
            Active       = true,
        };

        dbContext.Users.AddRange(devUser, estimatorCsl, estimatorEts, executive);
        dbContext.SaveChanges();

        // Roles
        dbContext.UserRoles.AddRange(
            new UserRole { UserId = devUser.UserId,      RoleId = adminRole.RoleId },
            new UserRole { UserId = estimatorCsl.UserId, RoleId = estimatorRole.RoleId },
            new UserRole { UserId = estimatorEts.UserId, RoleId = estimatorRole.RoleId },
            new UserRole { UserId = executive.UserId,    RoleId = analyticsRole.RoleId }
        );
        dbContext.SaveChanges();

        // Company access
        dbContext.UserCompanies.AddRange(
            // dev.user → CSL + ETS (GLOBAL is auto-granted via Administrator role)
            new UserCompany { UserId = devUser.UserId,      CompanyCode = "CSL" },
            new UserCompany { UserId = devUser.UserId,      CompanyCode = "ETS" },
            // estimator.csl → CSL only
            new UserCompany { UserId = estimatorCsl.UserId, CompanyCode = "CSL" },
            // estimator.ets → ETS only
            new UserCompany { UserId = estimatorEts.UserId, CompanyCode = "ETS" },
            // executive → no direct company (gets GLOBAL via Analytics role)
            new UserCompany { UserId = executive.UserId,    CompanyCode = "CSL" }
        );
        dbContext.SaveChanges();
    }
}
