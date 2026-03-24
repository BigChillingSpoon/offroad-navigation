using Microsoft.Extensions.Options;
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Candidates.Scoring;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Planner;
using Routing.Application.Planning.Profiles;
using Routing.Domain.Enums;
using Routing.Domain.ValueObjects;

namespace Offroad.Tests.Routing.Application.Planning.Candidates.Scoring;

public class HardPenaltiesTests
{
    #region Route Scorer Penalty Tests

    [Fact]
    public void Score_RouteWithNationalParkAndGate_DeductsBothPenalties()
    {
        // Arrange
        // MaxOffroad scoring with 100% offroad → base score = 100.0
        // NationalPark penalty = 500.0, Gate penalty = 150.0
        // Expected: 100.0 - 500.0 - 150.0 = -550.0
        var sut = new RouteCandidateScorer(CreateOptionsMonitor());
        var intent = new RouteIntent
        {
            Start = new Coordinate(50.0, 14.0),
            End = new Coordinate(50.1, 14.1),
            Balance = RouteBalance.MaxOffroad
        };

        var offroadSegment = CreateOffroadSegment();
        var candidates = new[]
        {
            CreateCandidate(
                totalDistance: offroadSegment.DistanceMeters,
                segments: new List<Segment> { offroadSegment },
                restrictedZones: new List<Interval<RestrictionType>>
                {
                    new(0, 5, RestrictionType.NationalPark)
                },
                barriers: new List<RoadBarrier>
                {
                    new(BarrierType.Gate, 3, new Coordinate(50.0, 14.0))
                })
        };

        // Act
        var result = sut.Score(candidates, intent, new UserRoutingProfile(), new PlannerSettings());

        // Assert — base 100.0 - 500.0 (park) - 150.0 (gate) = -550.0
        Assert.Equal(-550.0, result[0].Score, precision: 1);
    }

    [Fact]
    public void Score_RouteWithMultipleRestrictions_DeductsAll()
    {
        // Arrange
        // Shortest with equal distance → base score = 100.0
        // Forestry (50) + Private (200) = 250.0 penalty
        // Expected: 100.0 - 250.0 = -150.0
        var sut = new RouteCandidateScorer(CreateOptionsMonitor());
        var intent = new RouteIntent
        {
            Start = new Coordinate(50.0, 14.0),
            End = new Coordinate(50.1, 14.1),
            Balance = RouteBalance.Shortest
        };

        var candidates = new[]
        {
            CreateCandidate(
                totalDistance: 10_000,
                restrictedZones: new List<Interval<RestrictionType>>
                {
                    new(0, 3, RestrictionType.Forestry),
                    new(4, 8, RestrictionType.Private)
                })
        };

        // Act
        var result = sut.Score(candidates, intent, new UserRoutingProfile(), new PlannerSettings());

        // Assert — base 100.0 - 50.0 (forestry) - 200.0 (private) = -150.0
        Assert.Equal(-150.0, result[0].Score, precision: 5);
    }

    #endregion

    #region Loop Scorer Penalty Tests

    [Fact]
    public void Score_LoopWithNationalParkAndGate_DeductsBothPenalties()
    {
        // Arrange
        // 100% offroad → base = 100.0, elevationGain=0 → penalty=0
        // Loop config: NationalPark = 600.0, Gate = 200.0 (higher than route)
        // Expected: 100.0 - 600.0 - 200.0 = -700.0
        var sut = new LoopCandidateScorer(CreateOptionsMonitor());
        var intent = new LoopIntent
        {
            Start = new Coordinate(50.0, 14.0),
            PreferredLengthKm = 30,
            MaxDriveDistanceKm = 50
        };

        var offroadSegment = CreateOffroadSegment();
        var candidates = new[]
        {
            CreateCandidate(
                totalDistance: offroadSegment.DistanceMeters,
                segments: new List<Segment> { offroadSegment },
                restrictedZones: new List<Interval<RestrictionType>>
                {
                    new(0, 5, RestrictionType.NationalPark)
                },
                barriers: new List<RoadBarrier>
                {
                    new(BarrierType.Gate, 3, new Coordinate(50.0, 14.0))
                })
        };

        // Act
        var result = sut.Score(candidates, intent, new UserRoutingProfile(), new PlannerSettings());

        // Assert — base 100.0 - 600.0 (park, loop config) - 200.0 (gate, loop config) = -700.0
        Assert.Equal(-700.0, result[0].Score, precision: 1);
    }

