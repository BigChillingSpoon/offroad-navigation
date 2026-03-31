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
    private readonly string _originalWorkingDirectory = Directory.GetCurrentDirectory();

    public MockGraphHopperHandler GraphHopperHandler { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // The app loads GeoJSON via File.ReadAllText with a path relative to CWD.
        // Point CWD to the API project so "../../routing/..." resolves correctly.
        Directory.SetCurrentDirectory(ResolveApiProjectDirectory());

        // Ensure debug output directory exists (PlanningDebugExtensions.LogToGPX)
        try { Directory.CreateDirectory(@"C:\tmp"); } catch { /* best-effort on CI */ }

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GraphHopper:BaseUrl"] = "http://graphhopper.test:8989"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Swap the primary HTTP handler for the GraphHopper typed client.
            // The handler chain becomes: HttpClient → Polly ResilienceHandler → our mock.
            services.Configure<HttpClientFactoryOptions>(
                typeof(IRoutingProvider).Name,
                options =>
                {
                    options.HttpMessageHandlerBuilderActions.Add(handlerBuilder =>
                    {
                        handlerBuilder.PrimaryHandler = GraphHopperHandler;
                    });
                });

            // Tune resilience for fast tests:
            //  - 1 retry (instead of 3) so timeout tests finish in ~8s, not ~30s
            //  - Long circuit-breaker window so a timeout test can't trip the breaker for subsequent tests
            services.PostConfigureAll<HttpStandardResilienceOptions>(options =>
            {
                options.Retry.MaxRetryAttempts = 1;
                options.Retry.Delay = TimeSpan.FromMilliseconds(50);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(10);
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        // Restore CWD so other test projects sharing the process aren't affected
        try { Directory.SetCurrentDirectory(_originalWorkingDirectory); } catch { }
        base.Dispose(disposing);
    }

    private static string ResolveApiProjectDirectory()
    {
        // Walk up from test output bin/ folder until we find the solution root
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Offroad.sln")))
            dir = dir.Parent;

        return dir is not null
            ? Path.Combine(dir.FullName, "Offroad.Api")
            : throw new InvalidOperationException(
                "Could not locate Offroad.sln. Ensure the test runs from within the repository tree.");
    }
}
