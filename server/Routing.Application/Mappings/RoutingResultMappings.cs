using Routing.Application.Contracts.Models;
using Routing.Domain.Models;

namespace Routing.Application.Mappings
{
    public static class RoutingResultMappings
    {
        public static TripResult ToTripResult(this Trip trip)
            => new()
            {
                Id = trip.Id,
                Name = trip.Name,
                Type = trip.Type,
                TotalDistanceMeters = trip.Plan.TotalDistanceInMeters,
                OffroadDistanceMeters = trip.Plan.OffroadDistanceMeters,
                Duration = TimeSpan.FromSeconds(trip.Plan.DurationSeconds),
                ElevationGainMeters = trip.Plan.ElevationGainMeters,
            };
    }
}
