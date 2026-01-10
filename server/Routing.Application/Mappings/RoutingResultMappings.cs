using Routing.Application.Contracts.Responses;
using Routing.Application.Planning.Encoding;
using Routing.Domain.Models;
using Routing.Domain.ValueObjects;

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
                Metrics = trip.Plan.ToTripMetrics(),
                Details = trip.Plan.Segments.ToTripDetails(),
            };

        private static TripMetrics ToTripMetrics(this TripPlan plan)
            => new()
            {
                TotalDistanceMeters = plan.TotalDistanceInMeters,
                OffroadDistanceMeters = plan.OffroadDistanceMeters,
                Duration = TimeSpan.FromSeconds(plan.DurationSeconds),
                ElevationGainMeters = plan.ElevationGainMeters,
            };

        private static TripDetails ToTripDetails(this IReadOnlyList<Segment> segments)
        {
            var allPoints = segments.SelectMany(s => s.Geometry).ToList();
            var polyline = PolylineEncoder.Encode(allPoints);

            return new TripDetails
            {
                Polyline = polyline,
                Start = allPoints.FirstOrDefault(),//todo throw/handle exceptions
                End = allPoints.LastOrDefault()
            };
        }
    }
}
