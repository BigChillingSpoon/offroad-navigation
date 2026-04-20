using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;

namespace Routing.Application.Planning.Goals
{
    public sealed class LoopGoal : ITripGoal<LoopIntent, LoopTripCandidate>
    {
        public bool IsSatisfied(LoopTripCandidate candidate, LoopIntent intent)
        {
            //to be implemented
            return true;
        }

        public double GetGoalScore(LoopTripCandidate candidate, LoopIntent intent)
        {
            return 0d;
        }
    }
}
