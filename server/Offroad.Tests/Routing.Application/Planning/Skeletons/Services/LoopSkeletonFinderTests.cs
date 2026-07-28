using Routing.Application.Planning.Skeletons.Models;
using Routing.Application.Planning.Skeletons.Services;
using Routing.Domain.Entities;
using Routing.Domain.ValueObjects;

namespace Offroad.Tests.Routing.Application.Planning.Skeletons.Services;

public class LoopSkeletonFinderTests
{
    // A closed 4-node square with no chords: Start -> A -> B -> C -> Start. With no shortcuts
    // available, the DFS has exactly two possible traversals of this loop - clockwise from Start
    // via A, and counter-clockwise from Start via C - which are the same physical loop found twice.
    private const long StartId = 1;
    private const long AId = 2;
    private const long BId = 3;
    private const long CId = 4;
    private const double EdgeLengthMeters = 1000;
    private const double TargetLoopDistanceMeters = 4 * EdgeLengthMeters;

    private readonly LoopSkeletonFinder _sut = new(new SteepestDescentFirstPreference());

    [Fact]
    public void FindSkeletons_LoopFoundInBothDirections_ReturnsOnlyOneSkeleton()
    {
        // Arrange
        var startNode = new Node(StartId, new Coordinate(50.000, 14.000), isEntryPoint: true);
        var nodeA = new Node(AId, new Coordinate(50.001, 14.000), isEntryPoint: false);
        var nodeB = new Node(BId, new Coordinate(50.001, 14.001), isEntryPoint: false);
        var nodeC = new Node(CId, new Coordinate(50.000, 14.001), isEntryPoint: false);
        var nodes = new List<Node> { startNode, nodeA, nodeB, nodeC };

        var edges = new List<Edge>
        {
            new(101, StartId, AId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: false),
            new(102, AId, BId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: false),
            new(103, BId, CId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: false),
            new(104, CId, StartId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: false),
        };

        var options = new SkeletonSearchOptions(AllowGates: true, AllowPrivateRoads: true, AllowRestrictedZones: true);

        // Act
        var result = _sut.FindSkeletons(startNode, nodes, edges, TargetLoopDistanceMeters, options);

        // Assert - the forward (Start->A->B->C->Start) and backward (Start->C->B->A->Start)
        // traversals must collapse into a single skeleton, not be reported as two loops.
        Assert.Single(result);
    }

    [Fact]
    public void FindSkeletons_LoopFoundInBothDirections_WaypointsExcludeEntrance()
    {
        // Arrange - same square as above.
        var startNode = new Node(StartId, new Coordinate(50.000, 14.000), isEntryPoint: true);
        var nodeA = new Node(AId, new Coordinate(50.001, 14.000), isEntryPoint: false);
        var nodeB = new Node(BId, new Coordinate(50.001, 14.001), isEntryPoint: false);
        var nodeC = new Node(CId, new Coordinate(50.000, 14.001), isEntryPoint: false);
        var nodes = new List<Node> { startNode, nodeA, nodeB, nodeC };

        var edges = new List<Edge>
        {
            new(101, StartId, AId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: false),
            new(102, AId, BId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: false),
            new(103, BId, CId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: false),
            new(104, CId, StartId, EdgeLengthMeters, hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: false),
        };

        var options = new SkeletonSearchOptions(AllowGates: true, AllowPrivateRoads: true, AllowRestrictedZones: true);

        // Act
        var result = _sut.FindSkeletons(startNode, nodes, edges, TargetLoopDistanceMeters, options);

        // Assert - only the 3 intermediate stops (A, B, C), never a trailing duplicate of Entrance.
        var skeleton = Assert.Single(result);
        Assert.Equal(3, skeleton.Waypoints.Count);
        Assert.DoesNotContain(skeleton.Waypoints, w => w == skeleton.Entrance);
    }

    /// <summary>
    /// Regression test for a real-world miss: a curved edge whose real tangent near the shared node
    /// continues almost straight (small turn) got wrongly rejected because turn angle was computed from
    /// the straight chord between the edge's two endpoint NODES, ignoring the edge's own interior
    /// geometry - for a long/curved edge that chord can point in a very different direction than the
    /// edge's actual initial heading. Coordinates below were chosen so the real departure tangent
    /// (Start->A->mid) is only a 20 degree turn (must be allowed), while the old whole-edge chord
    /// (A straight to B) works out to a 141 degree turn (would have been wrongly rejected at the
    /// MaxTurnAngleDegrees=120 limit).
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
                hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: false,
                geometry: new List<Coordinate> { start, a }),

            // Curved: A -> B via an interior vertex ("mid") that keeps the real departure tangent
            // near A close to a straight continuation, even though the edge swings sharply afterwards.
            new(302, aId, bId, 122.31,
                hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: false,
                geometry: new List<Coordinate> { a, mid, b }),

            // Closing: B -> Start, colinear with the curve's arrival tangent (zero closing turn).
            new(303, bId, startId, 97.82,
                hasBarrier: false, hasNoEntry: false, isRestricted: false, elevationGainMeters: 0, isOffroad: false,
                geometry: new List<Coordinate> { b, start }),
        };

        var options = new SkeletonSearchOptions(AllowGates: true, AllowPrivateRoads: true, AllowRestrictedZones: true);
        const double targetLoopDistanceMeters = 111.19 + 122.31 + 97.82;

        // Act
        var result = _sut.FindSkeletons(startNode, nodes, edges, targetLoopDistanceMeters, options);

        // Assert - the A->B edge must be taken despite its sharp whole-edge chord, because the real
        // tangent at the A end is a gentle 20 degree turn, not the chord's 141 degrees.
        Assert.Single(result);
    }
}
