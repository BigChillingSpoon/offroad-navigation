using Routing.Application.Planning.Candidates.Models;
using Routing.Domain.Models;

namespace Routing.Application.Mappings
{
    public static class TripCandidateMappings
    {
        public static TripPlan ToTripPlan(this TripCandidate candidate)
        {
            var totalDistance = candidate.Segments.Sum(c => c.DistanceMeters);
            var offroadDistance = candidate.Segments.Sum(c => c.OffroadDistanceMeters);
            var duration = candidate.Segments.Sum(c => c.Duration.Ticks);
            var elevationGain = candidate.Segments.Sum(c => c.ElevationGainMeters);

            return TripPlan.Create(totalDistance, offroadDistance, TimeSpan.FromTicks(duration), elevationGain, candidate.Segments);
        }
    }
}
