using Greenhouse.Application.Abstractions;
using Greenhouse.Domain.Nawy;
using Microsoft.EntityFrameworkCore;

namespace Greenhouse.Infrastructure.Persistence;

public sealed class EfNawaRepository : INawaRepository
{
    private readonly GreenhouseDbContext _dbContext;

    public EfNawaRepository(GreenhouseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Nawa?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Nawy.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Nawa>> ListAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Nawy.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Nawa nawa, CancellationToken cancellationToken)
    {
        await _dbContext.Nawy.AddAsync(nawa, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
