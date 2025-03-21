using Microsoft.EntityFrameworkCore;
using Thunders.TechTest.ApiService.Models;

namespace Thunders.TechTest.ApiService.Data;

public class TollUsageDbContext : DbContext
{
    public TollUsageDbContext(DbContextOptions<TollUsageDbContext> options) : base(options)
    {
    }

    public DbSet<TollUsage> TollUsages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TollUsage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TollBooth).IsRequired();
            entity.Property(e => e.City).IsRequired();
            entity.Property(e => e.State).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<TollUsage>()
            .HasIndex(x => new { x.UsageDateTime, x.City });

        modelBuilder.Entity<TollUsage>()
            .HasIndex(x => new { x.UsageDateTime, x.TollBooth });

        modelBuilder.Entity<TollUsage>()
            .HasIndex(x => new { x.TollBooth, x.VehicleType });
    }
} 