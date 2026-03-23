using Routing.Domain.Enums;
using Routing.Domain.ValueObjects;

namespace Routing.Application.Planning.Candidates.Builders
{
    public static class BarrierBuilder
    {
        public static IReadOnlyList<RoadBarrier> Build(IEnumerable<Interval<BarrierType>> barrierIntervals, IReadOnlyList<Coordinate> geometry)
        {
            if (barrierIntervals == null || !barrierIntervals.Any())
                return Array.Empty<RoadBarrier>();

            return barrierIntervals
                .Select(interval => new RoadBarrier(
                    Type: interval.Value,
                    PointIndex: interval.FromIndex,
                    Coordinate: geometry[interval.ToIndex]
                )).ToList();
        }
    }
}
