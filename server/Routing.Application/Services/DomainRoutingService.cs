
using Offroad.Routing.Domain.Entities;
using Offroad.Routing.Domain.Services;
using Routing.Application.Planning.Skeletons.Models;
using Routing.Domain.Enums;
using Routing.Domain.Models;
using System.Diagnostics;

namespace Offroad.Routing.Application.Services;

public class DomainRoutingService : IDomainRoutingService
{
    public IReadOnlyList<LoopSkeleton> FindSkeletons(
        Hookpoint startHookpoint,
        IReadOnlyList<Hookpoint> allHookpointsInArea,
        IReadOnlyList<Road> allRoadsInArea,
        double targetLoopDistanceMeters)
    {
        // Implementation will go here
        throw new NotImplementedException();
    }
}
