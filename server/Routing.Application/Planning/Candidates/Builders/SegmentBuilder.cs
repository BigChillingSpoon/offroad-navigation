using Offroad.Core.Exceptions;
using Routing.Domain.ValueObjects;

namespace Routing.Application.Planning.Candidates.Builders
{
    public sealed class SegmentBuilder
    {
        public static List<Segment> Build(
            IReadOnlyList<Coordinate> geometry,
            IReadOnlyList<RoadClassInterval> roadClassIntervals,
            IReadOnlyList<SurfaceInterval> surfaceIntervals)
        {
            var boundaries = CollectBoundaries(roadClassIntervals, surfaceIntervals);
            return CreateSegments(geometry, boundaries, roadClassIntervals, surfaceIntervals);
        }

        private static List<int> CollectBoundaries(IReadOnlyList<RoadClassInterval> roadClassIntervals, IReadOnlyList<SurfaceInterval> surfaceIntervals)
        {
            return roadClassIntervals
                .SelectMany(s => new[] { s.FromIndex, s.ToIndex })
                .Concat(
                    surfaceIntervals.SelectMany(s => new[] { s.FromIndex, s.ToIndex }))
                .Distinct()
                .OrderBy(i => i)
                .ToList();
        }

        private static List<Segment> CreateSegments(
            IReadOnlyList<Coordinate> geometry,
            List<int> boundaries,
            IReadOnlyList<RoadClassInterval> roadClassIntervals,
            IReadOnlyList<SurfaceInterval> surfaceIntervals)
        {
            var segments = new List<Segment>();
            for (int i = 0; i < boundaries.Count - 1; i++)
            {
                int fromIndex = boundaries[i];
                int toIndex = boundaries[i + 1];
                var segment = CreateSegment(
                    geometry,
                    fromIndex,
                    toIndex,
                    roadClassIntervals,
                    surfaceIntervals);
                segments.Add(segment);
            }
            return segments;
        }

        private static Segment CreateSegment(
            IReadOnlyList<Coordinate> geometry,
            int fromIndex,
            int toIndex,
            IReadOnlyList<RoadClassInterval> roadClassIntervals,
            IReadOnlyList<SurfaceInterval> surfaceIntervals)
        {
            var road = roadClassIntervals
                .SingleOrDefault(r => r.FromIndex <= fromIndex && fromIndex < r.ToIndex)
                ?? throw new ContractViolationException(
                    $"RoadClassIntervals must fully cover geometry. No interval found for index {fromIndex}.");

            var surface = surfaceIntervals
                .SingleOrDefault(s => s.FromIndex <= fromIndex && fromIndex < s.ToIndex)
                ?? throw new ContractViolationException(
                    $"SurfaceIntervals must fully cover geometry. No interval found for index {fromIndex}.");

            var intervalGeometry = geometry
                .Skip(fromIndex)
                .Take((toIndex - fromIndex) + 1)
                .ToList();

            return Segment.Create(
                start: intervalGeometry[0],
                end: intervalGeometry[intervalGeometry.Count - 1],
                geometry: intervalGeometry,
                roadClassType: road.RoadClass,
                surfaceType: surface.Surface
            );
        }
    }
}
