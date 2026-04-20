using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Profiles;

namespace Routing.Application.Planning.Candidates.Scoring
{
    public interface ITripCandidateScorer<in TIntent, TCandidate>
        where TIntent : ITripIntent
        where TCandidate : TripCandidate
    {
        IReadOnlyList<ScoredTripCandidate<TCandidate>> Score(IReadOnlyList<TCandidate> candidates, TIntent intent, UserRoutingProfile profile);
    }
}