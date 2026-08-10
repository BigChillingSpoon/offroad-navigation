using System.Diagnostics;
using Routing.Application.Planning.Skeletons.Models;
using Routing.Domain.Entities;
using Routing.Domain.Utilities;
using Routing.Domain.ValueObjects;

namespace Routing.Application.Planning.Skeletons.Services;

/// <summary>
/// One branch-and-bound traversal from a single entrance, holding all mutable search state for the call.
/// Kept off <see cref="LoopSkeletonFinder"/> so a single shared finder stays thread-safe across arenas.
/// </summary>
internal sealed class LoopSearch
{
    private readonly Node _startNode;
    private readonly Coordinate _start;
    private readonly Dictionary<long, List<(Edge Edge, Node Neighbor)>> _adjacency;
    private readonly double _target;
    private readonly double _lMin;
    private readonly double _lMax;

    private readonly List<SkeletonVector> _path = new();
    private readonly HashSet<long> _visited;

    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private long _expansions;
    private bool _budgetExceeded;

    private readonly int _beamWidth;

    // The best non-overlapping loops found so far (at most MaxLoopsPerArena). Pruning uses the weakest
    // kept score once the set is full - a branch is abandoned only when it can't beat even the worst kept.
    private readonly List<KeptLoop> _kept = new();
    private double _pruneScore = double.NegativeInfinity;

    public LoopSearch(
        Node startNode,
        Dictionary<long, List<(Edge Edge, Node Neighbor)>> adjacency,
        double target)
    {
        _startNode = startNode;
        _start = startNode.Coordinate;
        _adjacency = adjacency;
        _target = target;
        _lMin = target * SkeletonSearchSettings.MinTargetFraction;
        _lMax = target * SkeletonSearchSettings.MaxTargetFraction;
        _visited = new HashSet<long> { startNode.Id };

        var nodeCount = adjacency.Count;
        _beamWidth = Math.Clamp(
            (int)Math.Round(SkeletonSearchSettings.BeamMaxWidth * SkeletonSearchSettings.BeamScaleNodes / (nodeCount + SkeletonSearchSettings.BeamScaleNodes)),
            SkeletonSearchSettings.BeamMinWidth,
            SkeletonSearchSettings.BeamMaxWidth);
    }

    public IReadOnlyList<LoopSkeleton> Run()
    {
        Expand(_startNode, dAcc: 0, offroadAcc: 0, elevAcc: 0);
        return _kept
            .OrderByDescending(k => k.Score)
            .Select(k => ToLoopSkeleton(_startNode, k.Chain))
            .ToList();
    }

    private sealed record KeptLoop(List<SkeletonVector> Chain, double Score, HashSet<long> Nodes);

    private void Expand(Node current, double dAcc, double offroadAcc, double elevAcc)
    {
        if (_budgetExceeded) return;

        // Anti-hang budget: stop and keep best-so-far on a pathological arena.
        if (++_expansions > SkeletonSearchSettings.MaxNodeExpansions || _stopwatch.Elapsed > SkeletonSearchSettings.TimeBudget)
        {
            _budgetExceeded = true;
            return;
        }

        // Branch-and-bound. If even the most optimistic completion of this partial path (all remaining
        // budget offroad, lands exactly on target, no further climbing, perfect roundness) cannot beat the
        // weakest loop we are keeping, abandon the branch. With a full beam this is admissible (never
        // drops the optimum); under a narrow beam the beam itself, not this bound, makes it approximate.
        if (UpperBoundScore(dAcc, offroadAcc, elevAcc) <= _pruneScore)
            return;

        if (!_adjacency.TryGetValue(current.Id, out var neighbors))
            return;

        TryClose(current, neighbors, dAcc, offroadAcc, elevAcc);

        var lastHeading = _path.Count > 0 ? _path[^1].HeadingAngleDegrees : double.NaN;

        // Adaptive beam: only the top-_beamWidth neighbours (best-first order) are expanded.
        foreach (var (edge, neighbor) in OrderNeighbours(current, neighbors, dAcc).Take(_beamWidth))
        {
            if (_visited.Contains(neighbor.Id)) continue;

            // Reachability bound: a point farther than Lmax/2 from start can't sit on a valid loop
            // (the loop would have to be at least twice that long).
            if (GeoCalculator.CalculateDistance(_start, neighbor.Coordinate) > _lMax / 2)
                continue;

            var newDAcc = dAcc + edge.LengthMeters;

            // Return-home bound: road distance home >= straight-line home, so if the Euclidean lower
            // bound already overshoots the budget, no completion can close in time.
            if (newDAcc + GeoCalculator.CalculateDistance(neighbor.Coordinate, _start) > _lMax)
                continue;

            var departureBearing = GetDepartureBearing(edge, current, neighbor);

            // Reject hairpins.
            if (_path.Count > 0 && GeoCalculator.GetAngleDifference(lastHeading, departureBearing) > SkeletonSearchSettings.MaxTurnAngleDegrees)
                continue;

            var arrivalBearing = GetArrivalBearing(edge, current, neighbor);

            _path.Add(new SkeletonVector(current, neighbor, edge.LengthMeters, arrivalBearing, edge));
            _visited.Add(neighbor.Id);

            Expand(
                neighbor,
                newDAcc,
                offroadAcc + edge.OffroadLengthMeters,
                elevAcc + edge.ElevationGainMeters);

            _path.RemoveAt(_path.Count - 1);
            _visited.Remove(neighbor.Id);

            if (_budgetExceeded) return;
        }
    }

