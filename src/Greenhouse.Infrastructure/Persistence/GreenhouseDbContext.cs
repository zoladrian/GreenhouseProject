using Greenhouse.Domain.SensorReadings;
using Microsoft.EntityFrameworkCore;

namespace Greenhouse.Infrastructure.Persistence;

public sealed class GreenhouseDbContext : DbContext
{
    public GreenhouseDbContext(DbContextOptions<GreenhouseDbContext> options)
        : base(options)
    {
    }

    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SensorReading>(builder =>
        {
            builder.ToTable("SensorReadings");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.SensorIdentifier).HasMaxLength(120).IsRequired();
            builder.Property(x => x.Topic).HasMaxLength(255).IsRequired();
            builder.Property(x => x.RawPayloadJson).IsRequired();
            builder.Property(x => x.ReceivedAtUtc).IsRequired();
            builder.HasIndex(x => x.ReceivedAtUtc);
            builder.HasIndex(x => x.SensorIdentifier);
        });
    }
}
