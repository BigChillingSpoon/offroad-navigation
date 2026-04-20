using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;

namespace Routing.Application.Planning.Goals
{
    public interface ITripGoal<in TIntent, in TCandidate>
        where TIntent : ITripIntent
        where TCandidate : TripCandidate
    {
        /// <summary>
        /// This method serves as a "hard filter" to discard corrupted or impossible routes.
        /// <returns>
        /// True if the candidate is technically valid and meets the hard constraints of the intent.
        /// </returns>
        /// </summary>
        bool IsSatisfied(TCandidate candidate, TIntent intent);

        /// <summary>
        /// How close we are to our goal (heuristics).
        /// Higher = better.
        /// </summary>
        double GetGoalScore(TCandidate candidate, TIntent intent);
    }
}