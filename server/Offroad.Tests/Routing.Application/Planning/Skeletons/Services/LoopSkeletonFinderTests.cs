using Routing.Application.Planning.Skeletons.Models;
using Routing.Application.Planning.Skeletons.Services;
using Routing.Domain.Entities;
using Routing.Domain.ValueObjects;

namespace Offroad.Tests.Routing.Application.Planning.Skeletons.Services;

public class LoopSkeletonFinderTests
{
    // A closed 4-node square with no chords: Start -> A -> B -> C -> Start. It is the only loop, so
    // the branch-and-bound search must return exactly one best skeleton for it.
    private const long StartId = 1;
    private const long AId = 2;
    private const long BId = 3;
    private const long CId = 4;
    private const double EdgeLengthMeters = 1000;
    private const double TargetLoopDistanceMeters = 4 * EdgeLengthMeters;

    private readonly LoopSkeletonFinder _sut = new();

    private static SkeletonSearchOptions AllowAll(byte maxGrade = 5) =>
        new(AllowGates: true, AllowPrivateRoads: true, MaxGrade: maxGrade);

    // The plain closed square whose only loop is Start -> A -> B -> C -> Start, with the A->B edge
    // swapped for the supplied one. Admitting or dropping that single edge decides between exactly one
    // skeleton and none, which is what the access-rule tests below assert.
    private static (Node Start, List<Node> Nodes, List<Edge> Edges) SquareWithAbEdge(Edge abEdge)
    {
        var startNode = new Node(StartId, new Coordinate(50.000, 14.000), isEntryPoint: true);
        var nodeA = new Node(AId, new Coordinate(50.001, 14.000), isEntryPoint: false);
        var nodeB = new Node(BId, new Coordinate(50.001, 14.001), isEntryPoint: false);
        var nodeC = new Node(CId, new Coordinate(50.000, 14.001), isEntryPoint: false);
        var nodes = new List<Node> { startNode, nodeA, nodeB, nodeC };

        var edges = new List<Edge>
        {
            new(101, StartId, AId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: true),
            abEdge,
            new(103, BId, CId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: true),
            new(104, CId, StartId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: true),
        };

        return (startNode, nodes, edges);
    }

    private static SkeletonSearchOptions Options(bool allowGates = true, bool allowPrivateRoads = true) =>
        new(AllowGates: allowGates, AllowPrivateRoads: allowPrivateRoads);

    [Fact]
    public void FindSkeletons_SingleSquareLoop_ReturnsOneSkeleton()
    {
        // Arrange
        var startNode = new Node(StartId, new Coordinate(50.000, 14.000), isEntryPoint: true);
        var nodeA = new Node(AId, new Coordinate(50.001, 14.000), isEntryPoint: false);
        var nodeB = new Node(BId, new Coordinate(50.001, 14.001), isEntryPoint: false);
        var nodeC = new Node(CId, new Coordinate(50.000, 14.001), isEntryPoint: false);
        var nodes = new List<Node> { startNode, nodeA, nodeB, nodeC };

        var edges = new List<Edge>
        {
            new(101, StartId, AId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: true),
            new(102, AId, BId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: true),
            new(103, BId, CId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: true),
            new(104, CId, StartId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: true),
        };

        // Act
        var result = _sut.FindSkeletons(startNode, nodes, edges, TargetLoopDistanceMeters, AllowAll());

        // Assert - the forward and backward traversals are the same physical loop with an identical
        // score (elevation gain is direction-agnostic), so the best-of-both is a single skeleton.
        Assert.Single(result);
    }

    [Fact]
    public void FindSkeletons_SingleSquareLoop_WaypointsExcludeEntrance()
    {
        // Arrange - same square as above.
        var startNode = new Node(StartId, new Coordinate(50.000, 14.000), isEntryPoint: true);
        var nodeA = new Node(AId, new Coordinate(50.001, 14.000), isEntryPoint: false);
        var nodeB = new Node(BId, new Coordinate(50.001, 14.001), isEntryPoint: false);
        var nodeC = new Node(CId, new Coordinate(50.000, 14.001), isEntryPoint: false);
        var nodes = new List<Node> { startNode, nodeA, nodeB, nodeC };

        var edges = new List<Edge>
        {
            new(101, StartId, AId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: true),
            new(102, AId, BId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: true),
            new(103, BId, CId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: true),
            new(104, CId, StartId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: true),
        };

        // Act
        var result = _sut.FindSkeletons(startNode, nodes, edges, TargetLoopDistanceMeters, AllowAll());

        // Assert - only the 3 intermediate stops, never a trailing duplicate of Entrance.
        var skeleton = Assert.Single(result);
        Assert.Equal(3, skeleton.Waypoints.Count);
        Assert.DoesNotContain(skeleton.Waypoints, w => w == skeleton.Entrance);
    }

