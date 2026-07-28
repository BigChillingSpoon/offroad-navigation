using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Candidates.Selection;
using Routing.Application.Planning.Intents;
using Routing.Domain.Enums;
using Routing.Domain.ValueObjects;

namespace Offroad.Tests.Routing.Application.Planning.Candidates.Selection;

public class LoopEntranceSelectorTests
{
    private readonly LoopEntranceSelector _sut = new();

    [Fact]
    public void Select_MultipleCandidatesSharingEntrance_ReturnsOnlyHighestScored()
    {
        // Arrange - three skeleton attempts scored for the same entrance; only the best-scored
        // one should survive.
        var entrance = new Coordinate(50.0, 14.0);
        var worse = Scored(entrance, score: 10);
        var best = Scored(entrance, score: 90);
        var middle = Scored(entrance, score: 50);

        // Act
        var result = _sut.Select(new[] { worse, best, middle }, CreateLoopIntent());

        // Assert
        var winner = Assert.Single(result);
        Assert.Same(best.Candidate, winner.Candidate);
    }

    [Fact]
    public void Select_CandidatesAcrossTwoEntrances_ReturnsOneWinnerPerEntrance()
    {
        // Arrange
        var entranceA = new Coordinate(50.0, 14.0);
        var entranceB = new Coordinate(51.0, 15.0);

        var worseA = Scored(entranceA, score: 10);
        var bestA = Scored(entranceA, score: 90);
        var bestB = Scored(entranceB, score: 70);
        var worseB = Scored(entranceB, score: 5);

        // Act
        var result = _sut.Select(new[] { worseA, bestA, bestB, worseB }, CreateLoopIntent());

        // Assert - exactly one winner per entrance.
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => ReferenceEquals(r.Candidate, bestA.Candidate));
        Assert.Contains(result, r => ReferenceEquals(r.Candidate, bestB.Candidate));
    }

    [Fact]
    public void Select_ExactScoreTieWithinEntrance_FirstGeneratedCandidateWins()
    {
        // Arrange - two candidates for the same entrance with an identical score. The tie-break
        // is documented as deterministic: the first one generated (input order) wins.
        var entrance = new Coordinate(50.0, 14.0);
        var first = Scored(entrance, score: 42);
        var second = Scored(entrance, score: 42);

        // Act
        var result = _sut.Select(new[] { first, second }, CreateLoopIntent());

        // Assert
        var winner = Assert.Single(result);
        Assert.Same(first.Candidate, winner.Candidate);
    }

    [Fact]
    public void Select_EmptyInput_ReturnsEmpty()
    {
        // Act
        var result = _sut.Select(Array.Empty<ScoredTripCandidate<LoopTripCandidate>>(), CreateLoopIntent());

        // Assert
        Assert.Empty(result);
    }

    private static LoopIntent CreateLoopIntent() => new()
    {
        Start = new Coordinate(50.0, 14.0),
        PreferredLengthKm = 10,
        MaxDriveDistanceKm = 50,
    };

    private static ScoredTripCandidate<LoopTripCandidate> Scored(Coordinate entrance, double score)
    {
        var candidate = LoopTripCandidate.Create(
            new List<Segment>(),
            new List<RoadBarrier>(),
            new List<Interval<RestrictionType>>(),
            new EncodedPolyline(),
            totalDistance: 1000,
            duration: TimeSpan.FromMinutes(30),
            elevationGain: 0,
            elevationLoss: 0,
            maxGradientPercentage: 0,
            hookCoordinate: default,
            hookPolylineIndex: default,
            estimatedTransitDistanceMeters: default,
            entranceCoordinate: entrance);

        return new ScoredTripCandidate<LoopTripCandidate>(candidate, score);
    }
}
