using Routing.Domain.Enums;
using Routing.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Candidates.Builders
{
    public static class BarrierBuilder
    {
        public static IReadOnlyList<RoadBarrier> Build(IEnumerable<BarrierInterval> barrierIntervals, IReadOnlyList<Coordinate> geometry)
        {
            if (barrierIntervals == null || !barrierIntervals.Any())
                return Array.Empty<RoadBarrier>();

            return barrierIntervals
                .Select(interval => new RoadBarrier(
                    Type:interval.BarrierType,
                    PointIndex: interval.FromIndex,
                    Coordinate: geometry[interval.ToIndex]
                )).ToList();
        }
    }
}

