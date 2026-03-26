using Microsoft.EntityFrameworkCore;
using SwearJar.Api.Models;

namespace SwearJar.Api.Data;

public class SwearJarDbContext : DbContext
{
    public SwearJarDbContext(DbContextOptions<SwearJarDbContext> options) : base(options) { }

    public DbSet<SwearEntry> SwearEntries => Set<SwearEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("swearjar");
        
        modelBuilder.Entity<SwearEntry>(e => {
            e.HasIndex(se => new { se.UserId, se.Time });
            e.HasQueryFilter(se => !se.IsDeleted);
        });
    }
}

