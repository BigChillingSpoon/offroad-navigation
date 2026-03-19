using Routing.Domain.Enums;

namespace Routing.Domain.ValueObjects
{
    public record RoadAccessInterval : RouteAttributeInterval
    {
        public required RoadAccessType RoadAccess { get; init; }
    }
}
