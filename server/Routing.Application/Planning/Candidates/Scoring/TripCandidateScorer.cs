using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Planner;
using Routing.Application.Planning.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Candidates.Scoring
{
    public sealed class TripCandidateScorer : ITripCandidateScorer //todo divide into scorer for loops and scorer for routes
    {
        public IReadOnlyList<ScoredTripCandidate> Score<TIntent>(IReadOnlyList<TripCandidate> candidates, TIntent intent, UserRoutingProfile profile, PlannerSettings settings) where TIntent : ITripIntent
        {
            return candidates.Select(candidate =>
            {
                var totalDistance = candidate.Segments.Sum(c => c.DistanceMeters);
                var offroadDistance = candidate.Segments.Sum(c => c.OffroadDistanceMeters);
                var elevationGain = candidate.Segments.Sum(c => c.ElevationGainMeters);

                var offroadRatio = totalDistance <= 0 ? 0 : offroadDistance / totalDistance;

                //preference scoring (placeholder):
                // - prefer offroad ratio
                // - penalize extreme elevation gain
                var score =
                    (offroadRatio * 100.0) -
                    (elevationGain * 0.01);

                return new ScoredTripCandidate
                {
                    Candidate = candidate,
                    Score = score
                };
            }).ToList();
        }
    }
}
