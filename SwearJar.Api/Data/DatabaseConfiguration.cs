using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace SwearJar.Api.Data;

public static class DatabaseConfiguration
{
    public static string GetDefaultConnectionString(IConfiguration configuration)
    {
        return configuration.GetConnectionString("DefaultConnection")
            ?? configuration["SQLAZURECONNSTR_DefaultConnection"]
            ?? configuration["CUSTOMCONNSTR_DefaultConnection"]
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
    }

    public static void ConfigureSqlServer(DbContextOptionsBuilder optionsBuilder, IConfiguration configuration)
    {
        var connectionString = GetDefaultConnectionString(configuration);
        optionsBuilder.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure());
    }
}
