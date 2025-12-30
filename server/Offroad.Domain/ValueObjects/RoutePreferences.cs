using Routing.Domain.Enums;

namespace Routing.Domain.ValueObjects
{
    public record RoutePreferences
    {
        public bool AllowPrivateRoads { get; init; }
        public bool AllowGates { get; init; }
        public RouteBalance RouteBalance { get; init; } = RouteBalance.Balanced;
    }
}
