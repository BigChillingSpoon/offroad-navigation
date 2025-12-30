using Routing.Domain.Enums;

namespace Routing.Application.Contracts.Models
{
    public sealed record PlanRouteRequest
    {
        public required double StartLatitude { get; init; }
        public required double StartLongitude { get; init; }
        public required double EndLatitude { get; init; }
        public required double EndLongitude { get; init; }
        public bool AllowPrivateRoads { get; init; }
        public bool AllowGates { get; init; }
        public RouteBalance RouteBalance { get; init; } = RouteBalance.Balanced;
    }
}
