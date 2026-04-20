using Routing.Application.Mappings;
using Routing.Application.Planning.Candidates.Generators;
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Candidates.Scoring;
using Routing.Application.Planning.Goals;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Planner;
using Routing.Application.Planning.Profiles;
using Routing.Domain.Models;

namespace Routing.Application.Planning.Pipelines
{
    public interface IPlanningPipeline<in TIntent> where TIntent : ITripIntent
    {
        Task<IReadOnlyList<TripPlan>> PlanAsync(TIntent intent, UserRoutingProfile profile, PlannerSettings settings, CancellationToken ct);
    }

    internal sealed class RoutePlanningPipeline : BasePlanningPipeline<RouteIntent, TripCandidate>
    {
        public RoutePlanningPipeline(
            ICandidateGenerator<RouteIntent, TripCandidate> generator,
            ITripGoal<RouteIntent, TripCandidate> goal,
            ITripCandidateScorer<RouteIntent, TripCandidate> scorer)
            : base(generator, goal, scorer)
        {
        }

        protected override TripPlan MapToPlan(TripCandidate candidate)
        {
            return candidate.ToTripPlan();
        }
    }

    internal sealed class LoopPlanningPipeline : BasePlanningPipeline<LoopIntent, LoopTripCandidate>
    {
        public LoopPlanningPipeline(
            ICandidateGenerator<LoopIntent, LoopTripCandidate> generator,
            ITripGoal<LoopIntent, LoopTripCandidate> goal,
            ITripCandidateScorer<LoopIntent, LoopTripCandidate> scorer)
            : base(generator, goal, scorer)
        {
        }

        protected override TripPlan MapToPlan(LoopTripCandidate candidate)
        {
            //to be implemented specificly for loop
            var basePlan = candidate.ToTripPlan();
            return basePlan;
        }
    }
}
