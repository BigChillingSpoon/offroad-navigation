using System.Diagnostics;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Skeletons.Models;
using Routing.Application.Planning.Skeletons.Services;
using Routing.Domain.Entities;
using Routing.Domain.Utilities;
using Routing.Domain.ValueObjects;
using LoopSkeleton = Routing.Application.Planning.Skeletons.Models.LoopSkeleton;

/// <summary>
/// Finds the single best loop skeleton from an entrance with a branch-and-bound search over the real
/// GIS graph. Instead of enumerating every loop and sorting afterwards, it scores incrementally and
/// prunes any branch that provably cannot beat the best loop found so far, using only admissible
/// (bullet-proof) cuts so the returned loop is the true optimum for the entrance.
/// </summary>
public class LoopSkeletonFinder : ILoopSkeletonFinder
{
    // Turn/U-turn validity: a loop that doubles back more sharply than this at a junction is not an
    // acceptable loop, so "best" means the best among turn-valid loops. (S4, a domain rule.)
    private const double MaxTurnAngleDegrees = 120.0;

    // Don't attempt to close the loop before we've travelled a meaningful part of the target - avoids
    // degenerate tiny loops. (S7.)
    private const double MinClosureFraction = 0.5;

    private static readonly double MinTargetFraction = LoopDistanceTolerance.MinFraction; // 0.85
    private static readonly double MaxTargetFraction = LoopDistanceTolerance.MaxFraction; // 1.15

    // In-search score weights, mirroring the SHAPE of LoopCandidateScorer (offroad ratio dominates,
    // elevation a small penalty, distance deviation a modest penalty). Only used to rank skeletons so
    // the best one is sent to GraphHopper; the pipeline re-scores the real routed result afterwards.
    private const double OffroadScoreWeight = 100.0;
    private const double ElevationPenaltyPerMeter = 0.01;
    private const double DistanceDeviationPenaltyWeight = 50.0;

    // Rewards a round loop over a thin/pinched "out-and-back" one via the isoperimetric quotient
    // Q = 4*pi*Area / Perimeter^2 of the skeleton's node polygon (1 = perfect circle, ~0 = sliver).
    // Offroad ratio still dominates; this mainly breaks ties toward the rounder shape and pushes the
    // search away from slivers. A rounder skeleton also routes more tightly through GraphHopper, so it
    // is less likely to be inflated out of the distance band downstream.
    private const double RoundnessScoreWeight = 40.0;

    // Neighbour-ordering weights (S3 vortex shape + S5 offroad preference). Ordering ONLY affects how
    // fast a good loop is found (which tightens the S1 bound) - never which loops are valid, so it
    // cannot change the optimum.
    private const double OffroadOrderingBonus = 1.0;
    private const double RadialOrderingWeight = 1.0;

    // S8 anti-hang budget: on a pathological mesh the search stops and returns the best loop found so
    // far rather than running unbounded. Generous, so it never triggers on normal arenas.
    private const long MaxNodeExpansions = 3_000_000;
    // Per-arena latency cap. The adaptive beam lets mid-size arenas finish well under this; the very
    // largest loops (15km+, whose beam^depth space can't be exhausted) return their best-so-far here,
    // so this is effectively the "fast" guarantee for big loops.
    private static readonly TimeSpan TimeBudget = TimeSpan.FromSeconds(5);

    // Adaptive beam: expand only the top-K neighbours per node (by the vortex/offroad ordering), with K
    // scaling smoothly with arena size - K = clamp(round(BeamMaxWidth * BeamScaleNodes /
    // (nodeCount + BeamScaleNodes)), BeamMinWidth, BeamMaxWidth). Small arenas keep K high (>= typical
    // degree, so the search stays effectively exhaustive/optimal); big dense arenas drop to a narrow,
    // fast guided beam (near-optimal). No length threshold - the branching just tightens as the graph
    // grows, which is what makes big-loop searches fast.
    private const int BeamMaxWidth = 6;
    private const int BeamMinWidth = 2;
    private const double BeamScaleNodes = 150.0;

    // B: keep up to this many spatially-distinct (non-overlapping) loops per arena, so one entrance can
    // yield several separate loops (e.g. on opposite sides of a road through a forest) rather than only
    // the single best. Two loops count as overlapping - and only the higher-scored is kept - when they
    // share more than LoopOverlapFraction of the smaller loop's interior nodes.
    private const int MaxLoopsPerArena = 4;
    private const double LoopOverlapFraction = 0.5;

