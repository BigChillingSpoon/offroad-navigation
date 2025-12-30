using Routing.Application.Contracts.Models;
using Routing.Domain.ValueObjects;

namespace Routing.Application.Mappings
{
    public static class RequestMappings
    {
        public static Coordinate ToStartCoordinate(this PlanRouteRequest request)
            => new(request.StartLatitude, request.StartLongitude);
        public static Coordinate ToStartCoordinate(this FindLoopsRequest request)
            => new(request.StartLatitude, request.StartLongitude);

        public static Coordinate ToEndCoordinate(this PlanRouteRequest request)
            => new(request.EndLatitude, request.EndLongitude);

        public static RoutePreferences ToPreferences(this PlanRouteRequest request)
            => new()
            {
                AllowPrivateRoads = request.AllowPrivateRoads,
                AllowGates = request.AllowGates,
                RouteBalance = request.RouteBalance
            };

        public static LoopPreferences ToPreferences(this FindLoopsRequest request)
            => new()
            {
                MaxDriveDistanceKm = request.MaxDriveDistanceKm,
                PreferredLengthKm = request.PreferredLengthKm,
                AllowPrivateRoads = request.AllowPrivateRoads,
                AllowGates = request.AllowGates
            };
    }
}
