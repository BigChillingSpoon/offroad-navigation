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

            if (plan.RestrictedZones != null)
                {
                    tripEvents.AddRange(plan.RestrictedZones.Select(z => new IntervalEvent
                    {
                        Type = TripEventType.Restriction.ToString(),
                        SubType = z.RestrictionType.ToString(),
                        FromIndex = z.FromIndex,
                        ToIndex = z.ToIndex
                    }));
                }

            return tripEvents.ToList(); //add some order by maybe
        }
    }
}
