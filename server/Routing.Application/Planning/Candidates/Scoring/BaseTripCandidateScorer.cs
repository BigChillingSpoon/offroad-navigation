using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Planner;
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
            var scoredList = new List<ScoredTripCandidate<TCandidate>>(candidates.Count);

            foreach (var candidate in candidates)
            {
                var score = ScoreCandidate(candidate, intent, candidates, Weights);
                scoredList.Add(
                    new ScoredTripCandidate<TCandidate>() {
                        Candidate = candidate,
                        Score = score
                    }
                );
            }

            return scoredList;
        }

        protected abstract double ScoreCandidate(TCandidate candidate, TIntent intent, IReadOnlyList<TCandidate> allCandidates, PenaltyWeights weights);
    }
}