    #endregion

    #region Config-Driven Tests

    [Fact]
    public void Score_RouteAndLoopScorers_UseDifferentPenaltyWeights()
    {
        // Arrange — same candidate through both scorers
        var options = CreateOptionsMonitor();
        var routeScorer = new RouteCandidateScorer(options);
        var loopScorer = new LoopCandidateScorer(options);

        var offroadSegment = CreateOffroadSegment();
        var candidates = new[]
        {
            CreateCandidate(
                totalDistance: offroadSegment.DistanceMeters,
                segments: new List<Segment> { offroadSegment },
                restrictedZones: new List<Interval<RestrictionType>>
                {
                    new(0, 5, RestrictionType.NationalPark)
                })
        };

        // Act
        var routeResult = routeScorer.Score(candidates,
            new RouteIntent { Start = new Coordinate(50, 14), End = new Coordinate(50.1, 14.1), Balance = RouteBalance.MaxOffroad },
            new UserRoutingProfile(), new PlannerSettings());

        var loopResult = loopScorer.Score(candidates,
            new LoopIntent { Start = new Coordinate(50, 14), PreferredLengthKm = 30, MaxDriveDistanceKm = 50 },
            new UserRoutingProfile(), new PlannerSettings());

        // Assert — Loop NationalPark penalty (600) is higher than Route (500)
        Assert.True(routeResult[0].Score > loopResult[0].Score,
            $"Route score ({routeResult[0].Score}) should be higher than Loop score ({loopResult[0].Score}) because Loop has harsher NationalPark penalty");
    }

    [Fact]
    public void Score_NoPenalties_ReturnsOnlyBaseScore()
    {
        // Arrange — no barriers, no restricted zones
        var sut = new RouteCandidateScorer(CreateOptionsMonitor());
        var intent = new RouteIntent
        {
            Start = new Coordinate(50.0, 14.0),
            End = new Coordinate(50.1, 14.1),
            Balance = RouteBalance.Shortest
        };
        var candidates = new[] { CreateCandidate(totalDistance: 10_000) };

        // Act
        var result = sut.Score(candidates, intent, new UserRoutingProfile(), new PlannerSettings());

        // Assert — pure base score, no deductions
        Assert.Equal(100.0, result[0].Score, precision: 5);
    }

    #endregion

    #region Helper Methods

    private static IOptionsMonitor<ScoringProfiles> CreateOptionsMonitor()
    {
        var profiles = new ScoringProfiles
        {
            Route = new PenaltyWeights
            {
                Detour = new DetourWeights
                {
                    MaxRatio = 0.3,
                    StandardPenaltyRate = 50.0,
                    ExcessiveBasePenalty = 15.0,
                    ExcessiveRate = 200.0
                },
                Restrictions = new RestrictionWeights
                {
                    NationalPark = 500.0,
                    Private = 200.0,
                    Forestry = 50.0
                },
                Barriers = new BarrierWeights
                {
                    Gate = 150.0
                }
            },
            Loop = new PenaltyWeights
            {
                Restrictions = new RestrictionWeights
                {
                    NationalPark = 600.0,
                    Private = 250.0,
                    Forestry = 80.0
                },
                Barriers = new BarrierWeights
                {
                    Gate = 200.0
                }
            }
        };

        return new TestOptionsMonitor<ScoringProfiles>(profiles);
    }

    private static TripCandidate CreateCandidate(
        double totalDistance,
        List<Segment>? segments = null,
        List<RoadBarrier>? barriers = null,
        List<Interval<RestrictionType>>? restrictedZones = null,
        double elevationGain = 0,
        double elevationLoss = 0)
    {
        return TripCandidate.Create(
            segments ?? new List<Segment>(),
            barriers ?? new List<RoadBarrier>(),
            restrictedZones ?? new List<Interval<RestrictionType>>(),
            new EncodedPolyline(),
            totalDistance,
            TimeSpan.FromMinutes(30),
            elevationGain,
            elevationLoss);
    }

    private static Segment CreateOffroadSegment()
    {
        var geometry = new List<Coordinate>
        {
            new(50.0, 14.0),
            new(50.001, 14.0)
        };
        return Segment.Create(geometry, 0, 1, RoadClassType.TRACK, SurfaceType.DIRT, TrackType.UNKNOWN);
    }

    #endregion
}