    // Once any loop is found, branches whose optimistic score can't come within this margin of the best
    // loop so far are pruned (and kept loops that fall this far below the best are dropped). This is
    // what bounds deep big-loop searches: without it, keeping several loops means no branch-and-bound
    // pruning until the kept set is full, so a deep arena explores exponentially before any bound bites.
    // The score scale is ~0-140 (offroad up to 100 + roundness up to 40), so 30 keeps genuinely
    // different loops while still cutting the long tail of clearly-worse ones.
    private const double DiversityScoreMargin = 30.0;

    public IReadOnlyList<LoopSkeleton> FindSkeletons(
        Node startNode,
        IReadOnlyList<Node> nodesInArea,
        IReadOnlyList<Edge> edgesInArea,
        double targetLoopDistanceMeters,
        SkeletonSearchOptions options)
    {
        var nodesById = nodesInArea.ToDictionary(n => n.Id);
        var adjacency = BuildGraph(edgesInArea, nodesById, options);

        // Per-call state kept off the finder instance: FindSkeletons runs concurrently across arenas
        // (Task.WhenAll in the generator), so a shared LoopSkeletonFinder must stay stateless.
        var search = new LoopSearch(startNode, adjacency, targetLoopDistanceMeters);
        return search.Run();
    }

    /// <summary>
    /// Builds a bidirectional adjacency list from the raw edges, dropping any edge the search options
    /// exclude (H1: barriers, no-entry, restricted zones, and grade above the vehicle limit). Edges
    /// traverse both directions - direction-of-travel is governed by HasNoEntry, not Source/Target.
    /// </summary>
    private static Dictionary<long, List<(Edge Edge, Node Neighbor)>> BuildGraph(
        IReadOnlyList<Edge> edges,
        Dictionary<long, Node> nodesById,
        SkeletonSearchOptions options)
    {
        var adjacency = new Dictionary<long, List<(Edge, Node)>>();

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
            // Single "avoid private/restricted" switch, mirroring GraphHopper's custom model exactly:
            // private/forestry access (HasNoEntry ~ road_access) plus national-park TRACKS (IsRestricted
            // && road_class == TRACK). Non-track roads through a park stay routable, as in GH.
            if (!options.AllowPrivateRoads &&
                (edge.HasNoEntry || (edge.IsRestricted && edge.Highway == "track")))
                continue;
            if (edge.Grade > options.MaxGrade) continue; // H1: vehicle can't take this grade

            AddDirected(edge.SourceNodeId, edge.TargetNodeId, edge);
            AddDirected(edge.TargetNodeId, edge.SourceNodeId, edge);
        }

