using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Planner;
using Routing.Application.Planning.Profiles;

namespace Routing.Application.Planning.Candidates.Scoring
{
    public sealed class LoopCandidateScorer : ITripCandidateScorer<LoopIntent>
    {
        public IReadOnlyList<ScoredTripCandidate> Score(IReadOnlyList<TripCandidate> candidates, LoopIntent intent, UserRoutingProfile profile, PlannerSettings settings) 
        {
            return candidates.Select(candidate =>
            {
                

                //preference scoring (placeholder):
                // - prefer offroad ratio
                // - penalize extreme elevation gain
                var score =
                    (candidate.OffroadRatio * 100.0) -
                    (candidate.ElevationGainMeters * 0.01);

                return new ScoredTripCandidate
                {
                    Candidate = candidate,
                    Score = score
                };
            }).ToList();
        }
    }
}
