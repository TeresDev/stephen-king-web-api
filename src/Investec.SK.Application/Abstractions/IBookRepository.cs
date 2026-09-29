using Investec.SK.Domain;

namespace Investec.SK.Application.Abstractions;

public interface IBookRepository
{
    Task<UpstreamResult<IReadOnlyList<Book>>> GetAllAsync(CancellationToken cancellationToken);
}
