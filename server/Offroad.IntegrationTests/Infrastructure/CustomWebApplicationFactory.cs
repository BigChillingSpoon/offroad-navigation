using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Resilience;
using Routing.Application.Abstractions;

namespace Offroad.IntegrationTests.Infrastructure;

/// <summary>
/// Spins up the full Offroad.Api pipeline with a mocked GraphHopper HTTP backend.
/// The Polly resilience pipeline (retry, timeout, circuit breaker) stays in place
/// and wraps the mock — requests flow through the real middleware stack end-to-end.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Offroad.Api.Program>
{
    public MockGraphHopperHandler GraphHopperHandler { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Routing:ParksGeoJsonPath", "Infrastructure/dummy_parks.json");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GraphHopper:BaseUrl"] = "http://graphhopper.test:8989"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.Configure<HttpClientFactoryOptions>(
                typeof(IRoutingProvider).Name,
                options =>
                {
                    options.HttpMessageHandlerBuilderActions.Add(handlerBuilder =>
                    {
                        handlerBuilder.PrimaryHandler = GraphHopperHandler;
                    });
                });

            services.PostConfigureAll<HttpStandardResilienceOptions>(options =>
            {
                options.Retry.MaxRetryAttempts = 1;
                options.Retry.Delay = TimeSpan.FromMilliseconds(50);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(10);
            });
        });
    }
}