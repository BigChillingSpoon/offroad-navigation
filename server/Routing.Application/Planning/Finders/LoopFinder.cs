using Offroad.Core;
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

        public async Task<Result<List<Trip>>> FindLoopsAsync(LoopIntent intent, UserRoutingProfile profile, CancellationToken ct)
        {
            var goal = new LoopGoal();
            var settings = new PlannerSettings();
            var plans = await _planner.PlanAsync(intent, goal, profile, settings, ct);

            if (!plans.Any())
                return Error.Validation("No loops found.");

            var loops = plans.Select(p => Trip.Create("Test loop", TripType.Loop, p));
            return loops.ToList();
        }
    }
}
