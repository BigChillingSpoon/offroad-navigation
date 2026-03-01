using Routing.Domain.Enums;
using Routing.Domain.Exceptions;
using Routing.Domain.Utilities;

namespace Routing.Domain.ValueObjects
{
    public class Segment
    {
        public Coordinate Start { get; }
        public Coordinate End { get; }
        public IReadOnlyList<Coordinate> Geometry { get; }
        public RoadClassType RoadClass { get; }
        public SurfaceType Surface { get; }
        public double DistanceMeters { get; }

        /// <summary>
        /// Determines if this segment is offroad based on surface and road class.
        /// Unpaved surfaces are always offroad.
        /// Track roads are offroad unless the surface is explicitly paved.
        /// </summary>
        public bool IsOffroad
        {
            get
            {
                if (IsUnpavedSurface)
                    return true;

                // Track roads are typically offroad unless surface is explicitly paved
                // This is not the same as previous condition, in case of unknown surface we can still say that this is an offroad
                if (RoadClass == RoadClassType.TRACK && !IsPavedSurface)
                    return true;

                return false;
            }
        }

        private bool IsUnpavedSurface => Surface is
            SurfaceType.UNPAVED or
            SurfaceType.GRAVEL or
            SurfaceType.FINE_GRAVEL or
            SurfaceType.COMPACTED or
            SurfaceType.DIRT or
            SurfaceType.GROUND or
            SurfaceType.SAND or
            SurfaceType.GRASS or
            SurfaceType.WOOD;

        private bool IsPavedSurface => Surface is
            SurfaceType.PAVED or
            SurfaceType.ASPHALT or
            SurfaceType.CONCRETE or
            SurfaceType.PAVING_STONES or
            SurfaceType.COBBLESTONE;

        //for ef core in the future
        private Segment() { }

        private Segment(Coordinate start, Coordinate end, IReadOnlyList<Coordinate> geometry, RoadClassType roadClassType, SurfaceType surfaceType, double distanceMeters)
        {
            Start = start;
            End = end;
            Geometry = geometry;
            RoadClass = roadClassType;
            Surface = surfaceType;
            DistanceMeters = distanceMeters;
        }

        public static Segment Create(Coordinate start, Coordinate end, IReadOnlyList<Coordinate> geometry, RoadClassType roadClassType, SurfaceType surfaceType)
        {
            Validate(start, end, geometry);
            var distanceMeters = GeoCalculator.CalculatePathDistance(geometry);
            return new Segment(start, end, geometry, roadClassType, surfaceType, distanceMeters);
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
