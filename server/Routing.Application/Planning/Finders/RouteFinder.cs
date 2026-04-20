using Offroad.Core;
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Pipelines;
using Routing.Application.Planning.Profiles;
using Routing.Domain.Enums;
using Routing.Domain.Models;

namespace Routing.Application.Planning.Finders
{
    public class RouteFinder : IRouteFinder
    {
        private readonly IPlanningPipeline<RouteIntent,TripCandidate> _planner;
        public RouteFinder(IPlanningPipeline<RouteIntent, TripCandidate> planner)
        {
            _planner = planner;
        }

        public async Task<Result<Trip>> FindRouteAsync(RouteIntent intent, UserRoutingProfile profile, CancellationToken ct)
        {

            var plans = await _planner.PlanAsync(intent, profile, ct);
            var firstPlan = plans.FirstOrDefault(); //for now we only return most suitable route, without any alternatives

            //no plan = no trip :)
            if (firstPlan is null)
                return Error.Validation("Couldn't plan route");

            var trip = Trip.Create("Test route", TripType.Route, firstPlan);
            return trip;
        }
    }
}
