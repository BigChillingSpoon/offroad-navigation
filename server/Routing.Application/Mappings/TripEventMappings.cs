using Routing.Application.Contracts.Responses;
using Routing.Domain.Enums;
using Routing.Domain.Models;

namespace Routing.Application.Mappings
{
    public static class TripEventMapper
    {
        public static IReadOnlyList<TripEvent> MapToEvents(this TripPlan plan)
        {
            var tripEvents = new List<TripEvent>();

            if (plan.Barriers != null)
            {
                tripEvents.AddRange(plan.Barriers.Select(b => new PointEvent
                {
                    Type = TripEventType.Barrier.ToString(),
                    SubType = b.Type.ToString(), 
                    PointIndex = b.PointIndex,
                    Coordinate = b.Coordinate
                }));
            }

            //TODO -after mapping restricted zones, add them as interval events, currently we dont have any logic for them in the planner, but in the future we might want to add some logic to avoid them or at least warn users about them
            //if (plan.RestrictedZones != null)
            //{
            //    apiEvents.AddRange(plan.RestrictedZones.Select(z => new TripEventDto
            //    {
            //        Type = "restriction",
            //        SubType = z.Type.ToString(),
            //        StartIndex = z.StartIndex,
            //        EndIndex = z.EndIndex
            //    }));
            //}

            return tripEvents.ToList(); //add some order by maybe
        }
    }
}
