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

            // Check for not rounded loops
            for (int i = 1; i < candidate.Segments.Count; i++)
            {
                var previous = candidate.Segments[i - 1];
                var current = candidate.Segments[i];
                
                if (previous.ToIndex != current.FromIndex)
                    return false;
            }

            // The skeleton search already targeted this distance band before the routing provider was
            // involved; re-check it here because the provider's real geometry can land outside it (snapping,
            // road-network detail the skeleton's graph didn't have, etc.).
            var targetDistanceMeters = intent.PreferredLengthKm * 1000;
            var minDistanceMeters = targetDistanceMeters * LoopGenerationSettings.GoalMinFraction;
            var maxDistanceMeters = targetDistanceMeters * LoopGenerationSettings.GoalMaxFraction;

            if (candidate.TotalDistanceMeters < minDistanceMeters || candidate.TotalDistanceMeters > maxDistanceMeters)
                return false;

            return true;
        }
    }
}
