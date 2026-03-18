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
        public IReadOnlyList<Segment> Segments { get; }
        public IReadOnlyList<RoadBarrier> Barriers { get; }
        //for EFCore
        private TripPlan() { }

        private TripPlan(double distance, double offroadDistance, TimeSpan duration, double elevationGain, double elevationLoss, IReadOnlyList<Segment> segments = null, IReadOnlyList<RoadBarrier> barriers = null)
        {
            TotalDistanceMeters = distance;
            Duration = duration;
            OffroadDistanceMeters = offroadDistance;
            Segments = segments ?? new List<Segment>();
            ElevationGainMeters = elevationGain;
            ElevationLossMeters = elevationLoss;
            Barriers = barriers ?? new List<RoadBarrier>();
        }

        public static TripPlan Create(double totalDistance, double offroadDistance, TimeSpan duration, double elevationGain, double elevationLoss, IReadOnlyList<Segment> segments = null, IReadOnlyList<RoadBarrier> barriers = null)
        {
            Validate(totalDistance, offroadDistance, duration, elevationGain, elevationLoss);
            return new TripPlan(totalDistance, offroadDistance, duration, elevationGain, elevationLoss, segments, barriers);
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
