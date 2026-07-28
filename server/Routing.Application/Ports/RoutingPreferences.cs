using Routing.Domain.Enums;

namespace Routing.Application.Ports
{
    public sealed record RoutingPreferences
    {
        public required RouteBalance Balance { get; init; }
        public bool AllowPrivateRoads { get; init; }
        public bool AllowGates { get; init; }
    }
}
