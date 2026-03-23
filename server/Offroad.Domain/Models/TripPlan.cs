using Routing.Domain.Enums;
using Routing.Domain.Exceptions;
using Routing.Domain.ValueObjects;
namespace Routing.Domain.Models
{
    public class TripPlan
    {
        public double TotalDistanceMeters { get; }
        public double OffroadDistanceMeters { get; }
        public TimeSpan Duration { get; }
        public double ElevationGainMeters { get; }
        public double ElevationLossMeters { get; }
        public EncodedPolyline Polyline { get; }
        public IReadOnlyList<Segment> Segments { get; }
        public IReadOnlyList<RoadBarrier> Barriers { get; }
        public IReadOnlyList<Interval<RestrictionType>> RestrictedZones { get; }

        //for EFCore
        private TripPlan() { }

        private TripPlan(double distance, double offroadDistance, TimeSpan duration, double elevationGain, double elevationLoss, EncodedPolyline polyline = null, IReadOnlyList<Segment> segments = null, IReadOnlyList<RoadBarrier> barriers = null, IReadOnlyList<Interval<RestrictionType>> restrictedZones = null)
        {
            TotalDistanceMeters = distance;
            Duration = duration;
            OffroadDistanceMeters = offroadDistance;
            Polyline = polyline ?? new EncodedPolyline();
            Segments = segments ?? new List<Segment>();
            ElevationGainMeters = elevationGain;
            ElevationLossMeters = elevationLoss;
            Barriers = barriers ?? new List<RoadBarrier>();
            RestrictedZones = restrictedZones ?? new List<Interval<RestrictionType>>();
        }

        public static TripPlan Create(double totalDistance, double offroadDistance, TimeSpan duration, double elevationGain, double elevationLoss, EncodedPolyline polyline = null, IReadOnlyList<Segment> segments = null, IReadOnlyList<RoadBarrier> barriers = null, IReadOnlyList<Interval<RestrictionType>> restrictedZones = null)
        {
            Validate(totalDistance, offroadDistance, duration, elevationGain, elevationLoss);
            return new TripPlan(totalDistance, offroadDistance, duration, elevationGain, elevationLoss, polyline, segments, barriers, restrictedZones);
        }

        private static void Validate(double totalDistance, double offroadDistance, TimeSpan duration, double elevationGain, double elevationLoss)
        {
            if (offroadDistance < 0)
                throw new DomainException("Offroad distance cannot be negative");
            if (elevationGain < 0)
                throw new DomainException("Elevation gain cannot be negative");
            if (elevationLoss < 0)
                throw new DomainException("Elevation loss cannot be negative");
            if (totalDistance < 0)
                throw new DomainException("Total distance cannot be negative.");
        }
    }
}
