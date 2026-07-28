using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;

namespace Routing.Application.Planning.Candidates.Selection
{
    /// <summary>
    /// Decides which of the already-scored candidates actually make it to the client. Runs after
    /// scoring, not as part of it - a scorer's only job is to assign a score, not to decide which
    /// scored candidates survive. Most trip types have nothing to select (every scored candidate is
    /// already a distinct final result) and register <see cref="PassThroughCandidateSelector{TIntent,TCandidate}"/>;
    /// trip types that can produce multiple competing candidates for the same logical slot (e.g. a
    /// loop tried from the same entrance via several skeletons) register their own selector instead.
    /// </summary>
    public interface ICandidateSelector<TIntent, TCandidate>
        where TIntent : ITripIntent
        where TCandidate : TripCandidate
    {
        IReadOnlyList<ScoredTripCandidate<TCandidate>> Select(IReadOnlyList<ScoredTripCandidate<TCandidate>> scored, TIntent intent);
    }
}
