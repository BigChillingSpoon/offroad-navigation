using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using System.Linq;

namespace Routing.Application.Planning.Goals
{
    public class RouteGoal : ITripGoal<RouteIntent>
    {
        /// <summary>
        /// <remarks>
        /// Referential continuity is checked using instance equality of coordinates, 
        /// ensuring that the end of one segment is the exact same memory object 
        /// as the start of the following segment, as provided by the SegmentBuilder.
        /// </remarks>
        /// </summary> 
        public bool IsSatisfied(TripCandidate candidate, RouteIntent intent)
        {
            if (candidate.Segments.Count == 0)
                return false;

            if (candidate.Segments.Any(s => s.DistanceMeters <= 0))
                return false;

            for (int i = 1; i < candidate.Segments.Count; i++)
            {
                var previous = candidate.Segments[i - 1];
                var current = candidate.Segments[i];

                if (previous.End != current.Start)
                    return false;
            }

            return true;
        }

        public double GetGoalScore(TripCandidate candidate, RouteIntent intent)
        {
            return 0d;
        }
    }
}
