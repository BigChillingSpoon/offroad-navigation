using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Planner;
using Routing.Application.Planning.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Candidates.Scoring
{
    public interface ITripCandidateScorer
    {
        IReadOnlyList<ScoredTripCandidate> Score<TIntent>(IReadOnlyList<TripCandidate> candidates, TIntent intent, UserRoutingProfile profile, PlannerSettings settings) where TIntent : ITripIntent;
    }
}
