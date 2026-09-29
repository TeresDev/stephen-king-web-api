using System.Net;
using System.Text.Json;
using Investec.SK.Application.Abstractions;
using Investec.SK.Infrastructure.Contracts;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Investec.SK.Infrastructure.Http;

public interface ISkApiClient
{
    Task<UpstreamResult<IReadOnlyList<T>>> GetListAsync<T>(
        string source,
        string relativePath,
        CancellationToken cancellationToken);
}

/// <summary>
/// The boundary where exceptions stop. Every upstream outcome becomes a typed
/// <see cref="UpstreamFailure"/>, so no exception ever reaches the Application layer.
/// </summary>
public sealed class SkApiClient(HttpClient httpClient, ILogger<SkApiClient> logger) : ISkApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<UpstreamResult<IReadOnlyList<T>>> GetListAsync<T>(
        string source,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var resource = new Uri(httpClient.BaseAddress!, relativePath).ToString();

        try
        {
            using var response = await httpClient.GetAsync(relativePath, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var kind = response.StatusCode == HttpStatusCode.NotFound
                    ? UpstreamFailureKind.NotFound
                    : UpstreamFailureKind.ServerError;

                logger.LogWarning("Upstream {Source} returned {Status}", source, (int)response.StatusCode);

                return Fail<T>(source, resource, (int)response.StatusCode, kind,
                               $"Upstream responded {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            var envelope = await JsonSerializer.DeserializeAsync<UpstreamEnvelope<T>>(
                stream, JsonOptions, cancellationToken);

            if (envelope?.Data is null)
            {
                return Fail<T>(source, resource, (int)response.StatusCode, UpstreamFailureKind.BadPayload,
                               "Upstream payload had no 'data' array");
            }

            return UpstreamResult<IReadOnlyList<T>>.Success(envelope.Data);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Upstream {Source} returned an unreadable payload", source);
            return Fail<T>(source, resource, null, UpstreamFailureKind.BadPayload,
                           "Upstream payload could not be parsed as JSON");
        }
        catch (BrokenCircuitException)
        {
            return Fail<T>(source, resource, null, UpstreamFailureKind.CircuitOpen,
                           "Circuit is open after repeated upstream failures");
        }
        catch (TimeoutRejectedException)
        {
            return Fail<T>(source, resource, null, UpstreamFailureKind.Timeout,
                           "Upstream did not respond within the configured timeout");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient surfaces its own timeout as a cancellation that the caller did not request.
            return Fail<T>(source, resource, null, UpstreamFailureKind.Timeout,
                           "Upstream did not respond within the configured timeout");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Upstream {Source} was unreachable", source);
            return Fail<T>(source, resource, null, UpstreamFailureKind.Transport, ex.Message);
        }
    }

    private static UpstreamResult<IReadOnlyList<T>> Fail<T>(
        string source, string resource, int? status, UpstreamFailureKind kind, string reason) =>
        UpstreamResult<IReadOnlyList<T>>.Failed(
            new UpstreamFailure(source, resource, status, kind, reason));
}
