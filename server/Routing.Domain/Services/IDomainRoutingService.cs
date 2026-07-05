
using Offroad.Routing.Domain.Entities;

namespace Offroad.Routing.Domain.Services;

public interface IDomainRoutingService
{
    IReadOnlyList<LoopSkeleton> FindSkeletons(
        Hookpoint startHookpoint,
        IReadOnlyList<Hookpoint> allHookpointsInArea,
        IReadOnlyList<Road> allRoadsInArea,
        double targetLoopDistanceMeters);
}
