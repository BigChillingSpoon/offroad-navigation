using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;

namespace Routing.Application.Planning.Goals
{
    public class LoopGoal : ITripGoal<LoopIntent>
    {
        public bool IsSatisfied(TripCandidate candidate, LoopIntent intent)
        {
            return true;
        }

        public double GetGoalScore(TripCandidate candidate, LoopIntent intent)
        {
            return 0d;
        }
    }
}
