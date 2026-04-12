using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Resilience;
using Routing.Application.Abstractions;
using Routing.Infrastructure.Data;
using Routing.Infrastructure.Persistance; 
using Testcontainers.PostgreSql;

namespace Offroad.IntegrationTests.Infrastructure;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Offroad.Api.Program>, IAsyncLifetime
{
    public MockGraphHopperHandler GraphHopperHandler { get; } = new();

    // Definition of GIS test container
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgis/postgis:16-3.4") // needs to be same as in real docker image
        .WithDatabase("offroad_test_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        // Create dummy test db tables
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        //For skipping the seeder inside of Program.cs
        builder.UseEnvironment("Testing");

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
            // --- SETTING UP DATABASE (TESTCONTAINERS) ---
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                // We connect to the random port that Docker assigned to us
                options.UseNpgsql(_dbContainer.GetConnectionString(), o => o.UseNetTopologySuite());
            });


            // --- GH SETTINGS --
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

    // DELETION OF TEST CONTAINER
    new public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }
}