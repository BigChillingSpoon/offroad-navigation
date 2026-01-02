using Routing.Application.Planning.Goals;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Profiles;
using Routing.Application.Planning.State;

namespace Routing.Application.Planning.Planner
{
    public class TripPlanner : ITripPlanner
    {
        public Task PlanAsync<TIntent>(TIntent intent, ITripGoal<TIntent> goal, UserRoutingProfile profile, PlannerSettings settings, CancellationToken ct) where TIntent : ITripIntent
        {
            var state = PlannerState.Initialize(intent.Start);

            while (!goal.IsSatisfied(state, intent))
            {
                //plan
                //tile selector  - selects next tile
                //state.update() - updates history, costs, position
            }
            //todo return actual planned route/loop
            return null;
        }
    }
}
