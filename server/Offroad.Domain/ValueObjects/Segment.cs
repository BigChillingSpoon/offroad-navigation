namespace Routing.Domain.ValueObjects
{
    public class Segment
    {
        public Coordinate Start { get; }
        public Coordinate End { get; }
        public double DistanceMeters { get; }
        public double DurationSeconds { get; }
        public IReadOnlyList<Coordinate> Geometry { get; }

        private Segment() { }

        public Segment(Coordinate start, Coordinate end, double distance, double duration, List<Coordinate> geometry)
        {
            Start = start;
            End = end;
            DistanceMeters = distance;
            DurationSeconds = duration;
            Geometry = geometry;
        }

        public static Segment Create(Coordinate start, Coordinate end, double distance, double duration, List<Coordinate> geometry)
        {
            if (distance < 0) throw new ArgumentException("Distance cannot be negative");
            return new Segment(start, end, distance, duration, geometry);
        }
    }
}
