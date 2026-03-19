using Greenhouse.Application.Abstractions;
using Greenhouse.Domain.Nawy;

namespace Greenhouse.Application.Nawy;

public sealed class CreateNawaCommandService
{
    private readonly INawaRepository _nawy;

    public CreateNawaCommandService(INawaRepository nawy)
    {
        _nawy = nawy;
    }

    public async Task<NawaDto> ExecuteAsync(string name, string? description, CancellationToken cancellationToken)
    {
        var nawa = Nawa.Create(name, description);
        await _nawy.AddAsync(nawa, cancellationToken);
        return NawaMapper.ToDto(nawa);
    }
}
