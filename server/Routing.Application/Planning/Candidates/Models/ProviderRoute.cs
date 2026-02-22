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
        public IReadOnlyList<RoadClassInterval> RoadClassIntervals { get; init; }
        public IReadOnlyList<SurfaceInterval> SurfaceIntervals { get; init; } 
    }
}
