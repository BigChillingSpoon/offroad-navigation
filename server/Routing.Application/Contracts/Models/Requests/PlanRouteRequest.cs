using Routing.Domain.ValueObjects;

namespace Routing.Application.Contracts.Models
{
    public sealed record PlanRouteRequest(
        Coordinate Start,
        Coordinate End,
        bool AllowPrivateRoads = false,
        bool AllowGates = false
    );
}


