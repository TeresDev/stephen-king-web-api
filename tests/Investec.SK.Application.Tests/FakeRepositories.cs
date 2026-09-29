using Investec.SK.Application.Abstractions;
using Investec.SK.Application.Responses;
using Investec.SK.Domain;

namespace Investec.SK.Application.Tests;

public sealed class FakeBookRepository : IBookRepository
{
    private readonly UpstreamResult<SourceData<Book>> _result;

    private FakeBookRepository(UpstreamResult<SourceData<Book>> result) => _result = result;

    public int CallCount { get; private set; }

    public static FakeBookRepository Returning(params Book[] books) =>
        new(UpstreamResult<SourceData<Book>>.Success(new SourceData<Book>(books, [])));

    public static FakeBookRepository ReturningWithSkips(
        IReadOnlyList<Book> books, IReadOnlyList<SkippedRecord> skipped) =>
        new(UpstreamResult<SourceData<Book>>.Success(new SourceData<Book>(books, skipped)));

    public static FakeBookRepository Failing(
        UpstreamFailureKind kind = UpstreamFailureKind.ServerError,
        int? status = 503) =>
        new(UpstreamResult<SourceData<Book>>.Failed(new UpstreamFailure(
            UpstreamSources.Books,
            "https://stephen-king-api.onrender.com/api/books",
            status,
            kind,
            "Upstream unavailable")));

    public Task<UpstreamResult<SourceData<Book>>> GetAllAsync(CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(_result);
    }
}

public sealed class FakeVillainRepository : IVillainRepository
{
    private readonly UpstreamResult<SourceData<Villain>> _result;

    private FakeVillainRepository(UpstreamResult<SourceData<Villain>> result) => _result = result;

    public int CallCount { get; private set; }

    public static FakeVillainRepository Returning(params Villain[] villains) =>
        new(UpstreamResult<SourceData<Villain>>.Success(new SourceData<Villain>(villains, [])));

    public static FakeVillainRepository ReturningWithSkips(
        IReadOnlyList<Villain> villains, IReadOnlyList<SkippedRecord> skipped) =>
        new(UpstreamResult<SourceData<Villain>>.Success(new SourceData<Villain>(villains, skipped)));

    public static FakeVillainRepository Failing(
        UpstreamFailureKind kind = UpstreamFailureKind.ServerError,
        int? status = 503) =>
        new(UpstreamResult<SourceData<Villain>>.Failed(new UpstreamFailure(
            UpstreamSources.Villains,
            "https://stephen-king-api.onrender.com/api/villains",
            status,
            kind,
            "Upstream unavailable")));

    public Task<UpstreamResult<SourceData<Villain>>> GetAllAsync(CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(_result);
    }
}