        return adjacency;
    }

    /// <summary>
    /// One branch-and-bound traversal from a single entrance. Holds all mutable state for the call so
    /// the finder itself stays thread-safe.
    /// </summary>
    private sealed class LoopSearch
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

        // B: the best non-overlapping loops found so far (at most MaxLoopsPerArena). Pruning uses the
        // weakest kept score once the set is full - a branch is abandoned only when it cannot beat even
        // the worst loop we are keeping.
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
            _lMin = target * MinTargetFraction;
            _lMax = target * MaxTargetFraction;
            _visited = new HashSet<long> { startNode.Id };

            // Adaptive beam width from the arena's node count (see BeamMaxWidth notes).
            var nodeCount = adjacency.Count;
            _beamWidth = Math.Clamp(
                (int)Math.Round(BeamMaxWidth * BeamScaleNodes / (nodeCount + BeamScaleNodes)),
                BeamMinWidth,
                BeamMaxWidth);
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

            // S8: stop and keep best-so-far on a pathological arena.
            if (++_expansions > MaxNodeExpansions || _stopwatch.Elapsed > TimeBudget)
            {
                _budgetExceeded = true;
                return;
            }

            // S1: branch-and-bound. If even the most optimistic completion of this partial path (all
            // remaining budget offroad, lands exactly on target, no further climbing, perfect roundness)
            // cannot beat the weakest loop we are keeping, abandon the branch. With a full beam this is
            // admissible (never drops the optimum); under a narrow beam the beam itself, not this bound,
            // is what makes the search approximate.
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

                // H4 (Holy Circle): a point farther than Lmax/2 from start can't sit on a valid loop
                // (the loop would have to be at least twice that long).
                if (GeoCalculator.CalculateDistance(_start, neighbor.Coordinate) > _lMax / 2)
                    continue;

                var newDAcc = dAcc + edge.LengthMeters;

                // H5 (return-home fuel bound): road distance home >= straight-line home, so if the
                // Euclidean lower bound already overshoots the budget, no completion can close in time.
                if (newDAcc + GeoCalculator.CalculateDistance(neighbor.Coordinate, _start) > _lMax)
                    continue;

                var departureBearing = GetDepartureBearing(edge, current, neighbor);

                // S4: reject hairpins.
                if (_path.Count > 0 && GeoCalculator.GetAngleDifference(lastHeading, departureBearing) > MaxTurnAngleDegrees)
                    continue;

                var arrivalBearing = GetArrivalBearing(edge, current, neighbor);

                _path.Add(new SkeletonVector(current, neighbor, edge.LengthMeters, arrivalBearing));
                _visited.Add(neighbor.Id);

                Expand(
                    neighbor,
                    newDAcc,
                    offroadAcc + (edge.IsOffroad ? edge.LengthMeters : 0),
                    elevAcc + edge.ElevationGainMeters);

                _path.RemoveAt(_path.Count - 1);
                _visited.Remove(neighbor.Id);

                if (_budgetExceeded) return;
            }
        }

        /// <summary>
        /// Records the loop formed by closing the current path straight back to the start, if that
        /// closing edge exists, keeps the total within tolerance (H6) and doesn't hairpin (S4). Does
        /// not stop the branch - extending further forms different, longer loops still worth exploring.
        /// </summary>
        private void TryClose(Node current, List<(Edge Edge, Node Neighbor)> neighbors, double dAcc, double offroadAcc, double elevAcc)
        {
            if (_path.Count < 2 || dAcc < _target * MinClosureFraction)
                return;

            var closing = neighbors.FirstOrDefault(n => n.Neighbor.Id == _startNode.Id);
            if (closing.Edge is null)
                return;

            var total = dAcc + closing.Edge.LengthMeters;
            if (total < _lMin || total > _lMax)
                return;

            var closingDeparture = GetDepartureBearing(closing.Edge, current, _startNode);
            if (GeoCalculator.GetAngleDifference(_path[^1].HeadingAngleDegrees, closingDeparture) > MaxTurnAngleDegrees)
                return;

            var totalOffroad = offroadAcc + (closing.Edge.IsOffroad ? closing.Edge.LengthMeters : 0);
            var totalElev = elevAcc + closing.Edge.ElevationGainMeters;
            var score = ScoreLoop(total, totalOffroad, totalElev, Roundness());

            // Too weak to keep: below the diversity floor (best-so-far minus the margin), so it can
            // never enter or improve the kept set.
            if (score <= _pruneScore)
                return;

            var closingArrival = GetArrivalBearing(closing.Edge, current, _startNode);
            var chain = new List<SkeletonVector>(_path)
            {
                new(current, _startNode, closing.Edge.LengthMeters, closingArrival)
            };

            Consider(chain, score, new HashSet<long>(_path.Select(v => v.ToNode.Id)));
        }

        /// <summary>
        /// Inserts a completed loop into the kept set of best non-overlapping loops. A loop that
        /// overlaps an already-kept better one is dropped; otherwise it replaces every overlapping
        /// weaker loop, and the set is capped to <see cref="MaxLoopsPerArena"/> by dropping the weakest.
        /// </summary>
        private void Consider(List<SkeletonVector> chain, double score, HashSet<long> nodes)
        {
            var overlapping = _kept.Where(k => Overlaps(k.Nodes, nodes)).ToList();
            if (overlapping.Any(k => k.Score >= score))
                return;

            foreach (var beaten in overlapping)
                _kept.Remove(beaten);

            _kept.Add(new KeptLoop(chain, score, nodes));

            // Drop loops that are now too far below the best (raising the pruning floor), then cap to
            // the K best non-overlapping.
            var best = _kept.Max(k => k.Score);
            _kept.RemoveAll(k => k.Score < best - DiversityScoreMargin);
            while (_kept.Count > MaxLoopsPerArena)
                _kept.Remove(_kept.MinBy(k => k.Score)!);

            _pruneScore = best - DiversityScoreMargin;
        }

        // Two loops overlap when they share more than LoopOverlapFraction of the smaller loop's
        // interior nodes (start is excluded - it is common to every loop from this entrance).
        private static bool Overlaps(HashSet<long> a, HashSet<long> b)
        {
            var (smaller, larger) = a.Count <= b.Count ? (a, b) : (b, a);
            if (smaller.Count == 0)
                return false;

            var shared = smaller.Count(larger.Contains);
            return shared > LoopOverlapFraction * smaller.Count;
        }

        /// <summary>
        /// Orders neighbours best-first to find a good loop early (which tightens the S1 bound):
        /// prefer offroad edges, and prefer hops whose radial in/out direction matches a loop's ideal
        /// sweep - heading outward from start early, back toward it late. Ordering only; it never drops
        /// a neighbour, so it cannot change which loop is optimal.
        /// </summary>
        private IEnumerable<(Edge Edge, Node Neighbor)> OrderNeighbours(Node current, List<(Edge Edge, Node Neighbor)> neighbors, double dAcc)
        {
            var progress = Math.Clamp(dAcc / _target, 0, 1);
            var desiredRadial = Math.Cos(progress * Math.PI); // +1 outward at start, -1 homeward near target
            var outwardBearing = _path.Count > 0 ? GeoCalculator.CalculateBearing(_start, current.Coordinate) : double.NaN;

            return neighbors
                .Select(n =>
                {
                    var key = n.Edge.IsOffroad ? OffroadOrderingBonus : 0.0;
                    if (_path.Count > 0)
                    {
                        var toNeighbour = GeoCalculator.CalculateBearing(current.Coordinate, n.Neighbor.Coordinate);
                        var radialAngle = GeoCalculator.GetAngleDifference(outwardBearing, toNeighbour);
                        var actualRadial = Math.Cos(radialAngle * Math.PI / 180.0);
                        key -= RadialOrderingWeight * Math.Pow(actualRadial - desiredRadial, 2);
                    }
                    return (n, key);
                })
                .OrderByDescending(x => x.key)
                .Select(x => x.n);
        }

        // Optimistic (admissible) upper bound on the score of ANY completion of the current path:
        // assume the whole remaining budget up to Lmax is offroad, the loop lands exactly on target
        // (zero distance penalty), there is no further climbing, and it closes into a perfect circle
        // (roundness = 1). Real completions can only score lower, so pruning on this never removes the
        // optimum.
        private double UpperBoundScore(double dAcc, double offroadAcc, double elevAcc)
        {
            var offroadUpperBound = OffroadScoreWeight * (offroadAcc + (_lMax - dAcc)) / _lMax;
            return offroadUpperBound - ElevationPenaltyPerMeter * elevAcc + RoundnessScoreWeight;
        }

        private double ScoreLoop(double totalDistance, double offroadDistance, double elevationGain, double roundness)
        {
            var offroad = OffroadScoreWeight * (offroadDistance / totalDistance);
            var elevationPenalty = ElevationPenaltyPerMeter * elevationGain;
            var distancePenalty = DistanceDeviationPenaltyWeight * Math.Abs(totalDistance - _target) / _target;
            var roundnessReward = RoundnessScoreWeight * roundness;
            return offroad - elevationPenalty - distancePenalty + roundnessReward;
        }

        // Isoperimetric quotient of the closed skeleton polygon (start -> each visited node -> start):
        // 4*pi*Area / Perimeter^2, in [0,1]. Uses a local equirectangular projection to meters; the
        // shoelace area and the straight-line perimeter are both translation-invariant, so projecting
        // relative to the start point is exact enough for a shape score.
        private double Roundness()
        {
            // Polygon vertices: the start, then every node reached along the path (the last being the
            // current node); the closing edge back to start closes the ring.
            var lat0 = _start.Latitude * Math.PI / 180.0;
            const double metersPerDegLat = 111132.92;
            var metersPerDegLon = 111412.84 * Math.Cos(lat0);

            double Px(Coordinate c) => (c.Longitude - _start.Longitude) * metersPerDegLon;
            double Py(Coordinate c) => (c.Latitude - _start.Latitude) * metersPerDegLat;

            var verts = new List<Coordinate> { _start };
            foreach (var v in _path)
                verts.Add(v.ToNode.Coordinate);

            if (verts.Count < 3)
                return 0;

            double area2 = 0, perimeter = 0;
            for (var i = 0; i < verts.Count; i++)
            {
                var a = verts[i];
                var b = verts[(i + 1) % verts.Count];
                double ax = Px(a), ay = Py(a), bx = Px(b), by = Py(b);
                area2 += ax * by - bx * ay;
                perimeter += Math.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay));
            }

            if (perimeter <= 0)
                return 0;

            var area = Math.Abs(area2) / 2.0;
            return Math.Clamp(4.0 * Math.PI * area / (perimeter * perimeter), 0, 1);
        }
    }

    /// <summary>
    /// The heading as you depart <paramref name="fromNode"/> onto <paramref name="edge"/> - the true
    /// initial bearing of the edge's own geometry near the junction, not a straight chord to
    /// <paramref name="toNode"/>. Falls back to the node-to-node chord when the edge has no stored
    /// geometry (e.g. synthetic edges in tests).
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
    /// final bearing of the edge's own geometry near the junction, stored so the next hop's turn-angle
    /// check compares against the real direction of travel.
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

        return new LoopSkeleton(
            Entrance: startNode.Coordinate,
            Waypoints: waypoints,
            TraveledDistanceKm: (float)(totalMeters / 1000.0));
    }
}