    /// <summary>
    /// Regression test for a real-world miss: a curved edge whose real tangent near the shared node
    /// continues almost straight (small turn) got wrongly rejected because turn angle was computed from
    /// the straight chord between the edge's two endpoint NODES, ignoring the edge's own interior
    /// geometry. The real departure tangent (Start->A->mid) is only a 20 degree turn (must be allowed),
    /// while the old whole-edge chord (A straight to B) is a 141 degree turn (would be wrongly rejected).
    /// </summary>
    [Fact]
    public void FindSkeletons_CurvedEdgeWithGentleRealTangent_IsNotRejectedByStraightChordBearing()
    {
        // Arrange
        const long startId = 1;
        const long aId = 2;
        const long bId = 3;

        var start = new Coordinate(50.000, 14.000);
        var a = new Coordinate(50.001, 14.000);
        var mid = new Coordinate(50.00146984631039, 14.000266049977006);
        var b = new Coordinate(50.00087386659606, 14.000158171167607);

        var startNode = new Node(startId, start, isEntryPoint: true);
        var nodeA = new Node(aId, a, isEntryPoint: false);
        var nodeB = new Node(bId, b, isEntryPoint: false);
        var nodes = new List<Node> { startNode, nodeA, nodeB };

        var edges = new List<Edge>
        {
            // Straight: Start -> A.
            new(301, startId, aId, 111.19,
                hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: true,
                geometry: new List<Coordinate> { start, a }),

            // Curved: A -> B via an interior vertex ("mid") that keeps the real departure tangent
            // near A close to a straight continuation, even though the edge swings sharply afterwards.
            new(302, aId, bId, 122.31,
                hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: true,
                geometry: new List<Coordinate> { a, mid, b }),

            // Closing: B -> Start, colinear with the curve's arrival tangent (zero closing turn).
            new(303, bId, startId, 97.82,
                hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: true,
                geometry: new List<Coordinate> { b, start }),
        };

        const double targetLoopDistanceMeters = 111.19 + 122.31 + 97.82;

        // Act
        var result = _sut.FindSkeletons(startNode, nodes, edges, targetLoopDistanceMeters, AllowAll());

        // Assert - the A->B edge must be taken despite its sharp whole-edge chord, because the real
        // tangent at the A end is a gentle 20 degree turn, not the chord's 141 degrees.
        Assert.Single(result);
    }

    [Fact]
    public void FindSkeletons_OnlyLoopUsesTooHighGrade_IsExcludedByVehicleLimit()
    {
        // Arrange - the same square, but the A->B edge is a grade-5 track. A vehicle limited to grade 3
        // cannot take it, which severs the only loop (H1), so no skeleton should be returned.
        var startNode = new Node(StartId, new Coordinate(50.000, 14.000), isEntryPoint: true);
        var nodeA = new Node(AId, new Coordinate(50.001, 14.000), isEntryPoint: false);
        var nodeB = new Node(BId, new Coordinate(50.001, 14.001), isEntryPoint: false);
        var nodeC = new Node(CId, new Coordinate(50.000, 14.001), isEntryPoint: false);
        var nodes = new List<Node> { startNode, nodeA, nodeB, nodeC };

        var edges = new List<Edge>
        {
            new(101, StartId, AId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: true),
            new(102, AId, BId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: true, geometry: null, grade: 5),
            new(103, BId, CId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: true),
            new(104, CId, StartId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: true),
        };

        // Act
        var restricted = _sut.FindSkeletons(startNode, nodes, edges, TargetLoopDistanceMeters, AllowAll(maxGrade: 3));
        var permitted = _sut.FindSkeletons(startNode, nodes, edges, TargetLoopDistanceMeters, AllowAll(maxGrade: 5));

        // Assert - grade cap 3 breaks the loop; cap 5 lets it through.
        Assert.Empty(restricted);
        Assert.Single(permitted);
    }

