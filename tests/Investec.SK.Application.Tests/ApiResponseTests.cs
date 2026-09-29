using Investec.SK.Application.Abstractions;
using Investec.SK.Application.Responses;

namespace Investec.SK.Application.Tests;

public class ResponseMetaTests
{
    private static readonly DateTimeOffset At = new(2026, 9, 29, 11, 16, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(36, 20, 2)]
    [InlineData(40, 20, 2)]
    [InlineData(41, 20, 3)]
    [InlineData(1, 20, 1)]
    [InlineData(0, 20, 0)]
    public void Create_ComputesTotalPagesByCeiling(int totalItems, int pageSize, int expectedPages)
    {
        var meta = ResponseMeta.Create(1, pageSize, totalItems, 0, "pages", "desc", false, At);

        Assert.Equal(expectedPages, meta.TotalPages);
    }

    [Fact]
    public void Create_PreservesRequestedPagingAndSort()
    {
        var meta = ResponseMeta.Create(2, 10, 36, 10, "bookName", "asc", false, At);

        Assert.Equal(2, meta.Page);
        Assert.Equal(10, meta.PageSize);
        Assert.Equal(36, meta.TotalItems);
        Assert.Equal(10, meta.ItemsOnPage);
        Assert.Equal("bookName", meta.Sort);
        Assert.Equal("asc", meta.Direction);
        Assert.Equal(At, meta.RetrievedAt);
    }
}

public class ApiResponseTests
{
    private static readonly DateTimeOffset At = new(2026, 9, 29, 11, 16, 0, TimeSpan.Zero);

    [Fact]
    public void Response_WithNoDiagnostics_IsNotDegraded()
    {
        var meta = ResponseMeta.Create(1, 20, 36, 20, "pages", "desc", false, At);
        var response = new ApiResponse<int[]>([1, 2, 3], meta, [], []);

        Assert.False(response.Meta.Degraded);
        Assert.Empty(response.Errors);
        Assert.Empty(response.Skipped);
    }

    [Fact]
    public void Response_CarriesSkippedRecordsForUnassemblableRows()
    {
        var meta = ResponseMeta.Create(1, 20, 36, 20, "pages", "desc", true, At);
        var skipped = new SkippedRecord(
            "Villain", "Bob Anderson", SkipReason.NoLinkedBooks,
            "Villain is a Maine resident but appears in no book");

        var response = new ApiResponse<int[]>([], meta, [], [skipped]);

        Assert.True(response.Meta.Degraded);
        Assert.Single(response.Skipped);
        Assert.Equal(SkipReason.NoLinkedBooks, response.Skipped[0].Reason);
        Assert.Equal("Bob Anderson", response.Skipped[0].Identifier);
    }

    [Fact]
    public void Response_CarriesUpstreamErrorsForFailedSources()
    {
        var meta = ResponseMeta.Create(1, 20, 0, 0, "pages", "desc", true, At);
        var failure = new UpstreamFailure(
            UpstreamSources.Books, "https://stephen-king-api.onrender.com/api/books",
            504, UpstreamFailureKind.Timeout, "No response within 5s");

        var response = new ApiResponse<int[]>([], meta, [failure], []);

        Assert.Single(response.Errors);
        Assert.Equal("books", response.Errors[0].Source);
        Assert.Equal(UpstreamFailureKind.Timeout, response.Errors[0].Kind);
    }
}
