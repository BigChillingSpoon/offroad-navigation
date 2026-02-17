using Microsoft.Extensions.DependencyInjection;
using Routing.Domain.Repositories;
using Routing.Infrastructure.GraphHopper;
using Routing.Infrastructure.Repositories;
using Routing.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using Routing.Infrastructure.GraphHopper.JsonConverters;
using System.Text.Json;
using Routing.Infrastructure.GraphHopper.Mappings;
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

            //HTTP Client
            services.AddHttpClient<IRoutingProvider, GraphHopperService>()
                .ConfigureHttpClient((sp, client) =>
                {
                    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GraphHopperOptions>>().Value;
                    client.BaseAddress = new Uri(options.BaseUrl);
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

                options.Converters.Add(new GraphHopperDetailSegmentConverter());

                return options;
            });

            //Mappers
            services.AddSingleton<GraphHopperResponseMapper>();

            return services;
        }
    }
}
