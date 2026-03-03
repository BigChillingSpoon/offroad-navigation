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
        public TrackType TrackType { get; }
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
                // 1. Explicit unpaved surface
                // If the mapper explicitly defined the material as dirt, gravel, sand, etc., 
                // it is a guaranteed offroad segment.
                if (IsUnpavedSurface)
                    return true;

                // 2. Degraded or rough track types (Grade 2-5)
                // Handles OSM data inconsistencies and edge cases. For example:
                // - A road tagged as 'service' but heavily degraded (Grade 4).
                // - An old, broken asphalt road where surface is 'asphalt' but tracktype is 'grade4'.
                // If it's rough enough, we treat it as offroad regardless of the road class or theoretical surface.
                if (TrackType is TrackType.GRADE2 or TrackType.GRADE3 or TrackType.GRADE4 or TrackType.GRADE5)
                    return true;

                // 3. Fallback for incomplete OSM data (The "Lazy Mapper" scenario)
                // Millions of forest/field paths in OSM only have the 'highway=track' tag 
                // with missing surface and tracktype data.
                // If it is a track, and we lack explicit proof that it is paved or perfectly solid (Grade 1),
                // we safely assume it is an offroad path.
                if (RoadClass == RoadClassType.TRACK && !IsPavedSurface && TrackType != TrackType.GRADE1)
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

        private Segment(Coordinate start, Coordinate end, IReadOnlyList<Coordinate> geometry, RoadClassType roadClassType, SurfaceType surfaceType, TrackType trackType, double distanceMeters)
        {
            Start = start;
            End = end;
            Geometry = geometry;
            RoadClass = roadClassType;
            Surface = surfaceType;
            TrackType = trackType;
            DistanceMeters = distanceMeters;
        }

        public static Segment Create(Coordinate start, Coordinate end, IReadOnlyList<Coordinate> geometry, RoadClassType roadClassType, TrackType trackType, SurfaceType surfaceType)
        {
            Validate(start, end, geometry);
            var distanceMeters = GeoCalculator.CalculatePathDistance(geometry);
            return new Segment(start, end, geometry, roadClassType, surfaceType, trackType, distanceMeters);
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
