using Routing.Application.Contracts.Responses;
using Routing.Application.Planning.Encoding;
using Routing.Domain.Models;
using Routing.Domain.ValueObjects;

namespace Routing.Application.Mappings
{
    public static class RoutingResultMappings
    {
        public static TripResult ToTripResult(this Trip trip)
        {
            if (trip is null)
                throw new InvalidOperationException("Trip cannot be null.");

            if (trip.Plan is null)
                throw new InvalidOperationException("TripPlan is missing.");

            return new() {
                Id = trip.Id,
                Name = trip.Name,
                Type = trip.Type,
                Metrics = trip.Plan.ToTripMetrics(),
                Details = trip.Plan.Segments.ToTripDetails(),
            };
        }


        private static TripMetrics ToTripMetrics(this TripPlan plan)
        {
            if (plan is null)
                throw new InvalidOperationException("TripPlan cannot be null.");

            return new TripMetrics
            {
                TotalDistanceMeters = plan.TotalDistanceMeters,
                OffroadDistanceMeters = plan.OffroadDistanceMeters,
                Duration = plan.Duration,
                ElevationGainMeters = plan.ElevationGainMeters,
                ElevationLossMeters = plan.ElevationLossMeters
            };
        }

        private static TripDetails ToTripDetails(this IReadOnlyList<Segment> segments)
        {
            if (segments.Count == 0)
            {
                return new EmptyTripDetails();
            }

            var allPoints = new List<Coordinate>();

            foreach (var segment in segments)
            {
                if (segment is null)
                    throw new InvalidOperationException("Segment cannot be null.");

                if (segment.Geometry is null)
                    throw new InvalidOperationException(
                        "Segment geometry is null.");

                if (segment.Geometry.Count == 0)
                    throw new InvalidOperationException(
                        "Segment geometry is empty.");

                allPoints.AddRange(segment.Geometry);
            }

            if (allPoints.Count == 0)
                throw new InvalidOperationException(
                    "TripPlan contains segments but no geometry points.");

            var hasElevation = allPoints.Any(p => p.Elevation.HasValue);

            var encodedPolyline = PolylineEncoder.Encode(
                allPoints,
                PolylineSettings.Multiplier,
                PolylineSettings.ElevationMultiplier,
                hasElevation);

            return new TripDetailsWithData(encodedPolyline, allPoints[0], allPoints[^1]);
        }
    }
}
