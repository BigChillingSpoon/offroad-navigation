using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Mappings;
using Routing.Domain.Models;

namespace Routing.Application.Mappings
{
    public sealed class RouteTripMapper : ITripMapper<TripCandidate>
    {
        public TripPlan MapToPlan(TripCandidate candidate)
        {
            return candidate.ToTripPlan();
        }
    }
}
