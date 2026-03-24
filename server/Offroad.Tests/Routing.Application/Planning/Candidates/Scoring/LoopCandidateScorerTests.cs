using Microsoft.Extensions.Options;
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Candidates.Scoring;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Planner;
using Routing.Application.Planning.Profiles;
using Routing.Domain.Enums;
using Routing.Domain.ValueObjects;

namespace Offroad.Tests.Routing.Application.Planning.Candidates.Scoring;

public class LoopCandidateScorerTests
{
    private readonly LoopCandidateScorer _sut;

    public LoopCandidateScorerTests()
    {
        _sut = new LoopCandidateScorer(CreateOptionsMonitor());
    }

    #region Base Score Tests

    [Fact]
    public void Score_FullOffroad_NoElevation_Returns100()
    {
        // Arrange
        // OffroadRatio = 1.0 → offroadScore = 100.0
        // ElevationGain = 0 → elevationPenalty = 0
        // Expected: 100.0
        var intent = CreateLoopIntent();
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

    [Fact]
    public void Score_FullOffroad_WithElevation_SubtractsElevationPenalty()
    {
        // Arrange
        // OffroadRatio = 1.0 → offroadScore = 100.0
        // ElevationGain = 500 → elevationPenalty = 500 * 0.01 = 5.0
        // Expected: 100.0 - 5.0 = 95.0
        var intent = CreateLoopIntent();
        var offroadSegment = CreateOffroadSegment();
        var candidates = new[]
        {
            CreateCandidate(
                totalDistance: offroadSegment.DistanceMeters,
                segments: new List<Segment> { offroadSegment },
                elevationGain: 500.0)
        };

        // Act
        var result = _sut.Score(candidates, intent, new UserRoutingProfile(), new PlannerSettings());

        // Assert
        Assert.Equal(95.0, result[0].Score, precision: 1);
    }

    [Fact]
    public void Score_NoOffroad_HighElevation_ReturnsNegative()
    {
        // Arrange
        // OffroadRatio = 0 → offroadScore = 0
        // ElevationGain = 2000 → elevationPenalty = 20.0
        // Expected: 0 - 20.0 = -20.0
        var intent = CreateLoopIntent();
        var candidates = new[]
        {
            CreateCandidate(totalDistance: 10_000, elevationGain: 2000.0)
        };

        // Act
        var result = _sut.Score(candidates, intent, new UserRoutingProfile(), new PlannerSettings());

        // Assert
        Assert.Equal(-20.0, result[0].Score, precision: 5);
    }

    #endregion

    #region Empty Input Tests

    [Fact]
    public void Score_EmptyCandidates_ReturnsEmptyList()
    {
        // Act
        var result = _sut.Score(Array.Empty<TripCandidate>(), CreateLoopIntent(), new UserRoutingProfile(), new PlannerSettings());

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Helper Methods

    private static IOptionsMonitor<ScoringProfiles> CreateOptionsMonitor()
    {
        return new TestOptionsMonitor<ScoringProfiles>(new ScoringProfiles());
    }

    private static LoopIntent CreateLoopIntent()
    {
        return new LoopIntent
        {
            Start = new Coordinate(50.0, 14.0),
            PreferredLengthKm = 30,
            MaxDriveDistanceKm = 50
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
        var start = new Coordinate(50.0, 14.0);
        var end = new Coordinate(50.001, 14.0);
        var geometry = new List<Coordinate> { start, end };
        return Segment.Create(start, end, geometry, RoadClassType.TRACK, TrackType.UNKNOWN, SurfaceType.DIRT);
    }

    #endregion
}
