using Investec.SK.Application.Abstractions;
using Investec.SK.Application.Responses;
using Investec.SK.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Investec.SK.Api.IntegrationTests;

/// <summary>
/// Boots the real pipeline — routing, validation, ProblemDetails, JSON — with the two upstream
/// repositories replaced. No test reaches the network.
/// </summary>
public sealed class TestApiFactory(
    UpstreamResult<SourceData<Book>> books,
    UpstreamResult<SourceData<Villain>> villains)
    : WebApplicationFactory<Program>
{
    // ConfigureTestServices — NOT IHostBuilder.ConfigureServices — because only the former is
    // guaranteed to run AFTER Program.cs's AddInfrastructure(). Registering the stubs earlier
    // lets the real repositories win and the tests silently hit the network.
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IBookRepository>();
            services.RemoveAll<IVillainRepository>();
            services.AddScoped<IBookRepository>(_ => new StubBookRepository(books));
            services.AddScoped<IVillainRepository>(_ => new StubVillainRepository(villains));
        });
    }

    public static TestApiFactory Healthy() => new(
        UpstreamResult<SourceData<Book>>.Success(new SourceData<Book>(SampleData.Books, [])),
        UpstreamResult<SourceData<Villain>>.Success(new SourceData<Villain>(SampleData.Villains, [])));

    public static TestApiFactory HealthyWithSourceSkips(
        IReadOnlyList<SkippedRecord> bookSkips, IReadOnlyList<SkippedRecord> villainSkips) => new(
        UpstreamResult<SourceData<Book>>.Success(new SourceData<Book>(SampleData.Books, bookSkips)),
        UpstreamResult<SourceData<Villain>>.Success(new SourceData<Villain>(SampleData.Villains, villainSkips)));

    public static TestApiFactory BooksDown() => new(
        UpstreamResult<SourceData<Book>>.Failed(Failure(UpstreamSources.Books)),
        UpstreamResult<SourceData<Villain>>.Success(new SourceData<Villain>(SampleData.Villains, [])));

    public static TestApiFactory VillainsDown() => new(
        UpstreamResult<SourceData<Book>>.Success(new SourceData<Book>(SampleData.Books, [])),
        UpstreamResult<SourceData<Villain>>.Failed(Failure(UpstreamSources.Villains)));

    public static TestApiFactory BothDown() => new(
        UpstreamResult<SourceData<Book>>.Failed(Failure(UpstreamSources.Books)),
        UpstreamResult<SourceData<Villain>>.Failed(Failure(UpstreamSources.Villains)));

    private static UpstreamFailure Failure(string source) => new(
        source,
        $"https://stephen-king-api.onrender.com/api/{source}",
        503,
        UpstreamFailureKind.ServerError,
        "Upstream unavailable");

    private sealed class StubBookRepository(UpstreamResult<SourceData<Book>> result) : IBookRepository
    {
        public Task<UpstreamResult<SourceData<Book>>> GetAllAsync(CancellationToken ct) =>
            Task.FromResult(result);
    }

    private sealed class StubVillainRepository(UpstreamResult<SourceData<Villain>> result) : IVillainRepository
    {
        public Task<UpstreamResult<SourceData<Villain>>> GetAllAsync(CancellationToken ct) =>
            Task.FromResult(result);
    }
}

/// <summary>A small deterministic cast that exercises every response feature.</summary>
public static class SampleData
{
    public static IReadOnlyList<Book> Books { get; } =
    [
        Book(1, "It", 1986, 1138, "978-0-670-81302-5"),
        Book(2, "The Stand", 1978, 823, "978-0-385-12168-2"),
        Book(3, "Carrie", 1974, 199, "978-0-385-08695-0"),
    ];

    public static IReadOnlyList<Villain> Villains { get; } =
    [
        Maine(10, "Pennywise", Link(1, "It")),
        Maine(11, "Randall Flagg", Link(2, "The Stand")),
        Maine(12, "Margaret White", Link(3, "Carrie")),
        Maine(13, "Bob Anderson"),                                  // no books -> skipped[]
        Derry(20, "Greta Bowie", Gender.Female, "Derry residents", Link(1, "It")),
        Derry(21, "Marcia Fadden", Gender.Female, "Dery residents"), // tolerated spelling
        Derry(22, "Henry Bowers", Gender.Male, "Derry residents", Link(1, "It")),
    ];

    private static Book Book(int id, string title, int year, int pages, string isbn)
    {
        BookTitle.TryCreate(title, out var t);
        PublishYear.TryCreate(year, out var y);
        PageCount.TryCreate(pages, out var p);
        Isbn.TryCreate(isbn, out var i);
        return new Book(new BookId(id), t, y, i, p, "Doubleday", []);
    }

    private static BookLink Link(int id, string title) => new(new BookId(id), title);

    private static Villain Maine(int id, string name, params BookLink[] books)
    {
        VillainName.TryCreate(name, out var n);
        return new Villain(new VillainId(id), n, Gender.Male, "Deceased",
                           ResidenceNotes.From(["Residents of Maine"]), books);
    }

    private static Villain Derry(int id, string name, Gender gender, string note, params BookLink[] books)
    {
        VillainName.TryCreate(name, out var n);
        return new Villain(new VillainId(id), n, gender, "Deceased",
                           ResidenceNotes.From([note]), books);
    }
}
