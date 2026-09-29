using Investec.SK.Application.Abstractions;
using Investec.SK.Domain;

namespace Investec.SK.Application.Tests;

public sealed class FakeBookRepository : IBookRepository
{
    private readonly UpstreamResult<IReadOnlyList<Book>> _result;

    private FakeBookRepository(UpstreamResult<IReadOnlyList<Book>> result) => _result = result;

    public int CallCount { get; private set; }

    public static FakeBookRepository Returning(params Book[] books) =>
        new(UpstreamResult<IReadOnlyList<Book>>.Success(books));

    public static FakeBookRepository Failing(
        UpstreamFailureKind kind = UpstreamFailureKind.ServerError,
        int? status = 503) =>
        new(UpstreamResult<IReadOnlyList<Book>>.Failed(new UpstreamFailure(
            UpstreamSources.Books,
            "https://stephen-king-api.onrender.com/api/books",
            status,
            kind,
            "Upstream unavailable")));

    public Task<UpstreamResult<IReadOnlyList<Book>>> GetAllAsync(CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(_result);
    }
}

public sealed class FakeVillainRepository : IVillainRepository
{
    private readonly UpstreamResult<IReadOnlyList<Villain>> _result;

    private FakeVillainRepository(UpstreamResult<IReadOnlyList<Villain>> result) => _result = result;

    public int CallCount { get; private set; }

    public static FakeVillainRepository Returning(params Villain[] villains) =>
        new(UpstreamResult<IReadOnlyList<Villain>>.Success(villains));

    public static FakeVillainRepository Failing(
        UpstreamFailureKind kind = UpstreamFailureKind.ServerError,
        int? status = 503) =>
        new(UpstreamResult<IReadOnlyList<Villain>>.Failed(new UpstreamFailure(
            UpstreamSources.Villains,
            "https://stephen-king-api.onrender.com/api/villains",
            status,
            kind,
            "Upstream unavailable")));

    public Task<UpstreamResult<IReadOnlyList<Villain>>> GetAllAsync(CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(_result);
    }
}
