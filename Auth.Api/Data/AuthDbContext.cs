using Microsoft.EntityFrameworkCore;
using Auth.Api.Models;

namespace Auth.Api.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<UserApplicationAccess> UserApplicationAccesses => Set<UserApplicationAccess>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("auth");
        
        modelBuilder.Entity<User>(e => {
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Username).HasMaxLength(50);
            e.Property(u => u.Email).HasMaxLength(255);
            e.Property(u => u.DisplayName).HasMaxLength(100);
            e.Property(u => u.Role).HasMaxLength(20);
            e.Property(u => u.AvatarUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<Application>(e => {
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasMaxLength(50);
            e.Property(a => a.Name).HasMaxLength(100);
            e.Property(a => a.RoutePrefix).HasMaxLength(50);
            e.Property(a => a.ApiPrefix).HasMaxLength(50);
        });

        modelBuilder.Entity<UserApplicationAccess>(e => {
            e.HasKey(ua => new { ua.UserId, ua.ApplicationId });
            e.HasOne(ua => ua.User).WithMany().HasForeignKey(ua => ua.UserId);
            e.HasOne(ua => ua.Application).WithMany().HasForeignKey(ua => ua.ApplicationId);
        });
    }
}
