using Microsoft.Extensions.Options;
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Domain.Enums;

namespace Routing.Application.Planning.Candidates.Scoring
{
    public sealed class RouteCandidateScorer : BaseTripCandidateScorer<RouteIntent, TripCandidate>
    {
        private readonly IOptionsMonitor<ScoringProfiles> _options;

        public RouteCandidateScorer(IOptionsMonitor<ScoringProfiles> options)
        {
            _options = options;
        }

        protected override PenaltyWeights Weights => _options.CurrentValue.Route;

        protected override double ScoreCandidate(TripCandidate candidate, RouteIntent intent, IReadOnlyList<TripCandidate> allCandidates, PenaltyWeights weights)
        {
            var shortestDistance = allCandidates.Min(c => c.TotalDistanceMeters);

            return intent.Balance switch
            {
                RouteBalance.Shortest => ScoreShortest(candidate, shortestDistance),
                RouteBalance.MaxOffroad => ScoreMaxOffroad(candidate),
                RouteBalance.Balanced => ScoreBalanced(candidate, shortestDistance, weights),
                _ => 0.0
            };
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

        private static double ScoreBalanced(TripCandidate candidate, double shortestDistance, PenaltyWeights weights)
        {
            if (shortestDistance <= 0) return 0;

            var detourRatio = (candidate.TotalDistanceMeters - shortestDistance) / shortestDistance;
            var offroadScore = candidate.OffroadRatio * 100.0;

            var detourPenalty = detourRatio <= weights.Detour.MaxRatio
                ? detourRatio * weights.Detour.StandardPenaltyRate
                : weights.Detour.ExcessiveBasePenalty + (detourRatio - weights.Detour.MaxRatio) * weights.Detour.ExcessiveRate;

            return offroadScore - detourPenalty;
        }
    }
}