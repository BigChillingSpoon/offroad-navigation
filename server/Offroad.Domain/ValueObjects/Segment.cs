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

        public Segment(Coordinate start, Coordinate end, double distance, double duration, double offroadDistance, double elevationGain , IReadOnlyList<Coordinate> geometry)
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
            if (distance < 0) throw new ArgumentException("Distance cannot be negative");
            return new Segment(start, end, distance, duration, offroadDistance, elevationGain, geometry);
        }
    }
}
