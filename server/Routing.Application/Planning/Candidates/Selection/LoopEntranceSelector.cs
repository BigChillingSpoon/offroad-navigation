using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;

namespace Routing.Application.Planning.Candidates.Selection
{
    /// <summary>
    /// Keeps only the best-scored candidate per entrance. Several skeleton attempts get sent to
    /// GraphHopper individually for the same entrance so the best one can be picked from real
    /// routed data - only one of them was ever meant to be a separate final result.
    /// </summary>
    public sealed class LoopEntranceSelector : ICandidateSelector<LoopIntent, LoopTripCandidate>
    {
        public IReadOnlyList<ScoredTripCandidate<LoopTripCandidate>> Select(IReadOnlyList<ScoredTripCandidate<LoopTripCandidate>> scored, LoopIntent intent)
        {
            return scored
                .GroupBy(s => s.Candidate.EntranceCoordinate)
                .Select(g => g.OrderByDescending(x => x.Score).First()) // stable: first-generated wins on an exact score tie
                .ToList();
        }
    }
}
