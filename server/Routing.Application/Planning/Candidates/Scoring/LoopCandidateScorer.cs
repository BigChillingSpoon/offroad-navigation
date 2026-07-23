using Microsoft.Extensions.Options;
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;

namespace Routing.Application.Planning.Candidates.Scoring
{
    public sealed class LoopCandidateScorer : BaseTripCandidateScorer<LoopIntent, LoopTripCandidate>
    {
        private readonly IOptionsMonitor<ScoringProfiles> _options;

        public LoopCandidateScorer(IOptionsMonitor<ScoringProfiles> options)
        {
            _options = options;
        }

        protected override PenaltyWeights Weights => _options.CurrentValue.Loop;

        protected override double ScoreCandidate(LoopTripCandidate candidate, LoopIntent intent, IReadOnlyList<LoopTripCandidate> allCandidates, PenaltyWeights weights)
        {
            var offroadScore = candidate.OffroadRatio * 100.0;
            var elevationPenalty = candidate.ElevationGainMeters * 0.01;
            var distancePenalty = ScoreDistanceDeviation(candidate, intent, weights);

            return offroadScore - elevationPenalty - distancePenalty;
        }

        // Mirrors RouteCandidateScorer.ScoreBalanced's detour penalty, but "detour" here means
        // deviation from the requested loop length rather than deviation from the shortest path -
        // a loop has no shortest-path baseline to detour from.
        private static double ScoreDistanceDeviation(LoopTripCandidate candidate, LoopIntent intent, PenaltyWeights weights)
        {
            var targetDistanceMeters = intent.PreferredLengthKm * 1000;
            if (targetDistanceMeters <= 0) return 0;

            var deviationRatio = Math.Abs(candidate.TotalDistanceMeters - targetDistanceMeters) / targetDistanceMeters;

            return deviationRatio <= weights.Detour.MaxRatio
                ? deviationRatio * weights.Detour.StandardPenaltyRate
                : weights.Detour.ExcessiveBasePenalty + (deviationRatio - weights.Detour.MaxRatio) * weights.Detour.ExcessiveRate;
        }
    }
}
