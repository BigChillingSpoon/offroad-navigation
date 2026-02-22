using Routing.Application.Planning.Candidates.Models;
using Routing.Domain.Models;

namespace Routing.Application.Mappings
{
    public static class TripCandidateMappings
    {
        public static TripPlan ToTripPlan(this TripCandidate candidate)
        {
            return TripPlan.Create(candidate.TotalDistanceMeters, candidate.OffroadDistanceMeters, candidate.Duration, candidate.ElevationGainMeters, candidate.ElevationLossMeters, candidate.Segments);
        }
    }
}
