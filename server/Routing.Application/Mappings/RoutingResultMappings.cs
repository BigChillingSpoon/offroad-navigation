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
                return new EmptyTripDetails();

            var allPoints = JoinSegmentGeometries(segments);

            var hasElevation = allPoints.Any(p => p.Elevation.HasValue);

            var encodedPolyline = PolylineEncoder.Encode(
                allPoints,
                PolylineSettings.Multiplier,
                PolylineSettings.ElevationMultiplier,
                hasElevation);

            return new TripDetailsWithData(encodedPolyline, allPoints[0], allPoints[^1]);
        }

        /// <summary>
        /// Joins segment geometries into a single list, avoiding duplicate points at segment boundaries.
        /// Each segment contains its full geometry including start and end points.
        /// When segments share boundary points (end of segment N = start of segment N+1),
        /// we skip the first point of subsequent segments to avoid duplicates.
        /// </summary>
        private static List<Coordinate> JoinSegmentGeometries(IReadOnlyList<Segment> segments)
        {
            var firstSegment = segments.First();
            ValidateSegment(firstSegment, 0);

            var allPoints = new List<Coordinate>(firstSegment.Geometry);

            for (int i = 1; i < segments.Count; i++)
            {
                var segment = segments[i];
                ValidateSegment(segment, i);

                // Skip first point - it's the same as previous segment's last point
                allPoints.AddRange(segment.Geometry.Skip(1));
            }
#if DEBUG
            foreach(var seg in allPoints)
            {
                //for debug purposes - actually handy for displaying the result - TODO REMOVE
                Console.WriteLine("["+seg.Longitude +","+ seg.Latitude+"],");
            }
#endif
            return allPoints;
        }

        private static void ValidateSegment(Segment segment, int index)
        {
            if (segment is null)
                throw new InvalidOperationException($"Segment at index {index} cannot be null.");

            if (segment.Geometry is null || segment.Geometry.Count == 0)
                throw new InvalidOperationException($"Segment at index {index} has no geometry.");
        }
    }
}
