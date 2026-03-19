using Routing.Domain.Enums;

namespace Routing.Domain.ValueObjects
{
    public record RestrictedZone : RouteAttributeInterval
    {
        public required RestrictionType RestrictionType { get; init; }
    }
}
