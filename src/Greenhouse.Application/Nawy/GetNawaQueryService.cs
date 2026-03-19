using Greenhouse.Application.Abstractions;

namespace Greenhouse.Application.Nawy;

public sealed class GetNawaQueryService
{
    private readonly INawaRepository _nawy;

    public GetNawaQueryService(INawaRepository nawy)
    {
        _nawy = nawy;
    }

    public async Task<NawaDto?> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var nawa = await _nawy.GetByIdAsync(id, cancellationToken);
        return nawa is null ? null : NawaMapper.ToDto(nawa);
    }
}
