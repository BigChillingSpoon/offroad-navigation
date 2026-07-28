using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;

namespace Routing.Application.Planning.Goals
{
    public sealed class LoopGoal : ITripGoal<LoopIntent, LoopTripCandidate>
    {
        public bool IsSatisfied(LoopTripCandidate candidate, LoopIntent intent)
        {
            if (candidate.Segments.Count == 0)
                return false;

            if (candidate.Segments.Any(s => s.DistanceMeters <= 0))
                return false;

            for (int i = 1; i < candidate.Segments.Count; i++)
            {
                var previous = candidate.Segments[i - 1];
                var current = candidate.Segments[i];

                if (previous.ToIndex != current.FromIndex)
                    return false;
            }

            // The skeleton search already targeted this distance band before GraphHopper was involved;
            // re-check it here because GraphHopper's real geometry can land outside it (different snapping,
            // road-network detail the skeleton's graph didn't have, etc.).
            var targetDistanceMeters = intent.PreferredLengthKm * 1000;
            var minDistanceMeters = targetDistanceMeters * LoopDistanceTolerance.MinFraction;
            var maxDistanceMeters = targetDistanceMeters * LoopDistanceTolerance.MaxFraction;

            if (candidate.TotalDistanceMeters < minDistanceMeters || candidate.TotalDistanceMeters > maxDistanceMeters)
                return false;

            return true;
        }

        public double GetGoalScore(LoopTripCandidate candidate, LoopIntent intent)
        {
            return 0d;
        }
    }
}
