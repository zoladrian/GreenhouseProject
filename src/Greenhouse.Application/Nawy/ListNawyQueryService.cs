using Greenhouse.Application.Abstractions;

namespace Greenhouse.Application.Nawy;

public sealed class ListNawyQueryService
{
    private readonly INawaRepository _nawy;

    public ListNawyQueryService(INawaRepository nawy)
    {
        _nawy = nawy;
    }

    public async Task<IReadOnlyList<NawaDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var list = await _nawy.ListAsync(cancellationToken);
        return list
            .OrderBy(n => n.Name)
            .Select(NawaMapper.ToDto)
            .ToList();
    }
}
