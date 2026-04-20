using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Routing.Application.Loops.Commands;
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
using Routing.Application.Planning.Pipelines;
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Goals;
using Routing.Application.Planning.Mappings;
using Routing.Application.Mappings;

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

            //PIPELINES
            services.AddScoped(typeof(IPlanningPipeline<,>), typeof(PlanningPipeline<,>));

            //CANDIDATES
            services.AddScoped<ICandidateGenerator<RouteIntent, TripCandidate>, RouteCandidateGenerator>();
            services.AddScoped<ICandidateGenerator<LoopIntent, LoopTripCandidate>, LoopCandidateGenerator>();
            services.AddScoped<ITripCandidateScorer<RouteIntent, TripCandidate>, RouteCandidateScorer>();
            services.AddScoped<ITripCandidateScorer<LoopIntent, LoopTripCandidate>, LoopCandidateScorer>();


            //GOALS
            services.AddScoped<ITripGoal<LoopIntent, LoopTripCandidate>, LoopGoal>();
            services.AddScoped<ITripGoal<RouteIntent, TripCandidate>, RouteGoal>();

            //BUILDERS
            services.AddScoped<IRestrictedZoneBuilder, RestrictedZoneBuilder>();
            
            // OPTIONS
            services.Configure<ScoringProfiles>(configuration.GetSection(ScoringProfiles.SectionName));

            // VALIDATION
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // MAPPERS
            services.AddScoped<ITripMapper<TripCandidate>,RouteTripMapper>();
            services.AddScoped<ITripMapper<LoopTripCandidate>,LoopTripMapper>();
            return services;
        }
    }
}
