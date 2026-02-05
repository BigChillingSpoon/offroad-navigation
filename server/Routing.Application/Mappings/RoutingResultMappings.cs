using Routing.Application.Contracts.Responses;
using Routing.Application.Planning.Encoding;
using Routing.Domain.Models;
using Routing.Domain.ValueObjects;
using System.Xml.Linq;

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
                TotalDistanceMeters = plan.TotalDistanceInMeters,
                OffroadDistanceMeters = plan.OffroadDistanceMeters,
                Duration = TimeSpan.FromSeconds(plan.DurationSeconds),
                ElevationGainMeters = plan.ElevationGainMeters,
            };
        }

        private static TripDetails ToTripDetails(this IReadOnlyList<Segment> segments)
        {
            //return empty trip details
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

            string polyline;
            try
            {
                polyline = PolylineEncoder.Encode(allPoints);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to encode trip geometry to polyline.",
                    ex);
            }

            return new TripDetailsWithData(polyline, allPoints[0], allPoints[^1]);
        }
    }
}
