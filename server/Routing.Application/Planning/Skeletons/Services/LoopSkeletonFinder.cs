using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Skeletons.Models;
using Routing.Application.Planning.Skeletons.Services;
using Routing.Domain.Entities;
using Routing.Domain.Utilities;
using Routing.Domain.ValueObjects;
using LoopSkeleton = Routing.Application.Planning.Skeletons.Models.LoopSkeleton;

public class LoopSkeletonFinder : ILoopSkeletonFinder
{
    // Maximální povolená "ostrost" zatáčky v lese (120 stupňů znamená, že nepojedeš ostře zpět)
    private const double MaxTurnAngleDegrees = 120.0;

    // Dynamic Minimum Distance: nedovolíme uzavřít okruh, dokud neujedeme smysluplnou část cílové vzdálenosti
    private const double MinClosureFraction = 0.3;
    private const double MinTargetFraction = LoopDistanceTolerance.MinFraction;
    private const double MaxTargetFraction = LoopDistanceTolerance.MaxFraction;

    private readonly ILoopDirectionPreference _directionPreference;

    public LoopSkeletonFinder(ILoopDirectionPreference directionPreference)
    {
        _directionPreference = directionPreference;
    }

    public IReadOnlyList<LoopSkeleton> FindSkeletons(
        Node startNode,
        IReadOnlyList<Node> nodesInArea,
        IReadOnlyList<Edge> edgesInArea,
        double targetLoopDistanceMeters,
        SkeletonSearchOptions options)
    {
        var nodesById = nodesInArea.ToDictionary(n => n.Id);
        var adjacency = BuildGraph(edgesInArea, nodesById, options);

        var rawSkeletons = new List<List<SkeletonVector>>();

        FindSkeletonsRecursive(
            startNode: startNode,
            currentPath: new List<SkeletonVector>(),
            visitedNodeIds: new HashSet<long> { startNode.Id },
            adjacency: adjacency,
            targetDistance: targetLoopDistanceMeters,
            accumulatedDistance: 0,
            completedSkeletons: rawSkeletons);

        var uniqueSkeletons = DeduplicateReversedLoops(rawSkeletons);

        return uniqueSkeletons
            .Select(chain => new
            {
                Chain = chain,
                Score = ScoreVectorChain(chain, targetLoopDistanceMeters)
            })
            .OrderByDescending(x => x.Score)
            .Select(x => ToLoopSkeleton(startNode, x.Chain))
            .ToList();
    }

    /// <summary>
    /// The graph is explored bidirectionally from a single start node, so every loop is found twice -
    /// once in each traversal direction (A-&gt;B-&gt;C-&gt;A and A-&gt;C-&gt;B-&gt;A). Both cover the exact same
    /// edges and therefore the exact same distance; they are not two different loops. This groups
    /// chains by a direction-agnostic identity and keeps only one per physical loop, chosen by
    /// <see cref="_directionPreference"/>.
    /// </summary>
    private List<List<SkeletonVector>> DeduplicateReversedLoops(List<List<SkeletonVector>> rawSkeletons)
    {
        var chosenByLoopIdentity = new Dictionary<string, List<SkeletonVector>>();

        foreach (var chain in rawSkeletons)
        {
            var identity = GetDirectionAgnosticIdentity(chain);

            chosenByLoopIdentity[identity] = chosenByLoopIdentity.TryGetValue(identity, out var existing)
                ? _directionPreference.ChooseDirection(existing, chain)
                : chain;
        }

        return chosenByLoopIdentity.Values.ToList();
    }

    /// <summary>
    /// A loop and its reverse traversal visit the exact same node sequence backwards. Canonicalizing
    /// to whichever of (sequence, reversed sequence) sorts first gives both directions the same key,
    /// while a genuinely different loop (different nodes) still gets a different key.
    /// </summary>
    private static string GetDirectionAgnosticIdentity(List<SkeletonVector> chain)
    {
        var interiorNodeIds = GetInteriorNodeIds(chain);
        var reversedNodeIds = Enumerable.Reverse(interiorNodeIds).ToList();

        var canonical = interiorNodeIds;
        for (var i = 0; i < interiorNodeIds.Count; i++)
        {
            if (interiorNodeIds[i] == reversedNodeIds[i]) continue;
            canonical = interiorNodeIds[i] < reversedNodeIds[i] ? interiorNodeIds : reversedNodeIds;
            break;
        }

        return string.Join(",", canonical);
    }

