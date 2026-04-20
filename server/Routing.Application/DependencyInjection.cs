using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
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
using Routing.Application.Loops;
using Routing.Application.Planning.Candidates.Generators;
using Routing.Application.Planning.Candidates.Scoring;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Candidates.Builders;
using NetTopologySuite.Features;
using NetTopologySuite.IO;
using Routing.Application.Planning.Pipelines;
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Goals;

namespace Routing.Domain
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddRoutingApplication(this IServiceCollection services, IConfiguration configuration)
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

            //PIPELINES
            services.AddScoped<IPlanningPipelineFactory, PlanningPipelineFactory>();
            services.AddScoped<IPlanningPipeline<RouteIntent>, RoutePlanningPipeline>();
            services.AddScoped<IPlanningPipeline<LoopIntent>, LoopPlanningPipeline>();

            services.AddScoped<ITileSelector, TileSelector>();//ghost to be removed

            //FINDERS
            services.AddScoped<ILoopFinder, LoopFinder>();
            services.AddScoped<IRouteFinder, RouteFinder>();

            //CANDIDATES
            services.AddScoped<ITripCandidateGeneratorFactory, TripCandidateGeneratorFactory>();
            services.AddScoped<ICandidateGenerator<RouteIntent, TripCandidate>, RouteCandidateGenerator>();
            services.AddScoped<ICandidateGenerator<LoopIntent, LoopTripCandidate>, LoopCandidateGenerator>();
            services.AddScoped<ITripCandidateScorerFactory, TripCandidateScorerFactory>();
            services.AddScoped<ITripCandidateScorer<RouteIntent, TripCandidate>, RouteCandidateScorer>();
            services.AddScoped<ITripCandidateScorer<LoopIntent, LoopTripCandidate>, LoopCandidateScorer>();
            services.AddScoped<ICandidateFactory, TripCandidateFactory>();


            //GOALS
            services.AddScoped<ITripGoal<LoopIntent, LoopTripCandidate>, LoopGoal>();
            services.AddScoped<ITripGoal<RouteIntent, TripCandidate>, RouteGoal>();

            //BUILDERS
            services.AddScoped<IRestrictedZoneBuilder, RestrictedZoneBuilder>();
            
            // OPTIONS
            services.Configure<ScoringProfiles>(configuration.GetSection(ScoringProfiles.SectionName));

            // VALIDATION
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            return services;
        }
    }
}
