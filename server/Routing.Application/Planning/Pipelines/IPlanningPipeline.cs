using Routing.Application.Mappings;
using Routing.Application.Planning.Candidates.Generators;
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Candidates.Scoring;
using Routing.Application.Planning.Goals;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Profiles;
using Routing.Domain.Models;

namespace Routing.Application.Planning.Pipelines
{
    public interface IPlanningPipeline<in TIntent, TCandidate>
       where TIntent : ITripIntent
       where TCandidate : TripCandidate
    {
        Task<IReadOnlyList<TripPlan>> PlanAsync(TIntent intent, UserRoutingProfile profile, CancellationToken ct);
    }
}
