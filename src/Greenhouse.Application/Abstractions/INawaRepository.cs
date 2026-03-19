using Greenhouse.Domain.Nawy;

namespace Greenhouse.Application.Abstractions;

public interface INawaRepository
{
    Task<Nawa?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Nawa>> ListAsync(CancellationToken cancellationToken);

    Task AddAsync(Nawa nawa, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
