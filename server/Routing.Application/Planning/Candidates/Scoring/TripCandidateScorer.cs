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
                
                var offroadRatio = candidate.OffroadDistanceMeters == 0 ? 0 : candidate.OffroadDistanceMeters / candidate.TotalDistanceMeters;

                //preference scoring (placeholder):
                // - prefer offroad ratio
                // - penalize extreme elevation gain
                var score =
                    (offroadRatio * 100.0) -
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
