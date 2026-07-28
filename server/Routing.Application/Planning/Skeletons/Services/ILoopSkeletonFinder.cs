using Routing.Domain.Entities;
using System.Collections.Generic;
using LoopSkeleton = Routing.Application.Planning.Skeletons.Models.LoopSkeleton;
using SkeletonSearchOptions = Routing.Application.Planning.Skeletons.Models.SkeletonSearchOptions;

namespace Routing.Application.Planning.Skeletons.Services;

/// <summary>
/// Defines the contract for the service that finds loop skeletons using a constrained search algorithm
/// over the real GIS graph (nodes and edges).
/// </summary>
public interface ILoopSkeletonFinder
{
    /// <summary>
    /// Finds valid loop skeletons by traversing actual edges between nodes, starting and ending at <paramref name="startNode"/>.
    /// </summary>
    /// <param name="startNode">The entry/portal node to start and close the loop at.</param>
    /// <param name="nodesInArea">The complete list of nodes in the search area, including the start node.</param>
    /// <param name="edgesInArea">The complete list of edges connecting the nodes in the search area.</param>
    /// <param name="targetLoopDistanceMeters">The desired total loop distance in meters.</param>
    /// <param name="options">User-preference pruning rules (gates, private roads, restricted zones).</param>
    /// <returns>A list of valid LoopSkeletons, ordered best-first. Returns an empty list if no valid skeletons can be found.</returns>
    IReadOnlyList<LoopSkeleton> FindSkeletons(
        Node startNode,
        IReadOnlyList<Node> nodesInArea,
        IReadOnlyList<Edge> edgesInArea,
        double targetLoopDistanceMeters,
        SkeletonSearchOptions options);
}
