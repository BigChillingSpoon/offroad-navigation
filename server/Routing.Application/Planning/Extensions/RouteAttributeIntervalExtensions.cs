using Routing.Domain.Enums;
using Routing.Domain.ValueObjects;

namespace Routing.Application.Planning.Extensions
{
    internal static class RouteAttributeIntervalExtensions
    {
        public static IReadOnlyList<SurfaceInterval> EnsureFullCoverage(this IReadOnlyList<SurfaceInterval> source, int maxEdgeIndex)
        {
            return source.FillMissingIntervals(
                maxEdgeIndex,
                (from, to) => new SurfaceInterval
                {
                    FromIndex = from,
                    ToIndex = to,
                    Surface = SurfaceType.Unknown
                });
        }

        public static IReadOnlyList<RoadClassInterval> EnsureFullCoverage(this IReadOnlyList<RoadClassInterval> source, int maxEdgeIndex)
        {
            return source.FillMissingIntervals(
                maxEdgeIndex,
                (from, to) => new RoadClassInterval
                {
                    FromIndex = from,
                    ToIndex = to,
                    RoadClass = RoadClassType.Unknown
                });
        }

        private static IReadOnlyList<T> FillMissingIntervals<T>(this IEnumerable<T> source, int maxEdgeIndex, Func<int, int, T> createUnknownSegment)
        where T : RouteAttributeInterval
        {
            var ordered = source.OrderBy(s => s.FromIndex).ToList();
            var result = new List<T>();
            int currentIndex = 0;

            foreach (var segment in ordered)
            {
                //inserts unknown segment before existing segment
                if (segment.FromIndex > currentIndex)
                    result.Add(createUnknownSegment(currentIndex, segment.FromIndex));
                
                //inserts existing segment
                result.Add(segment);
                currentIndex = segment.ToIndex;
            }

            //append last unknown segment if needed
            if (currentIndex < maxEdgeIndex)
            {
                result.Add(createUnknownSegment(currentIndex, maxEdgeIndex));
            }

            return result;
        }
    }
}
