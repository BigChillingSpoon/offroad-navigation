using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Planner;
using Routing.Application.Planning.Profiles;
using Routing.Domain.Enums;

namespace Routing.Application.Planning.Candidates.Scoring
{
    public sealed class RouteCandidateScorer : ITripCandidateScorer<RouteIntent>
    {
        private const double MaxDetourRatio = 0.3;

        public IReadOnlyList<ScoredTripCandidate> Score(IReadOnlyList<TripCandidate> candidates, RouteIntent intent, UserRoutingProfile profile, PlannerSettings settings)
        {
            if (candidates.Count == 0)
                return [];

            var shortestDistance = candidates.Min(c => c.TotalDistanceMeters);

            return candidates.Select(candidate =>
            {
                var score = intent.Balance switch
                {
                    RouteBalance.Shortest => ScoreShortest(candidate, shortestDistance),
                    RouteBalance.MaxOffroad => ScoreMaxOffroad(candidate),
                    RouteBalance.Balanced => ScoreBalanced(candidate, shortestDistance),
                    _ => 0.0
                };

                return new ScoredTripCandidate
                {
                    Candidate = candidate,
                    Score = score
                };
            }).ToList();
        }

        private static double ScoreShortest(TripCandidate candidate, double shortestDistance)
        {
            if (shortestDistance <= 0) return 0;
            return (shortestDistance / candidate.TotalDistanceMeters) * 100.0;
        }

        private static double ScoreMaxOffroad(TripCandidate candidate)
        {
            return candidate.OffroadRatio * 100.0;
        }

        private static double ScoreBalanced(TripCandidate candidate, double shortestDistance)
        {
            if (shortestDistance <= 0) return 0;

            var detourRatio = (candidate.TotalDistanceMeters - shortestDistance) / shortestDistance;
            var offroadScore = candidate.OffroadRatio * 100.0;

            var detourPenalty = detourRatio <= MaxDetourRatio
                ? detourRatio * 50.0
                : 15.0 + (detourRatio - MaxDetourRatio) * 200.0;

            return offroadScore - detourPenalty;
        }
    }
}
