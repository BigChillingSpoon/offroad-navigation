using Microsoft.Extensions.DependencyInjection;
using Routing.Domain.Repositories;
using Routing.Infrastructure.GraphHopper;
using Routing.Infrastructure.Repositories;
using Routing.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Routing.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddRoutingInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<GraphHopperOptions>()
                .Bind(configuration.GetSection(GraphHopperOptions.SectionName));

            services.AddHttpClient<IGraphHopperService, GraphHopperService>()
                .ConfigureHttpClient((sp, client) =>
                {
                    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GraphHopperOptions>>().Value;
                    client.BaseAddress = new Uri(options.BaseUrl);
                });

            services.AddSingleton<ITripRepository, InMemoryTripRepository>();
            return services;
        }
    }
}
