using Routing.Application.Planning.Candidates.Models;
using Routing.Domain.Models;

namespace Routing.Application.Planning.Mappings
{
    public interface ITripMapper<in TCandidate> where TCandidate : TripCandidate
    {
        TripPlan MapToPlan(TCandidate candidate);
    }
}