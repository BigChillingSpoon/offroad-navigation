using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Routing.Domain.Repositories;
using Routing.Infrastructure.GraphHopper;
using Routing.Infrastructure.Repositories;
using Routing.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Routing.Infrastructure.GraphHopper.JsonConverters;
using System.Text.Json;
using Routing.Infrastructure.GraphHopper.Mappings;
using Microsoft.EntityFrameworkCore;
using Routing.Infrastructure.Data;
using Routing.Infrastructure.Persistance;

namespace Routing.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddRoutingInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            //Options
            services.AddOptions<GraphHopperOptions>()
                .Bind(configuration.GetSection(GraphHopperOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            //HTTP Client with Resilience Pipeline
            services.AddHttpClient<IRoutingProvider, GraphHopperService>()
                .ConfigureHttpClient((sp, client) =>
                {
                    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GraphHopperOptions>>().Value;
                    client.BaseAddress = new Uri(options.BaseUrl);
                })
                .AddStandardResilienceHandler(options =>
                {
                    // Per-attempt timeout — dynamic based on route distance
                    options.AttemptTimeout.TimeoutGenerator = args =>
                    {
                        if (args.Context.Properties.TryGetValue(
                                new Polly.ResiliencePropertyKey<HttpRequestMessage>("Polly.Http.RequestMessage"),
                                out var request)
                            && request.Options.TryGetValue(GraphHopperService.DynamicTimeoutKey, out var dynamicTimeout))
                        {
                            return new ValueTask<TimeSpan>(dynamicTimeout);
                        }
                        return new ValueTask<TimeSpan>(TimeSpan.FromSeconds(10));
                    };

                    // Total timeout — 2.5x the per-attempt timeout to allow retries
                    options.TotalRequestTimeout.TimeoutGenerator = args =>
                    {
                        if (args.Context.Properties.TryGetValue(
                                new Polly.ResiliencePropertyKey<HttpRequestMessage>("Polly.Http.RequestMessage"),
                                out var request)
                            && request.Options.TryGetValue(GraphHopperService.DynamicTimeoutKey, out var dynamicTimeout))
                        {
                            return new ValueTask<TimeSpan>(TimeSpan.FromSeconds(dynamicTimeout.TotalSeconds * 2.5));
                        }
                        return new ValueTask<TimeSpan>(TimeSpan.FromSeconds(30));
                    };

                    // Retry with exponential backoff
                    options.Retry.MaxRetryAttempts = 3;

                    // Circuit breaker — stop hammering a failing service
                    options.CircuitBreaker.FailureRatio = 0.5;
                    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
                });

            //Repository
            services.AddSingleton<ITripRepository, InMemoryTripRepository>();

            //Json Options
            services.AddSingleton<JsonSerializerOptions>(_ =>
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                options.Converters.Add(new GraphHopperAttributeIntervalConverterFactory());   

                return options;
            });

            //Mappers
            services.AddSingleton<GraphHopperResponseMapper>();

            //DB CONTEXT
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"), 
                    x => x.UseNetTopologySuite()
                ));

            //GIS related
            services.AddScoped<GisDataSeeder>();
            return services;
        }
    }
}
