using Offroad.Core.Exceptions;
using Routing.Domain.ValueObjects;
using System.Collections.Generic;
using System.Linq;

namespace Routing.Application.Planning.Candidates.Builders
{
    public sealed class SegmentBuilder
    {
        public static List<Segment> Build(
            IReadOnlyList<Coordinate> geometry,
            IReadOnlyList<RoadClassInterval> roadClassIntervals,
            IReadOnlyList<SurfaceInterval> surfaceIntervals,
            IReadOnlyList<TrackTypeInterval> trackTypeIntervals)
        {
            var boundaries = CollectBoundaries(roadClassIntervals, surfaceIntervals, trackTypeIntervals);
            return CreateSegments(geometry, boundaries, roadClassIntervals, surfaceIntervals, trackTypeIntervals);
        }

        private static List<int> CollectBoundaries(
            IReadOnlyList<RoadClassInterval> roadClassIntervals,
            IReadOnlyList<SurfaceInterval> surfaceIntervals,
            IReadOnlyList<TrackTypeInterval> trackTypeIntervals)
        {
            return roadClassIntervals
                .SelectMany(s => new[] { s.FromIndex, s.ToIndex })
                .Concat(surfaceIntervals.SelectMany(s => new[] { s.FromIndex, s.ToIndex }))
                .Concat(trackTypeIntervals.SelectMany(t => new[] { t.FromIndex, t.ToIndex }))
                .Distinct()
                .OrderBy(i => i)
                .ToList();
        }

        private static List<Segment> CreateSegments(
            IReadOnlyList<Coordinate> geometry,
            List<int> boundaries,
            IReadOnlyList<RoadClassInterval> roadClassIntervals,
            IReadOnlyList<SurfaceInterval> surfaceIntervals,
            IReadOnlyList<TrackTypeInterval> trackTypeIntervals)
        {
            var segments = new List<Segment>(boundaries.Count - 1);

            for (int i = 0; i < boundaries.Count - 1; i++)
            {
                int fromIndex = boundaries[i];
                int toIndex = boundaries[i + 1];

                var segment = CreateSegment(
                    geometry,
                    fromIndex,
                    toIndex,
                    roadClassIntervals,
                    surfaceIntervals,
                    trackTypeIntervals);

                segments.Add(segment);
            }
            return segments;
        }

        private static Segment CreateSegment(
            IReadOnlyList<Coordinate> geometry,
            int fromIndex,
            int toIndex,
            IReadOnlyList<RoadClassInterval> roadClassIntervals,
            IReadOnlyList<SurfaceInterval> surfaceIntervals,
            IReadOnlyList<TrackTypeInterval> trackTypeIntervals)
        {
            var road = roadClassIntervals
                .SingleOrDefault(r => r.FromIndex <= fromIndex && fromIndex < r.ToIndex)
                ?? throw new ContractViolationException(
                    $"RoadClassIntervals must fully cover geometry. No interval found for index {fromIndex}.");

            var surface = surfaceIntervals
                .SingleOrDefault(s => s.FromIndex <= fromIndex && fromIndex < s.ToIndex)
                ?? throw new ContractViolationException(
                    $"SurfaceIntervals must fully cover geometry. No interval found for index {fromIndex}.");

            var track = trackTypeIntervals
                .SingleOrDefault(t => t.FromIndex <= fromIndex && fromIndex < t.ToIndex)
                ?? throw new ContractViolationException(
                    $"TrackTypeIntervals must fully cover geometry. No interval found for index {fromIndex}.");

            var intervalGeometry = new List<Coordinate>((toIndex - fromIndex) + 1);
            for (int i = fromIndex; i <= toIndex; i++)
            {
                intervalGeometry.Add(geometry[i]);
            }

            return Segment.Create(
                start: intervalGeometry[0],
                end: intervalGeometry[^1],
                geometry: intervalGeometry,
                roadClassType: road.RoadClass,
                surfaceType: surface.Surface,
                trackType: track.TrackType
            );
        }
    }
}