using Microsoft.Extensions.Options;
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Candidates.Scoring;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Planner;
using Routing.Application.Planning.Profiles;
using Routing.Domain.Enums;
using Routing.Domain.ValueObjects;

namespace Offroad.Tests.Routing.Application.Planning.Candidates.Scoring;

public class RouteCandidateScorerTests
{
    private readonly RouteCandidateScorer _sut;

    public RouteCandidateScorerTests()
    {
        _sut = new RouteCandidateScorer(CreateOptionsMonitor());
    }

    #region Shortest Balance Tests

    [Fact]
    public void Score_ShortestBalance_ShortestCandidateGets100()
    {
        // Arrange
        var intent = CreateRouteIntent(RouteBalance.Shortest);
        var candidates = new[]
        {
            CreateCandidate(totalDistance: 10_000),
            CreateCandidate(totalDistance: 20_000)
        };

        // Act
        var result = _sut.Score(candidates, intent, new UserRoutingProfile(), new PlannerSettings());

        // Assert
        Assert.Equal(100.0, result[0].Score, precision: 5);
        Assert.Equal(50.0, result[1].Score, precision: 5);
    }

    [Fact]
    public void Score_ShortestBalance_EqualDistances_BothGet100()
    {
        // Arrange
        var intent = CreateRouteIntent(RouteBalance.Shortest);
        var candidates = new[]
        {
            CreateCandidate(totalDistance: 15_000),
            CreateCandidate(totalDistance: 15_000)
        };

        // Act
        var result = _sut.Score(candidates, intent, new UserRoutingProfile(), new PlannerSettings());

        // Assert
        Assert.Equal(100.0, result[0].Score, precision: 5);
        Assert.Equal(100.0, result[1].Score, precision: 5);
    }

    #endregion

    #region Balanced Mode Tests

    [Fact]
    public void Score_BalancedMode_DetourUnderThreshold_AppliesStandardPenalty()
    {
        // Arrange
        // Candidate is 10% longer than shortest → detourRatio = 0.1
        // Standard penalty: 0.1 * 50.0 = 5.0
        // No offroad segments → offroadScore = 0
        // Expected: 0 - 5.0 = -5.0
        var intent = CreateRouteIntent(RouteBalance.Balanced);
        var candidates = new[]
        {
            CreateCandidate(totalDistance: 10_000),
            CreateCandidate(totalDistance: 11_000)
        };

        // Act
        var result = _sut.Score(candidates, intent, new UserRoutingProfile(), new PlannerSettings());

        // Assert — first candidate has 0 detour
        Assert.Equal(0.0, result[0].Score, precision: 5);

        // Second candidate: detourRatio=0.1, penalty=0.1*50=5.0, score=0-5=-5
        Assert.Equal(-5.0, result[1].Score, precision: 5);
    }

    [Fact]
    public void Score_BalancedMode_DetourOverThreshold_AppliesExcessivePenalty()
    {
        // Arrange
        // Candidate is 50% longer than shortest → detourRatio = 0.5
        // Threshold = 0.3, so excessive penalty applies:
        // penalty = 15.0 + (0.5 - 0.3) * 200.0 = 15.0 + 40.0 = 55.0
        // No offroad → score = 0 - 55.0 = -55.0
        var intent = CreateRouteIntent(RouteBalance.Balanced);
        var candidates = new[]
        {
            CreateCandidate(totalDistance: 10_000),
            CreateCandidate(totalDistance: 15_000)
        };

        // Act
        var result = _sut.Score(candidates, intent, new UserRoutingProfile(), new PlannerSettings());

        // Assert
        Assert.Equal(-55.0, result[1].Score, precision: 5);
    }

    [Fact]
    public void Score_BalancedMode_OffroadReturnsPositiveScore()
    {
        // Arrange
        // Candidate with 100% offroad, no detour
        // offroadScore = 1.0 * 100 = 100, detourPenalty = 0
        // Expected: 100.0
        var intent = CreateRouteIntent(RouteBalance.Balanced);
        var offroadSegment = CreateOffroadSegment();
        var candidates = new[]
        {
            CreateCandidate(
                totalDistance: offroadSegment.DistanceMeters,
                segments: new List<Segment> { offroadSegment })
        };

        // Act
        var result = _sut.Score(candidates, intent, new UserRoutingProfile(), new PlannerSettings());

        // Assert
        Assert.Equal(100.0, result[0].Score, precision: 1);
    }

    #endregion

    #region MaxOffroad Tests

    [Fact]
    public void Score_MaxOffroad_FullyOffroadCandidate_Gets100()
    {
        // Arrange
        var intent = CreateRouteIntent(RouteBalance.MaxOffroad);
        var offroadSegment = CreateOffroadSegment();
        var candidates = new[]
        {
            CreateCandidate(
                totalDistance: offroadSegment.DistanceMeters,
                segments: new List<Segment> { offroadSegment })
        };

        // Act
        var result = _sut.Score(candidates, intent, new UserRoutingProfile(), new PlannerSettings());

        // Assert — OffroadRatio = 1.0, score = 100.0
        Assert.Equal(100.0, result[0].Score, precision: 1);
    }

    [Fact]
    public void Score_MaxOffroad_NoOffroadSegments_Gets0()
    {
        // Arrange
        var intent = CreateRouteIntent(RouteBalance.MaxOffroad);
        var candidates = new[] { CreateCandidate(totalDistance: 10_000) };

        // Act
        var result = _sut.Score(candidates, intent, new UserRoutingProfile(), new PlannerSettings());

        // Assert
        Assert.Equal(0.0, result[0].Score, precision: 5);
    }

    #endregion

    #region Empty Input Tests

    [Fact]
    public void Score_EmptyCandidates_ReturnsEmptyList()
    {
        // Arrange
        var intent = CreateRouteIntent(RouteBalance.Balanced);

        // Act
        var result = _sut.Score(Array.Empty<TripCandidate>(), intent, new UserRoutingProfile(), new PlannerSettings());

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Helper Methods

    private static IOptionsMonitor<ScoringProfiles> CreateOptionsMonitor()
    {
        return new TestOptionsMonitor<ScoringProfiles>(new ScoringProfiles());
    }

    private static RouteIntent CreateRouteIntent(RouteBalance balance)
    {
        return new RouteIntent
        {
            Start = new Coordinate(50.0, 14.0),
            End = new Coordinate(50.1, 14.1),
            Balance = balance
        };
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
