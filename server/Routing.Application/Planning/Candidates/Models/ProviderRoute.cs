using Routing.Domain.Enums;
using Routing.Domain.ValueObjects;

namespace Routing.Application.Planning.Candidates.Models
{
    public sealed class ProviderRoute
    {
        public double Distance { get; init; }
        public TimeSpan Duration { get; init; }
        public double Ascend { get; init; }
        public double Descend { get; init; }
        public EncodedPolyline Polyline { get; init; } = new();
        public IReadOnlyList<Interval<RoadClassType>> RoadClassIntervals { get; init; }
        public IReadOnlyList<Interval<SurfaceType>> SurfaceIntervals { get; init; }
        public IReadOnlyList<Interval<TrackType>> TrackTypeIntervals { get; init; }
        public IReadOnlyList<Interval<BarrierType>> BarrierIntervals { get; init; }
        public IReadOnlyList<Interval<RoadAccessType>> RoadAccessIntervals { get; init; }
    }
}
