using Microsoft.Extensions.DependencyInjection;
using Routing.Domain.Repositories;
using Routing.Infrastructure.GraphHopper;
using Routing.Infrastructure.Repositories;

namespace Routing.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddRoutingInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<IGraphHopperService, GraphHopperService>();
            services.AddSingleton<ITripRepository, InMemoryTripRepository>();
            return services;
        }
    }
}
