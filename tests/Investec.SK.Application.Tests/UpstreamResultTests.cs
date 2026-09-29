using Investec.SK.Application.Abstractions;

namespace Investec.SK.Application.Tests;

public class UpstreamResultTests
{
    [Fact]
    public void Success_CarriesValueAndNoFailure()
    {
        var result = UpstreamResult<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Null(result.Failure);
    }

    [Fact]
    public void Failed_CarriesFailureAndNoValue()
    {
        var failure = new UpstreamFailure(
            UpstreamSources.Books,
            "https://stephen-king-api.onrender.com/api/books",
            504,
            UpstreamFailureKind.Timeout,
            "No response within 5s");

        var result = UpstreamResult<int>.Failed(failure);

        Assert.False(result.IsSuccess);
        Assert.Equal(default, result.Value);
        Assert.NotNull(result.Failure);
        Assert.Equal(UpstreamFailureKind.Timeout, result.Failure.Kind);
        Assert.Equal("books", result.Failure.Source);
        Assert.Equal(504, result.Failure.Status);
    }

    [Fact]
    public void Failure_StatusIsOptionalForTransportErrors()
    {
        var failure = new UpstreamFailure(
            UpstreamSources.Villains,
            "https://stephen-king-api.onrender.com/api/villains",
            Status: null,
            UpstreamFailureKind.Transport,
            "Name or service not known");

        Assert.Null(failure.Status);
        Assert.Equal(UpstreamFailureKind.Transport, failure.Kind);
    }

    [Fact]
    public void Sources_UseStableLowercaseNames()
    {
        Assert.Equal("books", UpstreamSources.Books);
        Assert.Equal("villains", UpstreamSources.Villains);
    }
}
