using Routing.Application.Planning.Candidates.Models;
using Routing.Domain.Models;

namespace Routing.Application.Mappings
{
    public static class TripCandidateMappings
    {
        public static TripPlan ToTripPlan(this TripCandidate candidate)
        {
            var totalDistance = candidate.PlanChunks.Sum(c => c.DistanceMeters);
            var offroad = candidate.PlanChunks.Sum(c => c.OffroadDistanceMeters);
            var duration = candidate.PlanChunks.Sum(c => c.DurationSeconds);

            return TripPlan.Create(totalDistance, offroad, duration);
        }
    }
}
