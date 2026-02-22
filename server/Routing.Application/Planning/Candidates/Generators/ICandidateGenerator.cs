using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Planner;
using Routing.Application.Planning.Profiles;

namespace Routing.Application.Planning.Candidates.Generators
{
    public interface ICandidateGenerator<TIntent> where TIntent : ITripIntent
    {
        Task<IReadOnlyList<TripCandidate>> GenerateCandidatesAsync(TIntent intent, UserRoutingProfile profile, PlannerSettings settings, CancellationToken ct);
    }
}