    /// <summary>
    /// Records the loop formed by closing the current path straight back to the start, if that closing
    /// edge exists, keeps the total within tolerance and doesn't hairpin. Does not stop the branch -
    /// extending further forms different, longer loops still worth exploring.
    /// </summary>
    private void TryClose(Node current, List<(Edge Edge, Node Neighbor)> neighbors, double dAcc, double offroadAcc, double elevAcc)
    {
        if (_path.Count < 2 || dAcc < _target * SkeletonSearchSettings.MinClosureFraction)
            return;

        var closing = neighbors.FirstOrDefault(n => n.Neighbor.Id == _startNode.Id);
        if (closing.Edge is null)
            return;

        var total = dAcc + closing.Edge.LengthMeters;
        if (total < _lMin || total > _lMax)
            return;

        var closingDeparture = GetDepartureBearing(closing.Edge, current, _startNode);
        if (GeoCalculator.GetAngleDifference(_path[^1].HeadingAngleDegrees, closingDeparture) > SkeletonSearchSettings.MaxTurnAngleDegrees)
            return;

        var totalOffroad = offroadAcc + closing.Edge.OffroadLengthMeters;
        var totalElev = elevAcc + closing.Edge.ElevationGainMeters;
        var score = ScoreLoop(total, totalOffroad, totalElev, Roundness());

        // Too weak to keep: below the diversity floor (best-so-far minus the margin).
        if (score <= _pruneScore)
            return;

        var closingArrival = GetArrivalBearing(closing.Edge, current, _startNode);
        var chain = new List<SkeletonVector>(_path)
        {
            new(current, _startNode, closing.Edge.LengthMeters, closingArrival, closing.Edge)
        };

        Consider(chain, score, new HashSet<long>(_path.Select(v => v.ToNode.Id)));
    }

    /// <summary>
    /// Inserts a completed loop into the kept set of best non-overlapping loops. A loop that overlaps an
    /// already-kept better one is dropped; otherwise it replaces every overlapping weaker loop, and the
    /// set is capped to <see cref="SkeletonSearchSettings.MaxLoopsPerArena"/> by dropping the weakest.
    /// </summary>
    private void Consider(List<SkeletonVector> chain, double score, HashSet<long> nodes)
    {
        var overlapping = _kept.Where(k => Overlaps(k.Nodes, nodes)).ToList();
        if (overlapping.Any(k => k.Score >= score))
            return;

        foreach (var beaten in overlapping)
            _kept.Remove(beaten);

        _kept.Add(new KeptLoop(chain, score, nodes));

        // Drop loops now too far below the best (raising the pruning floor), then cap to the K best.
        var best = _kept.Max(k => k.Score);
        _kept.RemoveAll(k => k.Score < best - SkeletonSearchSettings.DiversityScoreMargin);
        while (_kept.Count > SkeletonSearchSettings.MaxLoopsPerArena)
            _kept.Remove(_kept.MinBy(k => k.Score)!);

        _pruneScore = best - SkeletonSearchSettings.DiversityScoreMargin;
    }

    // Two loops overlap when they share more than LoopOverlapFraction of the smaller loop's interior
    // nodes (start is excluded - it is common to every loop from this entrance).
    private static bool Overlaps(HashSet<long> a, HashSet<long> b)
    {
        var (smaller, larger) = a.Count <= b.Count ? (a, b) : (b, a);
        if (smaller.Count == 0)
            return false;

        var shared = smaller.Count(larger.Contains);
        return shared > SkeletonSearchSettings.LoopOverlapFraction * smaller.Count;
    }

