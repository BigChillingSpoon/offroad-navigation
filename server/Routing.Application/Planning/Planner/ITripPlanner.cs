using Routing.Application.Planning.Goals;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Profiles;
using Routing.Domain.Models;

namespace Routing.Application.Planning.Planner
{
    public interface ITripPlanner
    {
        public Task<TripPlan> PlanAsync<TIntent>(TIntent intent, ITripGoal<TIntent> goal, UserRoutingProfile profile, PlannerSettings settings, CancellationToken ct) where TIntent : ITripIntent;
    }
}
