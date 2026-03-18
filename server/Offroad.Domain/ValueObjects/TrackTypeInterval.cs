using Routing.Domain.Enums;

namespace Routing.Domain.ValueObjects
{
    public sealed record TrackTypeInterval : RouteAttributeInterval
    {
        public required TrackType TrackType { get; init; }
    }
}
