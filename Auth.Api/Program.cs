using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Auth.Api.Data;
using Auth.Api.Middleware;
using Platform.Shared;
using Auth.Api.Services;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

// In Development, auto-start Azurite Docker container for local blob storage
if (builder.Environment.IsDevelopment())
{
    try
    {
        var composePath = Path.Combine(builder.Environment.ContentRootPath, "..", "docker-compose.yml");
        if (File.Exists(composePath))
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"compose -f \"{composePath}\" up -d",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = System.Diagnostics.Process.Start(psi);
            process?.WaitForExit(10_000);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Azurite] 無法自動啟動 Docker 容器: {ex.Message}");
    }
}

// Load shared configuration (JWT, connection strings, etc.) from solution root
builder.Configuration.AddJsonFile(
    Path.Combine(builder.Environment.ContentRootPath, "..", "appsettings.Shared.json"),
    optional: true, reloadOnChange: true);

// Azure Key Vault (if VaultUri configured, overrides all other config sources)
var vaultUri = builder.Configuration["AzureKeyVault:VaultUri"];
if (!string.IsNullOrWhiteSpace(vaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(vaultUri),
        new Azure.Identity.DefaultAzureCredential());
}

// SQL Server / Azure SQL
builder.Services.AddDbContext<AuthDbContext>(options =>
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

// Azure Blob Storage (uses Microsoft.Extensions.Azure for proper DI lifecycle)
var blobConnStr = builder.Configuration["AzureBlobStorage:ConnectionString"] ?? "UseDevelopmentStorage=true";
builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(blobConnStr);
});
builder.Services.AddSingleton<AvatarStorageService>();

builder.Services.AddControllers();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi();
}

var app = builder.Build();

// In Development, auto-apply migrations and seed data for convenience.
// In Production, migrations should be applied via CI/CD pipeline or deploy scripts
// (e.g. `dotnet ef database update`), not automatically on startup.
if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        context.Database.Migrate();
        SeedData.Initialize(context);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "無法自動執行資料庫遷移，請確認資料庫連線設定。應用程式仍會啟動，但資料庫相關功能可能無法使用。");
    }
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<MustChangePasswordMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();
app.Run();

