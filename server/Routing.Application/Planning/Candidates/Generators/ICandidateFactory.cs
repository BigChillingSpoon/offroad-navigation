using Routing.Application.Planning.Candidates.Models;
using Routing.Domain.ValueObjects;

namespace Routing.Application.Planning.Candidates.Generators
{
    public interface ICandidateFactory
    {
        Task<TripCandidate> CreateRouteCandidateAsync(ProviderRoute route);
        Task<LoopTripCandidate> CreateLoopCandidateAsync(ProviderRoute route, Coordinate userLocation);
    }
}
