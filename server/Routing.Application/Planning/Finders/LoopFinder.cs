using Routing.Application.Planning.Goals;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Planner;
using Routing.Application.Planning.Profiles;
using Routing.Domain.Enums;
using Routing.Domain.Models;

namespace Routing.Application.Planning.Finders
{
    public class LoopFinder : ILoopFinder
    {
        private readonly ITripPlanner _planner;
        public LoopFinder(ITripPlanner planner)
        {
            _planner = planner;
        }

        public async Task<List<Trip>> FindLoopsAsync(LoopIntent intent, UserRoutingProfile profile, CancellationToken ct)
        {
            var goal = new LoopGoal();
            var settings = new PlannerSettings();
            var plan = await _planner.PlanAsync(intent, goal, profile, settings, ct);
            var loop = Trip.Create("Test loop", TripType.Loop, plan);
            return new List<Trip> { loop };
        }
    }
}
