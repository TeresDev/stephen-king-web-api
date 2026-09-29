using Investec.SK.Domain;

namespace Investec.SK.Application.Abstractions;

public interface IVillainRepository
{
    Task<UpstreamResult<SourceData<Villain>>> GetAllAsync(CancellationToken cancellationToken);
}