    /// <summary>
    /// Every chain's last vector is the closing edge back to the start node - a fixed anchor shared
    /// by every chain in this search, not a distinguishing part of the loop's shape. Excluding it is
    /// what makes a forward chain's reverse actually equal its backward counterpart's sequence
    /// (both directions close on the same anchor; the anchor itself isn't mirrored).
    /// </summary>
    private static List<long> GetInteriorNodeIds(List<SkeletonVector> chain) =>
        chain.Take(chain.Count - 1).Select(v => v.ToNode.Id).ToList();

    /// <summary>
    /// Builds a bidirectional adjacency list from the raw edges, dropping any edge/neighbor that the
    /// search options exclude (barriers, no-entry, restricted zones). Edges traverse both directions -
    /// direction-of-travel is governed by HasNoEntry, not by which end is Source/Target.
    /// </summary>
    private static Dictionary<long, List<(Edge Edge, Node Neighbor)>> BuildGraph(
        IReadOnlyList<Edge> edges,
        Dictionary<long, Node> nodesById,
        SkeletonSearchOptions options)
    {
        var adjacency = new Dictionary<long, List<(Edge Edge, Node Neighbor)>>();

        void AddDirected(long fromId, long toId, Edge edge)
        {
            if (!nodesById.TryGetValue(toId, out var toNode)) return;

            if (!adjacency.TryGetValue(fromId, out var list))
            {
                list = new List<(Edge, Node)>();
                adjacency[fromId] = list;
            }

            list.Add((edge, toNode));
        }

        foreach (var edge in edges)
        {
            if (edge.HasBarrier && !options.AllowGates) continue;
            if (edge.HasNoEntry && !options.AllowPrivateRoads) continue;
            if (edge.IsRestricted && !options.AllowRestrictedZones) continue;

            AddDirected(edge.SourceNodeId, edge.TargetNodeId, edge);
            AddDirected(edge.TargetNodeId, edge.SourceNodeId, edge);
        }

        return adjacency;
    }

    private void FindSkeletonsRecursive(
        Node startNode,
        List<SkeletonVector> currentPath,
        HashSet<long> visitedNodeIds,
        Dictionary<long, List<(Edge Edge, Node Neighbor)>> adjacency,
        double targetDistance,
        double accumulatedDistance,
        List<List<SkeletonVector>> completedSkeletons)
    {
        var currentNode = currentPath.Count > 0 ? currentPath[^1].ToNode : startNode;

        if (!adjacency.TryGetValue(currentNode.Id, out var neighbors))
            return;

        // --- KONTROLA UZAVŘENÍ OKRUHU (pouze přes reálnou hranu zpět na start) ---
        if (currentPath.Count >= 2)
        {
            var lastHeadingAngle = currentPath[^1].HeadingAngleDegrees;

            if (accumulatedDistance >= targetDistance * MinClosureFraction)
            {
                var closingNeighbor = neighbors.FirstOrDefault(n => n.Neighbor.Id == startNode.Id);

                if (closingNeighbor.Edge != null)
                {
                    var totalWithClosing = accumulatedDistance + closingNeighbor.Edge.LengthMeters;

                    if (totalWithClosing >= targetDistance * MinTargetFraction && totalWithClosing <= targetDistance * MaxTargetFraction)
                    {
                        var closingDepartureBearing = GetDepartureBearing(closingNeighbor.Edge, currentNode, startNode);
                        var closingTurnAngle = GeoCalculator.GetAngleDifference(lastHeadingAngle, closingDepartureBearing);

                        if (closingTurnAngle <= MaxTurnAngleDegrees)
                        {
                            var closingArrivalBearing = GetArrivalBearing(closingNeighbor.Edge, currentNode, startNode);
                            var closingVector = new SkeletonVector(
                                currentNode,
                                startNode,
                                closingNeighbor.Edge.LengthMeters,
                                closingArrivalBearing);

                            completedSkeletons.Add(new List<SkeletonVector>(currentPath) { closingVector });
                                return;
                        }
                    }
                }
            }

            if (accumulatedDistance > targetDistance * MaxTargetFraction) 
                return; // Hard-cut
        }

        // --- SKOKY NA SOUSEDY (pouze reálně propojené hrany) ---
        foreach (var (edge, neighborNode) in neighbors)
        {
            if (visitedNodeIds.Contains(neighborNode.Id)) continue;

            var departureBearing = GetDepartureBearing(edge, currentNode, neighborNode);

            if (currentPath.Count > 0)
            {
                var turnAngle = GeoCalculator.GetAngleDifference(currentPath[^1].HeadingAngleDegrees, departureBearing);

                // ZABIJÁK V-VRACÁKŮ:
                if (turnAngle > MaxTurnAngleDegrees)
                    continue;
            }

            var arrivalBearing = GetArrivalBearing(edge, currentNode, neighborNode);

            currentPath.Add(new SkeletonVector(currentNode, neighborNode, edge.LengthMeters, arrivalBearing));
            visitedNodeIds.Add(neighborNode.Id);

            FindSkeletonsRecursive(
                startNode: startNode,
                currentPath: currentPath,
                visitedNodeIds: visitedNodeIds,
                adjacency: adjacency,
                targetDistance: targetDistance,
                accumulatedDistance: accumulatedDistance + edge.LengthMeters,
                completedSkeletons: completedSkeletons);

            // UNDO: Backtracking
            currentPath.RemoveAt(currentPath.Count - 1);
            visitedNodeIds.Remove(neighborNode.Id);
        }
    }