    [Fact]
    public void FindSkeletons_TwoLoopsFromStart_ReturnsTheMoreOffroadOne()
    {
        // Arrange - two separate square loops sharing only the Start node: a northern OFFROAD square
        // and a south-western ASPHALT square, both the same length. The score is offroad-ratio driven,
        // so the finder must return the offroad loop.
        var start = new Coordinate(50.000, 14.000);
        var startNode = new Node(StartId, start, isEntryPoint: true);

        // Offroad square (north).
        var p1 = new Coordinate(50.001, 14.000);
        var p2 = new Coordinate(50.001, 14.001);
        var p3 = new Coordinate(50.000, 14.001);
        var nP1 = new Node(10, p1, false);
        var nP2 = new Node(11, p2, false);
        var nP3 = new Node(12, p3, false);

        // Asphalt square (south-west).
        var q1 = new Coordinate(49.999, 14.000);
        var q2 = new Coordinate(49.999, 13.999);
        var q3 = new Coordinate(50.000, 13.999);
        var nQ1 = new Node(20, q1, false);
        var nQ2 = new Node(21, q2, false);
        var nQ3 = new Node(22, q3, false);

        var nodes = new List<Node> { startNode, nP1, nP2, nP3, nQ1, nQ2, nQ3 };

        var edges = new List<Edge>
        {
            // Offroad loop.
            new(1, StartId, 10, EdgeLengthMeters, false, false, false, 0, isOffroad: true),
            new(2, 10, 11, EdgeLengthMeters, false, false, false, 0, isOffroad: true),
            new(3, 11, 12, EdgeLengthMeters, false, false, false, 0, isOffroad: true),
            new(4, 12, StartId, EdgeLengthMeters, false, false, false, 0, isOffroad: true),
            // Asphalt loop.
            new(5, StartId, 20, EdgeLengthMeters, false, false, false, 0, isOffroad: false),
            new(6, 20, 21, EdgeLengthMeters, false, false, false, 0, isOffroad: false),
            new(7, 21, 22, EdgeLengthMeters, false, false, false, 0, isOffroad: false),
            new(8, 22, StartId, EdgeLengthMeters, false, false, false, 0, isOffroad: false),
        };

        // Act
        var result = _sut.FindSkeletons(startNode, nodes, edges, TargetLoopDistanceMeters, AllowAll());

        // Assert - both squares are non-overlapping, but the all-asphalt one scores far below the
        // all-offroad one (well past the diversity margin), so it is dropped and only the offroad loop
        // is returned. (Two comparably-good loops would both survive; a much worse one does not.)
        var skeleton = Assert.Single(result);
        var offroadWaypoints = new HashSet<Coordinate> { p1, p2, p3 };
        Assert.All(skeleton.Waypoints, w => Assert.Contains(w, offroadWaypoints));
    }

    [Fact]
    public void FindSkeletons_TwoComparableNonOverlappingLoops_ReturnsBoth()
    {
        // Arrange - two offroad square loops sharing only the Start node (north + south-west). They
        // share no interior nodes and score comparably, so both must be returned as separate loops.
        var start = new Coordinate(50.000, 14.000);
        var startNode = new Node(StartId, start, isEntryPoint: true);

        var p1 = new Coordinate(50.001, 14.000);
        var p2 = new Coordinate(50.001, 14.001);
        var p3 = new Coordinate(50.000, 14.001);
        var q1 = new Coordinate(49.999, 14.000);
        var q2 = new Coordinate(49.999, 13.999);
        var q3 = new Coordinate(50.000, 13.999);

        var nodes = new List<Node>
        {
            startNode,
            new(10, p1, false), new(11, p2, false), new(12, p3, false),
            new(20, q1, false), new(21, q2, false), new(22, q3, false),
        };

        var edges = new List<Edge>
        {
            new(1, StartId, 10, EdgeLengthMeters, false, false, false, 0, isOffroad: true),
            new(2, 10, 11, EdgeLengthMeters, false, false, false, 0, isOffroad: true),
            new(3, 11, 12, EdgeLengthMeters, false, false, false, 0, isOffroad: true),
            new(4, 12, StartId, EdgeLengthMeters, false, false, false, 0, isOffroad: true),
            new(5, StartId, 20, EdgeLengthMeters, false, false, false, 0, isOffroad: true),
            new(6, 20, 21, EdgeLengthMeters, false, false, false, 0, isOffroad: true),
            new(7, 21, 22, EdgeLengthMeters, false, false, false, 0, isOffroad: true),
            new(8, 22, StartId, EdgeLengthMeters, false, false, false, 0, isOffroad: true),
        };

        // Act
        var result = _sut.FindSkeletons(startNode, nodes, edges, TargetLoopDistanceMeters, AllowAll());

        // Assert - two distinct non-overlapping loops from the one entrance.
        Assert.Equal(2, result.Count);
        var north = new HashSet<Coordinate> { p1, p2, p3 };
        var southWest = new HashSet<Coordinate> { q1, q2, q3 };
        Assert.Contains(result, r => r.Waypoints.All(north.Contains));
        Assert.Contains(result, r => r.Waypoints.All(southWest.Contains));
    }

