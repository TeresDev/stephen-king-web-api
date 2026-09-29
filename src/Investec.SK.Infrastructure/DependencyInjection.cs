using Investec.SK.Application.Abstractions;
using Investec.SK.Application.Derry;
using Investec.SK.Application.Maine;
using Investec.SK.Application.Summary;
using Investec.SK.Infrastructure.Http;
using Investec.SK.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Investec.SK.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SkApiOptions>(configuration.GetSection(SkApiOptions.SectionName));
        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);

        var options = configuration.GetSection(SkApiOptions.SectionName).Get<SkApiOptions>()
                      ?? new SkApiOptions();

        services.AddHttpClient<ISkApiClient, SkApiClient>(client =>
                {
                    client.BaseAddress = options.BaseAddress;
                    client.Timeout = options.TotalTimeout;
                })
                .AddResilienceHandler("sk-api", builder =>
                {
                    builder.AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = options.RetryCount,
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                        Delay = TimeSpan.FromMilliseconds(200),
                    });

                    builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                    {
                        FailureRatio = 0.5,
                        MinimumThroughput = 10,
                        SamplingDuration = TimeSpan.FromSeconds(30),
                        BreakDuration = TimeSpan.FromSeconds(15),
                    });

                    builder.AddTimeout(options.AttemptTimeout);
                });

        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IVillainRepository, VillainRepository>();
        services.AddScoped<GetMaineVillainsQuery>();
        services.AddScoped<GetFemaleDerryQuery>();
        services.AddScoped<GetMaineSummaryQuery>();

        return services;
    }
}
