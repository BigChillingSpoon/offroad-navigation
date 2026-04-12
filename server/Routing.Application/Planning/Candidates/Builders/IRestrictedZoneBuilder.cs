using Routing.Domain.Enums;
using Routing.Domain.ValueObjects;

namespace Routing.Application.Planning.Candidates.Builders
{
    public interface IRestrictedZoneBuilder
    {
        Task<IReadOnlyList<Interval<RestrictionType>>> BuildAsync(IReadOnlyList<Interval<RoadAccessType>> roadAccessIntervals, IReadOnlyList<Coordinate> geometry);
    }
}
