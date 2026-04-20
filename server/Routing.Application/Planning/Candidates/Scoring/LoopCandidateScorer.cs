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

            return offroadScore - elevationPenalty;
        }
    }
}