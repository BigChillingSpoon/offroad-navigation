
using Routing.Domain.Enums;

namespace Routing.Domain.ValueObjects
{
    public sealed record BarrierInterval : RouteAttributeInterval
    {
        public required BarrierType BarrierType { get; init; }
    }
}
