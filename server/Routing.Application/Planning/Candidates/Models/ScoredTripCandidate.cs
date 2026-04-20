namespace Routing.Application.Planning.Candidates.Models
{
    public sealed record ScoredTripCandidate<TCandidate>(TCandidate Candidate, double Score)
           where TCandidate : TripCandidate;
}
