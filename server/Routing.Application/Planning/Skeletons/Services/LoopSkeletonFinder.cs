using Routing.Application.Planning.Skeletons.Models;
using Routing.Domain.Models;

namespace Routing.Application.Planning.Skeletons.Services;

public sealed class LoopSkeletonFinder : ILoopSkeletonFinder
{
    public IReadOnlyList<SkeletonCandidate> FindSkeletons(
        Hookpoint startHookpoint,
        IReadOnlyList<Hookpoint> allHookpointsInArea,
        double targetLoopDistanceMeters)
    {
        var validSkeletons = new List<SkeletonCandidate>();
        var initialVectorChain = new List<SkeletonVector>();

        // Start the recursive search
        FindSkeletonsRecursive(startHookpoint, initialVectorChain, allHookpointsInArea, targetLoopDistanceMeters, validSkeletons);

        return validSkeletons;
    }

    private void FindSkeletonsRecursive(
        Hookpoint currentHookpoint,
        List<SkeletonVector> currentVectorChain,
        IReadOnlyList<Hookpoint> remainingHookpoints,
        double targetLoopDistanceMeters,
        List<SkeletonCandidate> completedSkeletons)
    {
        // TODO: Implement the full Constrained Depth-First Search (DFS) algorithm here.
        // This method will be recursive and will build the 'currentVectorChain'.
        // On finding a valid chain, it will create a SkeletonCandidate and add it to the `completedSkeletons` list.
    }
}