    /// <summary>
    /// The heading as you depart <paramref name="fromNode"/> onto <paramref name="edge"/> - the true
    /// initial bearing of the edge's own geometry near the junction, not a straight chord to
    /// <paramref name="toNode"/>. A long or curved edge's whole-length chord can point in a very
    /// different direction than its first few meters, which is what actually matters for a turn-angle
    /// check happening right at the shared node. Falls back to the node-to-node chord when the edge
    /// has no stored geometry (e.g. synthetic edges in tests).
    /// </summary>
    private static double GetDepartureBearing(Edge edge, Node fromNode, Node toNode)
    {
        if (edge.Geometry.Count < 2)
            return GeoCalculator.CalculateBearing(fromNode.Coordinate, toNode.Coordinate);

        var traversedInReverse = fromNode.Id == edge.TargetNodeId;
        return GeoCalculator.CalculateDepartureBearing(edge.Geometry, traversedInReverse);
    }

    /// <summary>
    /// The heading as you arrive at <paramref name="toNode"/> from <paramref name="edge"/> - the true
    /// final bearing of the edge's own geometry near the junction. Stored as the vector's
    /// HeadingAngleDegrees so the next hop's turn-angle check compares against the real direction of
    /// travel at the node, not the previous edge's whole-length average direction.
    /// </summary>
    private static double GetArrivalBearing(Edge edge, Node fromNode, Node toNode)
    {
        if (edge.Geometry.Count < 2)
            return GeoCalculator.CalculateBearing(fromNode.Coordinate, toNode.Coordinate);

        var traversedInReverse = fromNode.Id == edge.TargetNodeId;
        return GeoCalculator.CalculateArrivalBearing(edge.Geometry, traversedInReverse);
    }

    /// <summary>
    /// Ranks completed chains by how close they land to the requested distance. Replaces the old "RAM
    /// spatial density" heuristic, which existed only to compensate for the previous model's lack of real
    /// connectivity guarantees - every edge here is a real, connected road, so that compensation is no
    /// longer needed. (There is no per-edge difficulty grade in the real schema to reward variety on.)
    /// </summary>
    private static double ScoreVectorChain(List<SkeletonVector> chain, double targetDistance)
    {
        var totalDistance = chain.Sum(v => v.EdgeLengthMeters);
        return 1.0 - Math.Abs(totalDistance - targetDistance) / targetDistance;
    }

    private static LoopSkeleton ToLoopSkeleton(Node startNode, List<SkeletonVector> chain)
    {
        var entrance = startNode.Coordinate;
        // Exclude the closing vector - its ToNode is startNode itself, already covered by Entrance.
        var waypoints = chain.Take(chain.Count - 1).Select(v => v.ToNode.Coordinate).ToList();
        var totalMeters = chain.Sum(v => v.EdgeLengthMeters);

        return new LoopSkeleton(
            Entrance: entrance,
            Waypoints: waypoints,
            TraveledDistanceKm: (float)(totalMeters / 1000.0));
    }
}
