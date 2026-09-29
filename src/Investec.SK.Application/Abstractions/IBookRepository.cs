using Investec.SK.Domain;

namespace Investec.SK.Application.Abstractions;

public interface IBookRepository
{
    Task<UpstreamResult<SourceData<Book>>> GetAllAsync(CancellationToken cancellationToken);
}
