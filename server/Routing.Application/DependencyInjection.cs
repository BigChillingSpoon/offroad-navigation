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
using Routing.Application.Planning.Candidates.Generators;
using Routing.Application.Planning.Candidates.Scoring;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Candidates.Builders;
using NetTopologySuite.Features;
using NetTopologySuite.IO;

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
            services.AddScoped<ITripCandidateGeneratorFactory, TripCandidateGeneratorFactory>();
            services.AddScoped<ICandidateGenerator<RouteIntent>, RouteCandidateGenerator>();
            services.AddScoped<ITripCandidateScorerFactory, TripCandidateScorerFactory>();
            services.AddScoped<ITripCandidateScorer<RouteIntent>, RouteCandidateScorer>();
            services.AddScoped<ITripCandidateScorer<LoopIntent>, LoopCandidateScorer>();
            services.AddScoped<IRestrictedZoneBuilder, RestrictedZoneBuilder>();
            //services.AddScoped<ICandidateGenerator<LoopIntent>, Loop>

            var geoJsonPath = "../../routing/graphhopper/data/restricted_areas/cz_national_parks.geojson.json"; 
            var parksCollection = new GeoJsonReader().Read<FeatureCollection>(File.ReadAllText(geoJsonPath));
            services.AddSingleton(parksCollection);

            return services;
        }
    }
}
