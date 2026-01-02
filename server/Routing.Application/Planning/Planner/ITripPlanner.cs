using Routing.Application.Planning.Goals;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Profiles;

namespace Routing.Application.Planning.Planner
{
    public interface ITripPlanner
    {
        //in future add some result to be returned
        public Task PlanAsync<TIntent>(TIntent intent, ITripGoal<TIntent> goal, UserRoutingProfile profile, PlannerSettings settings, CancellationToken ct) where TIntent : ITripIntent;
    }
}
