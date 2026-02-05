using Routing.Domain.Exceptions;

namespace Routing.Domain.ValueObjects
{
    public class Segment
    {
        public Coordinate Start { get; }
        public Coordinate End { get; }
        public double DistanceMeters { get; }
        public double DurationSeconds { get; }
        public double OffroadDistanceMeters { get; }
        public double ElevationGainMeters { get; }

        public IReadOnlyList<Coordinate> Geometry { get; }

        //to ef core in the future
        private Segment() { }

        public Segment(Coordinate start, Coordinate end, double distance, double duration, double offroadDistance, double elevationGain, IReadOnlyList<Coordinate> geometry)
        {
            Start = start;
            End = end;
            DistanceMeters = distance;
            DurationSeconds = duration;
            OffroadDistanceMeters = offroadDistance;
            Geometry = geometry;
            ElevationGainMeters = elevationGain;
        }

        public static Segment Create(Coordinate start, Coordinate end, double distance, double duration, double offroadDistance, double elevationGain, IReadOnlyList<Coordinate> geometry)
        {
            Validate(start, end, distance, duration, offroadDistance, elevationGain, geometry);
            return new Segment(start, end, distance, duration, offroadDistance, elevationGain, geometry);
        }
        private static void Validate(Coordinate start, Coordinate end, double distance, double duration, double offroadDistance, double elevationGain, IReadOnlyList<Coordinate> geometry)
        {
            if (start is null) throw new DomainException("Segment start cannot be null");
            if (end is null) throw new DomainException("Segment end cannot be null");
            if (geometry is null || geometry.Count == 0)
                throw new DomainException("Segment geometry cannot be empty");

            if (distance < 0) throw new DomainException("Distance cannot be negative");
            if (duration < 0) throw new DomainException("Duration cannot be negative");
            if (offroadDistance < 0) throw new DomainException("Offroad distance cannot be negative");
            if (elevationGain < 0) throw new DomainException("Elevation gain cannot be negative");
        }
    }
}
