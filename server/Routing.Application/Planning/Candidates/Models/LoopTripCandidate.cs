using Routing.Domain.Enums;
using Routing.Domain.ValueObjects;

namespace Routing.Application.Planning.Candidates.Models
{
    public sealed class LoopTripCandidate : TripCandidate
    {
        public Coordinate HookCoordinate { get; }
        public int HookPolylineIndex { get; }
        public double EstimatedTransitDistanceMeters { get; }

        /// <summary>
        /// The entrance this candidate was generated from. Several candidates can share the same
        /// entrance, since one arena can yield multiple distinct non-overlapping loops.
        /// </summary>
        public Coordinate EntranceCoordinate { get; }

        private LoopTripCandidate(
            IReadOnlyList<Segment> segments,
            IReadOnlyList<RoadBarrier> barriers,
            IReadOnlyList<Interval<RestrictionType>> restrictedZones,
            EncodedPolyline polyline,
            double totalDistance,
            TimeSpan duration,
            double offroadDistance,
            double elevationGain,
            double elevationLoss,
            double maxGradientPercentage,
            Coordinate hookCoordinate,
            int hookPolylineIndex,
            double estimatedTransitDistanceMeters,
            Coordinate entranceCoordinate)
            : base(segments, barriers, restrictedZones, polyline, totalDistance, duration, offroadDistance, elevationGain, elevationLoss, maxGradientPercentage)
        {
            HookCoordinate = hookCoordinate;
            HookPolylineIndex = hookPolylineIndex;
            EstimatedTransitDistanceMeters = estimatedTransitDistanceMeters;
            EntranceCoordinate = entranceCoordinate;
        }

        public static LoopTripCandidate Create(
            IReadOnlyList<Segment> segments,
            IReadOnlyList<RoadBarrier> barriers,
            IReadOnlyList<Interval<RestrictionType>> restrictedZones,
            EncodedPolyline polyline,
            double totalDistance,
            TimeSpan duration,
            double elevationGain,
            double elevationLoss,
            double maxGradientPercentage,
            Coordinate hookCoordinate,
            int hookPolylineIndex,
            double estimatedTransitDistanceMeters,
            Coordinate entranceCoordinate)
        {
            var offroadDistance = segments.Where(s => s.IsOffroad).Sum(s => s.DistanceMeters);

            Validate(totalDistance, duration, offroadDistance, elevationGain, elevationLoss);

            return new LoopTripCandidate(
                segments, barriers, restrictedZones, polyline,
                totalDistance, duration, offroadDistance, elevationGain, elevationLoss, maxGradientPercentage,
                hookCoordinate, hookPolylineIndex, estimatedTransitDistanceMeters, entranceCoordinate);
        }
    }
}