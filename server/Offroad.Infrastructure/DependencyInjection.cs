using Microsoft.Extensions.DependencyInjection;
using Routing.Infrastructure.GraphHopper;

namespace Routing.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddRoutingInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<IGraphHopperService, GraphHopperService>();
            return services;
        }
    }
}
