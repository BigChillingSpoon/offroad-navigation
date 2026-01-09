using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Planner;
using Routing.Application.Planning.Profiles;
using Routing.Application.Planning.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Candidates.Generators
{
    public interface ICandidateGenerator<TIntent> where TIntent : ITripIntent
    {
        Task<IReadOnlyList<TripCandidate>> GenerateCandidatesAsync(PlannerState state, TIntent intent, UserRoutingProfile profile, PlannerSettings settings, CancellationToken ct);
    }
}
