using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;

namespace Routing.Application.Planning.Goals
{
    public class RouteGoal : ITripGoal<RouteIntent>
    {
        public bool IsSatisfied(TripCandidate state, RouteIntent intent)
        {
            return true;
        }

        public double GetGoalScore(TripCandidate state, RouteIntent intent)
        {
            return 0d;
        }
    }
}
