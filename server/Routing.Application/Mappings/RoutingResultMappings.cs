using Routing.Application.Contracts.Responses;
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

            return new()
            {
                Id = trip.Id,
                Name = trip.Name,
                Type = trip.Type,
                Metrics = trip.Plan.ToTripMetrics(),
                Details = trip.Plan.ToTripDetails(),
                Events = TripEventMapper.MapToEvents(trip.Plan)
            };
        }

        private static TripMetrics ToTripMetrics(this TripPlan plan)
        {
            return new TripMetrics
            {
                TotalDistanceMeters = plan.TotalDistanceMeters,
                OffroadDistanceMeters = plan.OffroadDistanceMeters,
                Duration = plan.Duration,
                ElevationGainMeters = plan.ElevationGainMeters,
                ElevationLossMeters = plan.ElevationLossMeters
            };
        }

        private static TripDetails ToTripDetails(this TripPlan plan)
        {
            if (plan.Segments.Count == 0)
                return new EmptyTripDetails();

            return new TripDetailsWithData(plan.Polyline, plan.Segments[0].Start, plan.Segments[^1].End);
        }
    }
}
