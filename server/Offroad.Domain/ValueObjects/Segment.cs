using Routing.Domain.Enums;
using Routing.Domain.Exceptions;

namespace Routing.Domain.ValueObjects
{
    public class Segment
    {
        public Coordinate Start { get; }
        public Coordinate End { get; }
        public IReadOnlyList<Coordinate> Geometry { get; }
        public RoadClassType RoadClass { get; }
        public SurfaceType Surface { get; }

        //for ef core in the future
        private Segment() { }

        public Segment(Coordinate start, Coordinate end, IReadOnlyList<Coordinate> geometry, RoadClassType roadClassType, SurfaceType surfaceType)
        {
            Start = start;
            End = end;
            Geometry = geometry;
            RoadClass = roadClassType;
            Surface = surfaceType;
        }

        public static Segment Create(Coordinate start, Coordinate end, IReadOnlyList<Coordinate> geometry, RoadClassType roadClassType, SurfaceType surfaceType)
        {
            Validate(start, end, geometry);
            return new Segment(start, end, geometry, roadClassType, surfaceType);
        }
        private static void Validate(Coordinate start, Coordinate end, IReadOnlyList<Coordinate> geometry)
        {
            if (start is null) throw new DomainException("Segment start cannot be null");
            if (end is null) throw new DomainException("Segment end cannot be null");
            if (geometry.Count == 0)
                throw new DomainException("Segment geometry cannot be empty");
        }
    }
}
