using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SwearJar.Api.Data;
using Platform.Shared;

var builder = WebApplication.CreateBuilder(args);

// Load shared configuration (JWT, connection strings, etc.) from solution root
builder.Configuration.AddJsonFile(
    Path.Combine(builder.Environment.ContentRootPath, "..", "appsettings.Shared.json"),
    optional: true, reloadOnChange: true);

// Azure Key Vaultloads secrets into the configuration pipeline.
// Set "AzureKeyVault:VaultUri" in appsettings.json (or an env var) to enable.
var vaultUri = builder.Configuration["AzureKeyVault:VaultUri"];
if (!string.IsNullOrWhiteSpace(vaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(vaultUri),
        new DefaultAzureCredential());
}

// SQL Server / Azure SQL
builder.Services.AddDbContext<SwearJarDbContext>(options =>
    DatabaseConfiguration.ConfigureSqlServer(options, builder.Configuration));

// JWT Authentication from Platform.Shared
builder.Services.AddPlatformJwtAuth(builder.Configuration);

// CORS
var allowedOrigins = builder.Configuration["AllowedOrigins"] ?? "*";
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins == "*")
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

builder.Services.AddControllers();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi();
}

var app = builder.Build();

// In Development, auto-apply migrations for convenience.
// In Production, migrations should be applied via CI/CD pipeline or deploy scripts
// (e.g. `dotnet ef database update`), not automatically on startup.
if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SwearJarDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "無法自動執行資料庫遷移，請確認資料庫連線設定。應用程式仍會啟動，但資料庫相關功能可能無法使用。");
    }
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();
app.Run();
