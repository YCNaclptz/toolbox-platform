using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SwearJar.Api.Data;

/// <summary>
/// Design-time factory used by EF Core CLI tools (e.g. <c>dotnet ef migrations add</c>,
/// <c>dotnet ef database update</c>). Required because the CLI cannot resolve the
/// DbContext from the application host when connection strings are stored in User Secrets
/// or environment variables that aren't available at design time.
/// </summary>
public class SwearJarDbContextFactory : IDesignTimeDbContextFactory<SwearJarDbContext>
{
    public SwearJarDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";

        var basePath = Directory.GetCurrentDirectory();

        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile(Path.Combine(basePath, "..", "appsettings.Shared.json"), optional: true)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables();

        // Load user secrets in development so that dotnet ef commands can find the connection string
        if (environment == "Development")
        {
            configBuilder.AddUserSecrets<SwearJarDbContextFactory>(optional: true);
        }

        var configuration = configBuilder.Build();

        var optionsBuilder = new DbContextOptionsBuilder<SwearJarDbContext>();
        DatabaseConfiguration.ConfigureSqlServer(optionsBuilder, configuration);

        return new SwearJarDbContext(optionsBuilder.Options);
    }
}
