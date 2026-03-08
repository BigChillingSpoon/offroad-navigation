using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Planner;
using Routing.Application.Planning.Profiles;

namespace Routing.Application.Planning.Candidates.Scoring
{
    public interface ITripCandidateScorer<TIntent> where TIntent : ITripIntent
    {
        IReadOnlyList<ScoredTripCandidate> Score(IReadOnlyList<TripCandidate> candidates, TIntent intent, UserRoutingProfile profile, PlannerSettings settings);
    }
}
