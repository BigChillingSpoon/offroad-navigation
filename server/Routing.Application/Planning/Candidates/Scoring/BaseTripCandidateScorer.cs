using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Profiles;

namespace Routing.Application.Planning.Candidates.Scoring
{
    public abstract class BaseTripCandidateScorer<TIntent, TCandidate> : ITripCandidateScorer<TIntent, TCandidate>
        where TIntent : ITripIntent
        where TCandidate : TripCandidate
    {
        protected abstract PenaltyWeights Weights { get; }

        public IReadOnlyList<ScoredTripCandidate<TCandidate>> Score(IReadOnlyList<TCandidate> candidates, TIntent intent, UserRoutingProfile profile)
        {
            var scoredCandidates = new List<ScoredTripCandidate<TCandidate>>();

            foreach (var candidate in candidates)
            {
                var score = ScoreCandidate(candidate, intent, candidates, Weights);
                scoredCandidates.Add(new ScoredTripCandidate<TCandidate>(candidate, score));
            }

            return scoredCandidates;
        }

        protected abstract double ScoreCandidate(TCandidate candidate, TIntent intent, IReadOnlyList<TCandidate> allCandidates, PenaltyWeights weights);
    }
}