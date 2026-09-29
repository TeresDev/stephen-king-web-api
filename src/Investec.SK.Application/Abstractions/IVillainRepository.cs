using Investec.SK.Domain;

namespace Investec.SK.Application.Abstractions;

public interface IVillainRepository
{
    Task<UpstreamResult<IReadOnlyList<Villain>>> GetAllAsync(CancellationToken cancellationToken);
}
