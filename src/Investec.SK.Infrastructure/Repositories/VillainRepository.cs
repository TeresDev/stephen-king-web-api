using Investec.SK.Application.Abstractions;
using Investec.SK.Domain;
using Investec.SK.Infrastructure.Contracts;
using Investec.SK.Infrastructure.Http;
using Investec.SK.Infrastructure.Mapping;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Investec.SK.Infrastructure.Repositories;

public sealed class VillainRepository(
    ISkApiClient client,
    IMemoryCache cache,
    IOptions<SkApiOptions> options) : IVillainRepository
{
    private const string CacheKey = "upstream:villains";

    public async Task<UpstreamResult<SourceData<Villain>>> GetAllAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKey, out SourceData<Villain>? cached) && cached is not null)
        {
            return UpstreamResult<SourceData<Villain>>.Success(cached);
        }

        var response = await client.GetListAsync<VillainContract>(
            UpstreamSources.Villains, "villains", cancellationToken);

        if (!response.IsSuccess)
        {
            return UpstreamResult<SourceData<Villain>>.Failed(response.Failure!);
        }

        var mapped = VillainMapper.MapAll(response.Value);
        var sourceData = new SourceData<Villain>(mapped.Items, mapped.Skipped);

        cache.Set(CacheKey, sourceData, options.Value.CacheTtl);

        return UpstreamResult<SourceData<Villain>>.Success(sourceData);
    }
}
