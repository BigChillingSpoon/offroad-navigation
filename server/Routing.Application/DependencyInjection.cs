using Microsoft.Extensions.DependencyInjection;
using Routing.Application.Loops.Commands;
using Routing.Application.Planning.Finders;
using Routing.Application.Planning.Planner;
using Routing.Application.Planning;
using Routing.Application.Routes.Commands;
using Routing.Application.Contracts;
using Routing.Application.Routes;
using Routing.Application.Routes.Queries;
using Routing.Application.Loops.Queries;
using Routing.Application.Planning.Scoring;
using Routing.Application.Loops;

namespace Routing.Domain
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddRoutingApplication(this IServiceCollection services)
        {
            // MODULES
            services.AddScoped<IRoutesModule, RoutesModule>();
            services.AddScoped<ILoopsModule, LoopsModule>();

            // QUERIES + COMMANDS
            services.AddScoped<ILoopsCommands, LoopsCommands>();
            services.AddScoped<IRoutesCommands, RoutesCommands>();
            services.AddScoped<IRoutesQueries, RoutesQueries>();
            services.AddScoped<ILoopsQueries, LoopsQueries>();
            
            // PLANNING
            services.AddScoped<ITripPlanner, TripPlanner>();
            services.AddScoped<ITileSelector, TileSelector>();
            services.AddScoped<ILoopFinder, LoopFinder>();
            services.AddScoped<IRouteFinder, RouteFinder>();
            services.AddScoped<ISegmentScorer, SegmentScorer>();

            return services;
        }
    }
}
