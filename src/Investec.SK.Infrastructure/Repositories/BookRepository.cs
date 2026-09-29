using Investec.SK.Application.Abstractions;
using Investec.SK.Domain;
using Investec.SK.Infrastructure.Contracts;
using Investec.SK.Infrastructure.Http;
using Investec.SK.Infrastructure.Mapping;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Investec.SK.Infrastructure.Repositories;

/// <summary>
/// Books from upstream, mapped to the domain and cached for a short TTL. Only successes are
/// cached, so an outage never poisons the cache and stale data is never served.
/// </summary>
public sealed class BookRepository(
    ISkApiClient client,
    IMemoryCache cache,
    IOptions<SkApiOptions> options) : IBookRepository
{
    private const string CacheKey = "upstream:books";

    public async Task<UpstreamResult<SourceData<Book>>> GetAllAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKey, out SourceData<Book>? cached) && cached is not null)
        {
            return UpstreamResult<SourceData<Book>>.Success(cached);
        }

        var response = await client.GetListAsync<BookContract>(
            UpstreamSources.Books, "books", cancellationToken);

        if (!response.IsSuccess)
        {
            return UpstreamResult<SourceData<Book>>.Failed(response.Failure!);
        }

        var mapped = BookMapper.MapAll(response.Value);
        var sourceData = new SourceData<Book>(mapped.Items, mapped.Skipped);

        cache.Set(CacheKey, sourceData, options.Value.CacheTtl);

        return UpstreamResult<SourceData<Book>>.Success(sourceData);
    }
}
