using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;

namespace Routing.Application.Planning.Goals
{
    //ukazuje planneru kdy ma skoncit planovani
    //vyhodnocuje konec na zaklade plannerstate a rozhoduje zda je cil dosazen
    public interface ITripGoal<in TIntent> where TIntent : ITripIntent
    {
        /// <summary>
        /// This method serves as a "hard filter" to discard corrupted or impossible routes.
        /// <returns>
        /// True if the candidate is technically valid (contains segments with geometry 
        /// and maintains referential continuity between them); otherwise, false.
        /// </returns>
        /// </summary>
        bool IsSatisfied(TripCandidate candidate, TIntent intent);

        /// <summary>
        /// how close we are to our goal (heuristics).
        /// higher = better.
        /// </summary>
        double GetGoalScore(TripCandidate candidate, TIntent intent);
    }
}


