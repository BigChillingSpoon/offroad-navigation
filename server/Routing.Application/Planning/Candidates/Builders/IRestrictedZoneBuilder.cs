using Routing.Domain.Enums;
using Routing.Domain.ValueObjects;

namespace Routing.Application.Planning.Candidates.Builders
{
    public interface IRestrictedZoneBuilder
    {
        IReadOnlyList<Interval<RestrictionType>> Build(IReadOnlyList<Interval<RoadAccessType>> roadAccessIntervals, IReadOnlyList<Coordinate> geometry);
    }
}
