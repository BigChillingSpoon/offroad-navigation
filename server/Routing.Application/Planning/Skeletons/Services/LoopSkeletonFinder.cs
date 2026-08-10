using Routing.Application.Planning.Skeletons.Models;
using Routing.Domain.Entities;

namespace Routing.Application.Planning.Skeletons.Services;

/// <summary>
/// Finds the best distinct, non-overlapping loop skeletons from an entrance with a branch-and-bound
/// search over the real GIS graph (see <see cref="LoopSearch"/>). Small arenas are searched
/// (near-)exhaustively; large ones use an adaptive beam that trades exactness for speed.
/// </summary>
public class LoopSkeletonFinder : ILoopSkeletonFinder
{
    public IReadOnlyList<LoopSkeleton> FindSkeletons(
        Node startNode,
        IReadOnlyList<Node> nodesInArea,
        IReadOnlyList<Edge> edgesInArea,
        double targetLoopDistanceMeters,
        SkeletonSearchOptions options)
    {
        var nodesById = nodesInArea.ToDictionary(n => n.Id);
        var adjacency = BuildGraph(edgesInArea, nodesById, options);

        // Per-call search state lives on LoopSearch, so a single shared finder stays thread-safe when
        // arenas are searched concurrently.
        return new LoopSearch(startNode, adjacency, targetLoopDistanceMeters).Run();
    }

    /// <summary>
    /// Builds a bidirectional adjacency list from the raw edges, dropping any edge that is not usable:
    /// classes the routing provider can't route, car-inaccessible non-track roads, and (per the search
    /// options) barriers, private/restricted edges and grades above the vehicle limit. Edges traverse
    /// both directions - direction-of-travel is governed by HasNoEntry, not Source/Target.
    /// </summary>
    private static Dictionary<long, List<(Edge Edge, Node Neighbor)>> BuildGraph(
        IReadOnlyList<Edge> edges,
        Dictionary<long, Node> nodesById,
        SkeletonSearchOptions options)
    {
        var adjacency = new Dictionary<long, List<(Edge, Node)>>();

        foreach (var edge in edges)
        {
            // Paved edges are intentionally NOT excluded: the routing provider deprioritises them and
            // would detour around a paved connector, but we hand it the skeleton's full traversed
            // geometry, which pins it to our exact roads, so a short paved link is followed faithfully.
            if (SkeletonSearchSettings.NonRoutableHighways.Contains(edge.Highway)) continue;

            // The routing provider will not route a car-inaccessible road that is not a track (even when
            // private roads are allowed), so exclude it to avoid detours. Car-inaccessible tracks stay.
            if (edge.HasNoEntry && !edge.IsTrack) continue;

            if (edge.HasBarrier && !options.AllowGates) continue;

            // Single "avoid private/restricted" switch: private/forestry access plus national-park tracks.
            // Non-track roads through a park stay routable.
            if (!options.AllowPrivateRoads &&
                (edge.HasNoEntry || (edge.IsRestricted && edge.IsTrack)))
                continue;

            if (edge.Grade > options.MaxGrade) continue;

            AddDirected(adjacency, nodesById, edge.SourceNodeId, edge.TargetNodeId, edge);
            AddDirected(adjacency, nodesById, edge.TargetNodeId, edge.SourceNodeId, edge);
        }

        return adjacency;
    }

    private static void AddDirected(
        Dictionary<long, List<(Edge Edge, Node Neighbor)>> adjacency,
        Dictionary<long, Node> nodesById,
        long fromId,
        long toId,
        Edge edge)
    {
        if (!nodesById.TryGetValue(toId, out var toNode)) return;

        if (!adjacency.TryGetValue(fromId, out var list))
        {
            list = new List<(Edge, Node)>();
            adjacency[fromId] = list;
        }

        list.Add((edge, toNode));
    }
}
