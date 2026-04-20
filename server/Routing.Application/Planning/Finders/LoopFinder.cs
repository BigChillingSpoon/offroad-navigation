using Offroad.Core;
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Pipelines;
using Routing.Application.Planning.Profiles;
using Routing.Domain.Enums;
using Routing.Domain.Models;

namespace Routing.Application.Planning.Finders
{
    public class LoopFinder : ILoopFinder
    {
        private readonly IPlanningPipeline<LoopIntent,LoopTripCandidate> _planner;
        public LoopFinder(IPlanningPipeline<LoopIntent, LoopTripCandidate> planner)
        {
            _planner = planner;
        }

        public async Task<Result<List<Trip>>> FindLoopsAsync(LoopIntent intent, UserRoutingProfile profile, CancellationToken ct)
        {
            var plans = await _planner.PlanAsync(intent, profile, ct);

            if (!plans.Any())
                return Error.Validation("No loops found.");

            var loops = plans.Select(p => Trip.Create("Test loop", TripType.Loop, p));
            return loops.ToList();
        }
    }
}
