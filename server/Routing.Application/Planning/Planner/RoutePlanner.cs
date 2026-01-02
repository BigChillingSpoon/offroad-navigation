using Routing.Application.Planning.Goals;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Models;
using Routing.Application.Planning.Profiles;
using Routing.Application.Planning.State;

namespace Routing.Application.Planning.Planner
{
    public class RoutePlanner : IRoutePlanner
    {
        public Task PlanAsync<TIntent>(TIntent intent, IPlanningGoal<TIntent> goal, UserRoutingProfile profile, PlannerConfig config, CancellationToken ct) where TIntent : IRoutingIntent
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
