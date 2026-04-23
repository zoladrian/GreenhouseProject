using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Greenhouse.Infrastructure.Persistence;

public sealed class GreenhouseDbContextFactory : IDesignTimeDbContextFactory<GreenhouseDbContext>
{
    public GreenhouseDbContext CreateDbContext(string[] args)
    {
        var dbPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "design-time-greenhouse.db"));
        var dbDirectory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrWhiteSpace(dbDirectory))
        {
            Directory.CreateDirectory(dbDirectory);
        }

        var optionsBuilder = new DbContextOptionsBuilder<GreenhouseDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        return new GreenhouseDbContext(optionsBuilder.Options);
    }
}
