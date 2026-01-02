using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Profiles;
using Routing.Domain.ValueObjects;
using Routing.Application.Contracts.Models;

namespace Routing.Application.Mappings;

public static class RoutingRequestMappings
{
    #region ROUTES
    public static RouteIntent ToRouteIntent(this PlanRouteRequest request)
        => new()
        {
            Start = new Coordinate(request.StartLatitude, request.StartLongitude),
            End = new Coordinate(request.EndLatitude, request.EndLongitude),
            Balance = request.RouteBalance
        };

    public static UserRoutingProfile ToUserProfile(this PlanRouteRequest request)
        => new()
        {
            AllowPrivateRoads = request.AllowPrivateRoads,
            AllowGates = request.AllowGates,
        };
    #endregion ROUTES
    #region LOOPS
    public static LoopIntent ToLoopIntent(this FindLoopsRequest request)
        => new()
        {
            Start = new Coordinate(request.StartLatitude, request.StartLongitude),
            PreferredLengthKm = request.PreferredLengthKm,
            MaxDriveDistanceKm = request.MaxDriveDistanceKm
        };

    public static UserRoutingProfile ToUserProfile(this FindLoopsRequest request)
        => new()
        {
            AllowPrivateRoads = request.AllowPrivateRoads,
            AllowGates = request.AllowGates,
        };
    #endregion LOOPS
}
