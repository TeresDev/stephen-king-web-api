using System.Diagnostics;
using System.Net;

namespace Investec.SK.Infrastructure.Tests;

/// <summary>
/// A scripted HttpMessageHandler. Tests never reach the network; every upstream behaviour the
/// client must classify is produced here deterministically.
/// </summary>
public sealed class StubHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
    : HttpMessageHandler
{
    public int CallCount { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        CallCount++;
        return await respond(request, cancellationToken);
    }

    public static StubHttpMessageHandler RespondingWith(HttpStatusCode status, string body) =>
        new((_, _) => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
        }));

    public static StubHttpMessageHandler Throwing(Exception exception) =>
        new((_, _) => Task.FromException<HttpResponseMessage>(exception));

    public static StubHttpMessageHandler NeverCompleting() =>
        new(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            throw new UnreachableException();
        });
}