    [Fact]
    public void FindSkeletons_NoEntryNonTrackEdge_IsAlwaysExcluded()
    {
        // A car-inaccessible road that is NOT a track is one the routing provider won't route, so it is
        // dropped even when private roads are allowed - severing the only loop.
        var abEdge = new Edge(102, AId, BId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: true,
            isRestricted: false, elevationGainMeters: 0, isOffroad: true, geometry: null, grade: 0, highway: "unclassified");
        var (start, nodes, edges) = SquareWithAbEdge(abEdge);

        Assert.Empty(_sut.FindSkeletons(start, nodes, edges, TargetLoopDistanceMeters, AllowAll()));
    }

    [Fact]
    public void FindSkeletons_NoEntryTrackEdge_IsKeptOnlyWhenPrivateRoadsAllowed()
    {
        // A car-inaccessible TRACK stays routable (the provider routes tracks), but it is still a private
        // road, so it is admitted only when private roads are allowed.
        var abEdge = new Edge(102, AId, BId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: true,
            isRestricted: false, elevationGainMeters: 0, isOffroad: true, geometry: null, grade: 0, highway: "track");
        var (start, nodes, edges) = SquareWithAbEdge(abEdge);

        Assert.Empty(_sut.FindSkeletons(start, nodes, edges, TargetLoopDistanceMeters, Options(allowPrivateRoads: false)));
        Assert.Single(_sut.FindSkeletons(start, nodes, edges, TargetLoopDistanceMeters, Options(allowPrivateRoads: true)));
    }

    [Fact]
    public void FindSkeletons_RestrictedParkTrack_IsExcludedUnlessPrivateRoadsAllowed()
    {
        // National-park rule mirrors the routing provider: a TRACK inside a park is blocked unless the
        // user opts into private/restricted roads.
        var abEdge = new Edge(102, AId, BId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false,
            isRestricted: true, elevationGainMeters: 0, isOffroad: true, geometry: null, grade: 0, highway: "track");
        var (start, nodes, edges) = SquareWithAbEdge(abEdge);

        Assert.Empty(_sut.FindSkeletons(start, nodes, edges, TargetLoopDistanceMeters, Options(allowPrivateRoads: false)));
        Assert.Single(_sut.FindSkeletons(start, nodes, edges, TargetLoopDistanceMeters, Options(allowPrivateRoads: true)));
    }

    [Fact]
    public void FindSkeletons_RestrictedNonTrackInPark_StaysRoutable()
    {
        // Alignment edge case: the routing provider's national-park rule blocks only the TRACK road_class,
        // so a restricted NON-track road (e.g. a gravel unclassified road - offroad, but not a track)
        // inside a park stays routable even with private roads disallowed. Keying this off IsOffroad
        // instead of IsTrack would wrongly drop it and diverge from the provider.
        var abEdge = new Edge(102, AId, BId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false,
            isRestricted: true, elevationGainMeters: 0, isOffroad: true, geometry: null, grade: 0, highway: "unclassified");
        var (start, nodes, edges) = SquareWithAbEdge(abEdge);

        Assert.Single(_sut.FindSkeletons(start, nodes, edges, TargetLoopDistanceMeters, Options(allowPrivateRoads: false)));
    }

    [Fact]
    public void FindSkeletons_NonRoutableHighwayEdge_IsExcluded()
    {
        // A class the routing provider cannot route (e.g. highway=path) is dropped, severing the only loop.
        var abEdge = new Edge(102, AId, BId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false,
            isRestricted: false, elevationGainMeters: 0, isOffroad: true, geometry: null, grade: 0, highway: "path");
        var (start, nodes, edges) = SquareWithAbEdge(abEdge);

        Assert.Empty(_sut.FindSkeletons(start, nodes, edges, TargetLoopDistanceMeters, AllowAll()));
    }

    [Fact]
    public void FindSkeletons_BarrierEdge_IsKeptOnlyWhenGatesAllowed()
    {
        // A gated edge is admitted only when the user allows gates.
        var abEdge = new Edge(102, AId, BId, EdgeLengthMeters, hasBarrier: true, hasNoEntry: false,
            isRestricted: false, elevationGainMeters: 0, isOffroad: true, geometry: null, grade: 0, highway: "track");
        var (start, nodes, edges) = SquareWithAbEdge(abEdge);

        Assert.Empty(_sut.FindSkeletons(start, nodes, edges, TargetLoopDistanceMeters, Options(allowGates: false)));
        Assert.Single(_sut.FindSkeletons(start, nodes, edges, TargetLoopDistanceMeters, Options(allowGates: true)));
    }
}
