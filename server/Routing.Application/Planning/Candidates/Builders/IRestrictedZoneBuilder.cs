using Routing.Domain.ValueObjects;

namespace Routing.Application.Planning.Candidates.Builders
{
    public interface IRestrictedZoneBuilder
    {
        IReadOnlyList<RestrictedZone> Build(IReadOnlyList<RoadAccessInterval> roadAccessIntervals, IReadOnlyList<Coordinate> geometry);
    }
}
