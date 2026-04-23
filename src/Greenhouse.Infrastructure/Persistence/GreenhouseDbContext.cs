using Greenhouse.Domain.Nawy;
using Greenhouse.Domain.SensorReadings;
using Greenhouse.Domain.Sensors;
using Microsoft.EntityFrameworkCore;

namespace Greenhouse.Infrastructure.Persistence;

public sealed class GreenhouseDbContext : DbContext
{
    public GreenhouseDbContext(DbContextOptions<GreenhouseDbContext> options)
        : base(options)
    {
    }

    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();

    public DbSet<Nawa> Nawy => Set<Nawa>();

    public DbSet<Sensor> Sensors => Set<Sensor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Nawa>(builder =>
        {
            builder.ToTable("Nawy");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500);
            builder.Property(x => x.PlantNote).HasMaxLength(200);
            builder.Property(x => x.IsActive).IsRequired();
            builder.Property(x => x.MoistureMin);
            builder.Property(x => x.MoistureMax);
            builder.Property(x => x.TemperatureMin);
            builder.Property(x => x.TemperatureMax);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<Sensor>(builder =>
        {
            builder.ToTable("Sensors");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ExternalId).HasMaxLength(120).IsRequired();
            builder.HasIndex(x => x.ExternalId).IsUnique();
            builder.Property(x => x.DisplayName).HasMaxLength(120);
            builder.Property(x => x.Kind).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.HasOne<Nawa>()
                .WithMany()
                .HasForeignKey(x => x.NawaId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SensorReading>(builder =>
        {
            builder.ToTable("SensorReadings");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.SensorIdentifier).HasMaxLength(120).IsRequired();
            builder.Property(x => x.Topic).HasMaxLength(255).IsRequired();
            builder.Property(x => x.RawPayloadJson).IsRequired();
            builder.Property(x => x.ReceivedAtUtc).IsRequired();
            builder.Property(x => x.Rain);
            builder.Property(x => x.RainIntensityRaw);
            builder.Property(x => x.IlluminanceRaw);
            builder.Property(x => x.IlluminanceAverage20MinRaw);
            builder.Property(x => x.IlluminanceMaximumTodayRaw);
            builder.Property(x => x.CleaningReminder);
            builder.HasIndex(x => x.ReceivedAtUtc);
            builder.HasIndex(x => x.SensorIdentifier);
            builder.HasIndex(x => new { x.SensorId, x.ReceivedAtUtc });
            builder.HasOne<Sensor>()
                .WithMany()
                .HasForeignKey(x => x.SensorId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
