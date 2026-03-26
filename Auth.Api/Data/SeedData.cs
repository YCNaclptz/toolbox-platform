using Auth.Api.Models;

namespace Auth.Api.Data;

public static class SeedData
{
    public static void Initialize(AuthDbContext context)
    {
        // Seed admin user if no users exist
        if (!context.Users.Any())
        {
            var admin = new User
            {
                Username = "admin",
                DisplayName = "管理員",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = "Admin",
                MustChangePassword = true,
                IsActive = true
            };
            context.Users.Add(admin);
            context.SaveChanges();
        }

        // Seed swear-jar application if no apps exist
        if (!context.Applications.Any())
        {
            var swearJarApp = new Application
            {
                Id = "swear-jar",
                Name = "髒話罐",
                Description = "記錄和追蹤髒話罰款",
                Icon = "icon-money-bag",
                RoutePrefix = "/swear-jar",
                ApiPrefix = "/api/entries",
                IsActive = true,
                SortOrder = 1
            };
            context.Applications.Add(swearJarApp);
            context.SaveChanges();
        }

        // Grant all active users access to all active apps (development convenience)
        var activeUsers = context.Users.Where(u => u.IsActive).ToList();
        var activeApps = context.Applications.Where(a => a.IsActive).ToList();
        
        foreach (var user in activeUsers)
        {
            foreach (var app in activeApps)
            {
                if (!context.UserApplicationAccesses.Any(ua => ua.UserId == user.Id && ua.ApplicationId == app.Id))
                {
                    context.UserApplicationAccesses.Add(new UserApplicationAccess
                    {
                        UserId = user.Id,
                        ApplicationId = app.Id,
                        GrantedAt = DateTime.UtcNow
                    });
                }
            }
        }
        
        context.SaveChanges();
    }
}
