using Routing.Application.Planning.Goals;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Profiles;
using Routing.Domain.Models;

namespace Routing.Application.Planning.Planner
{
    public interface ITripPlanner
    {
        public Task<IReadOnlyList<TripPlan>> PlanAsync<TIntent>(TIntent intent, UserRoutingProfile profile, CancellationToken ct) where TIntent : ITripIntent;
    }
}
