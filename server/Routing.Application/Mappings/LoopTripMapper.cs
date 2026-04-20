using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Mappings;
using Routing.Domain.Models;

namespace Routing.Application.Mappings
{
    public sealed class LoopTripMapper : ITripMapper<LoopTripCandidate>
    {
        public TripPlan MapToPlan(LoopTripCandidate candidate)
        {
            return candidate.ToTripPlan();
        }
    }
}
