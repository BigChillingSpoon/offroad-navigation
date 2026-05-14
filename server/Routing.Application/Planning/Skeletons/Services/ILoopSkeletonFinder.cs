using Routing.Application.Planning.Skeletons.Models;
using Routing.Domain.Models;
using System.Collections.Generic;

namespace Routing.Application.Planning.Skeletons.Services;

/// <summary>
/// Defines the contract for the service that finds loop skeletons using a constrained search algorithm.
/// </summary>
public interface ILoopSkeletonFinder
{
    /// <summary>
    /// Finds valid loop skeletons based on a starting point and a collection of available hookpoints.
    /// </summary>
    /// <param name="startHookpoint">The entry/portal hookpoint to start the search from.</param>
    /// <param name="allHookpointsInArea">The complete list of hookpoints in the search area, including the start point.</param>
    /// <param name="targetLoopDistanceMeters">The desired total loop distance in meters.</param>
    /// <returns>A list of valid SkeletonCandidates. Returns an empty list if no valid skeletons can be found.</returns>
    IReadOnlyList<LoopSkeleton> FindSkeletons(
        Hookpoint startHookpoint,
        IReadOnlyList<Hookpoint> allHookpointsInArea,
        double targetLoopDistanceMeters);
}
