using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;

namespace Routing.Application.Planning.Candidates.Selection
{
    /// <summary>
    /// Default selector: keeps every scored candidate unchanged. Registered for trip types that
    /// have no notion of "several competing attempts at the same logical slot" (e.g. routes) -
    /// the pipeline itself never needs to know which trip types opt into real selection.
    /// </summary>
    public sealed class PassThroughCandidateSelector<TIntent, TCandidate> : ICandidateSelector<TIntent, TCandidate>
        where TIntent : ITripIntent
        where TCandidate : TripCandidate
    {
        public IReadOnlyList<ScoredTripCandidate<TCandidate>> Select(IReadOnlyList<ScoredTripCandidate<TCandidate>> scored, TIntent intent) => scored;
    }
}
