using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Planner;
using Routing.Application.Planning.Profiles;
using Routing.Domain.Enums;
using Routing.Domain.ValueObjects;

namespace Routing.Application.Planning.Candidates.Scoring
{
    public abstract class BaseTripCandidateScorer<TIntent> : ITripCandidateScorer<TIntent> where TIntent : ITripIntent
    {
        protected abstract PenaltyWeights Weights { get; }

        public IReadOnlyList<ScoredTripCandidate> Score(IReadOnlyList<TripCandidate> candidates, TIntent intent, UserRoutingProfile profile, PlannerSettings settings)
        {
            if (candidates.Count == 0)
                return [];

            var weights = Weights;

            return candidates.Select(candidate =>
            {
                var score = ScoreCandidate(candidate, intent, candidates, weights);
                score -= CalculateHardPenalties(candidate, weights);

                return new ScoredTripCandidate
                {
                    Candidate = candidate,
                    Score = score
                };
            }).ToList();
        }

        protected abstract double ScoreCandidate(TripCandidate candidate, TIntent intent, IReadOnlyList<TripCandidate> allCandidates, PenaltyWeights weights);

        private static double CalculateHardPenalties(TripCandidate candidate, PenaltyWeights weights)
        {
            double penalty = 0;

            foreach (var zone in candidate.RestrictedZones)
                penalty += GetRestrictionPenalty(zone.Value, weights.Restrictions);

            foreach (var barrier in candidate.Barriers)
                penalty += GetBarrierPenalty(barrier.Type, weights.Barriers);

            return penalty;
        }

        private static double GetRestrictionPenalty(RestrictionType type, RestrictionWeights weights)
        {
            return type switch
            {
                RestrictionType.NationalPark => weights.NationalPark,
                RestrictionType.NatureReserve => weights.NatureReserve,
                RestrictionType.NoAccess => weights.NoAccess,
                RestrictionType.Private => weights.Private,
                RestrictionType.Forestry => weights.Forestry,
                RestrictionType.Agricultural => weights.Agricultural,
                RestrictionType.Delivery => weights.Delivery,
                RestrictionType.Customers => weights.Customers,
                RestrictionType.Destination => weights.Destination,
                RestrictionType.Unknown => weights.Unknown,
                _ => 0
            };
        }

        private static double GetBarrierPenalty(BarrierType type, BarrierWeights weights)
        {
            return type switch
            {
                BarrierType.Gate => weights.Gate,
                BarrierType.LiftGate => weights.LiftGate,
                BarrierType.SwingGate => weights.SwingGate,
                BarrierType.Chain => weights.Chain,
                BarrierType.Block => weights.Block,
                BarrierType.Bollard => weights.Bollard,
                _ => 0
            };
        }
    }
}
