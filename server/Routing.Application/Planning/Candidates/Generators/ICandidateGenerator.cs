using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;

namespace Routing.Application.Planning.Candidates.Generators
{
    public interface ICandidateGenerator<in TIntent, TCandidate>
        where TIntent : ITripIntent
        where TCandidate : TripCandidate
    {
        Task<IReadOnlyList<TCandidate>> GenerateCandidatesAsync(TIntent intent, CancellationToken ct);
    }
}
