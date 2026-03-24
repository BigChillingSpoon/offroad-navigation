using Routing.Domain.Enums;
using Routing.Domain.Exceptions;
using Routing.Domain.Utilities;

namespace Routing.Domain.ValueObjects
{
    public class Segment
    {
        public int FromIndex { get; }
        public int ToIndex { get; }
        public Coordinate Start { get; }
        public Coordinate End { get; }
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

        private Segment(int fromIndex, int toIndex, Coordinate start, Coordinate end, RoadClassType roadClassType, SurfaceType surfaceType, TrackType trackType, double distanceMeters)
        {
            FromIndex = fromIndex;
            ToIndex = toIndex;
            Start = start;
            End = end;
            RoadClass = roadClassType;
            Surface = surfaceType;
            TrackType = trackType;
            DistanceMeters = distanceMeters;
        }

        public static Segment Create(IReadOnlyList<Coordinate> fullGeometry, int fromIndex, int toIndex, RoadClassType roadClassType, SurfaceType surfaceType, TrackType trackType)
        {
            if (fullGeometry is null || fullGeometry.Count == 0)
                throw new DomainException("Geometry cannot be null or empty");
            if (fromIndex < 0 || toIndex < fromIndex || toIndex >= fullGeometry.Count)
                throw new DomainException($"Invalid segment indices: [{fromIndex}, {toIndex}] for geometry of size {fullGeometry.Count}");

            var distanceMeters = GeoCalculator.CalculateRangeDistance(fullGeometry, fromIndex, toIndex);

            return new Segment(fromIndex, toIndex, fullGeometry[fromIndex], fullGeometry[toIndex], roadClassType, surfaceType, trackType, distanceMeters);
        }
    }
}
