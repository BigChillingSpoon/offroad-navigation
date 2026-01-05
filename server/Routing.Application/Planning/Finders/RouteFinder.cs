using Routing.Application.Planning.Goals;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Planner;
using Routing.Application.Planning.Profiles;
using Routing.Domain.Enums;
using Routing.Domain.Models;

namespace Routing.Application.Planning.Finders
{
    public class RouteFinder : IRouteFinder
    {
        private readonly ITripPlanner _planner;
        public RouteFinder(ITripPlanner planner)
        {
            _planner = planner;
        }

        public async Task<Trip> FindRouteAsync(RouteIntent intent, UserRoutingProfile profile, CancellationToken ct)
        {
            var goal = new RouteGoal();
            var settings = new PlannerSettings();

            var plan = await _planner.PlanAsync(intent, goal, profile, settings, ct);
            var trip = Trip.Create("Test route", TripType.Route, plan);
            return trip;
        }
    }
}
