using Routing.Application.Planning.Intents;
using Routing.Application.Planning.State;

namespace Routing.Application.Planning.Goals
{
    public class RouteGoal : ITripGoal<RouteIntent>
    {
        public bool IsSatisfied(PlannerState state, RouteIntent intent)
        {
            return false;
        }

        public double GetGoalScore(PlannerState state, RouteIntent intent)
        {
            return 0d;
        }
    }
}
