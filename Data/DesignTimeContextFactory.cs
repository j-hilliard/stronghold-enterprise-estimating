using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Stronghold.EnterpriseEstimating.Data;

public class DesignTimeContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Load connection string from Api/appsettings.Local.json (local dev)
        // falling back to Api/appsettings.json, then environment variable.
        var apiDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "Api");

        var config = new ConfigurationBuilder()
            .SetBasePath(apiDir)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("SqlDb")
            ?? "Server=.\\SQLEXPRESS;Database=StrongholdEstimating_Demo;Trusted_Connection=True;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