    /// <summary>
    /// Orders neighbours best-first to find a good loop early (which tightens the score bound): prefer
    /// offroad edges, and prefer hops whose radial in/out direction matches a loop's ideal sweep - heading
    /// outward from start early, back toward it late. Ordering only; it never drops a neighbour.
    /// </summary>
    private IEnumerable<(Edge Edge, Node Neighbor)> OrderNeighbours(Node current, List<(Edge Edge, Node Neighbor)> neighbors, double dAcc)
    {
        var progress = Math.Clamp(dAcc / _target, 0, 1);
        var desiredRadial = Math.Cos(progress * Math.PI); // +1 outward at start, -1 homeward near target
        var outwardBearing = _path.Count > 0 ? GeoCalculator.CalculateBearing(_start, current.Coordinate) : double.NaN;

        return neighbors
            .Select(n =>
            {
                var key = n.Edge.IsOffroad ? SkeletonSearchSettings.OffroadOrderingBonus : 0.0;
                if (_path.Count > 0)
                {
                    var toNeighbour = GeoCalculator.CalculateBearing(current.Coordinate, n.Neighbor.Coordinate);
                    var radialAngle = GeoCalculator.GetAngleDifference(outwardBearing, toNeighbour);
                    var actualRadial = Math.Cos(radialAngle * Math.PI / 180.0);
                    key -= SkeletonSearchSettings.RadialOrderingWeight * Math.Pow(actualRadial - desiredRadial, 2);
                }
                return (n, key);
            })
            .OrderByDescending(x => x.key)
            .Select(x => x.n);
    }

    // Optimistic (admissible) upper bound on the score of ANY completion of the current path: assume the
    // whole remaining budget up to Lmax is offroad, the loop lands exactly on target, there is no further
    // climbing, and it closes into a perfect circle. Real completions can only score lower.
    private double UpperBoundScore(double dAcc, double offroadAcc, double elevAcc)
    {
        var offroadUpperBound = SkeletonSearchSettings.OffroadScoreWeight * (offroadAcc + (_lMax - dAcc)) / _lMax;
        return offroadUpperBound - SkeletonSearchSettings.ElevationPenaltyPerMeter * elevAcc + SkeletonSearchSettings.RoundnessScoreWeight;
    }

    private double ScoreLoop(double totalDistance, double offroadDistance, double elevationGain, double roundness)
    {
        var offroad = SkeletonSearchSettings.OffroadScoreWeight * (offroadDistance / totalDistance);
        var elevationPenalty = SkeletonSearchSettings.ElevationPenaltyPerMeter * elevationGain;
        var distancePenalty = SkeletonSearchSettings.DistanceDeviationPenaltyWeight * Math.Abs(totalDistance - _target) / _target;
        var roundnessReward = SkeletonSearchSettings.RoundnessScoreWeight * roundness;
        return offroad - elevationPenalty - distancePenalty + roundnessReward;
    }

    // Roundness of the skeleton polygon (start -> each visited node -> start), 1 = circle, ~0 = sliver.
    private double Roundness()
    {
        var polygon = new List<Coordinate> { _start };
        foreach (var v in _path)
            polygon.Add(v.ToNode.Coordinate);

        return GeoCalculator.CalculateRoundness(polygon);
    }

    /// <summary>
    /// The heading as you depart <paramref name="fromNode"/> onto <paramref name="edge"/> - the true
    /// initial bearing of the edge's own geometry near the junction, not a straight chord to
    /// <paramref name="toNode"/>. Falls back to the node-to-node chord when the edge has no geometry.
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
    /// final bearing of the edge's own geometry near the junction, used as the next hop's reference heading.
    /// </summary>
    private static double GetArrivalBearing(Edge edge, Node fromNode, Node toNode)
    {
        if (edge.Geometry.Count < 2)
            return GeoCalculator.CalculateBearing(fromNode.Coordinate, toNode.Coordinate);

        var traversedInReverse = fromNode.Id == edge.TargetNodeId;
        return GeoCalculator.CalculateArrivalBearing(edge.Geometry, traversedInReverse);
    }

    private static LoopSkeleton ToLoopSkeleton(Node startNode, List<SkeletonVector> chain)
    {
        // Exclude the closing vector - its ToNode is startNode itself, already covered by Entrance.
        var waypoints = chain.Take(chain.Count - 1).Select(v => v.ToNode.Coordinate).ToList();
        var totalMeters = chain.Sum(v => v.EdgeLengthMeters);

        // Full traversed path = each edge's real geometry, oriented in the direction of travel and
        // concatenated. Handing this to the routing provider (instead of only the junctions) forces it
        // onto the exact roads the search chose, so the routed loop matches the designed one.
        var geometry = new List<Coordinate> { startNode.Coordinate };
        foreach (var v in chain)
        {
            var edgeGeom = v.Edge.Geometry;
            if (edgeGeom.Count < 2)
            {
                geometry.Add(v.ToNode.Coordinate); // synthetic/geometry-less edge (e.g. tests)
                continue;
            }

            // Edge geometry is stored Source -> Target; reverse it when we traverse Target -> Source.
            var traversedInReverse = v.FromNode.Id == v.Edge.TargetNodeId;
            var oriented = traversedInReverse ? edgeGeom.Reverse() : edgeGeom;
            geometry.AddRange(oriented.Skip(1)); // skip first vertex - duplicates the previous point
        }

        return new LoopSkeleton(
            Entrance: startNode.Coordinate,
            Waypoints: waypoints,
            Geometry: geometry,
            TraveledDistanceKm: (float)(totalMeters / 1000.0));
    }
}
