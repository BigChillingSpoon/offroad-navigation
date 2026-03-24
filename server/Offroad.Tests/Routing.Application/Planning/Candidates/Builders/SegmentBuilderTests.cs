using Offroad.Core.Exceptions;
using Routing.Application.Planning.Candidates.Builders;
using Routing.Domain.Enums;
using Routing.Domain.ValueObjects;

namespace Offroad.Tests.Routing.Application.Planning.Candidates.Builders;

public class SegmentBuilderTests
{
    #region Happy Path Tests

    [Fact]
    public void Build_SingleIntervalOfEachType_ReturnsSingleSegment()
    {
        // Arrange
        var geometry = CreateGeometry(5);
        var roadClassIntervals = new[] { new Interval<RoadClassType>(0, 4, RoadClassType.PRIMARY) };
        var surfaceIntervals = new[] { new Interval<SurfaceType>(0, 4, SurfaceType.ASPHALT) };
        var trackTypeIntervals = new[] { new Interval<TrackType>(0, 4, TrackType.GRADE1) };

        // Act
        var result = SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals, trackTypeIntervals);

        // Assert
        Assert.Single(result);
        Assert.Equal(RoadClassType.PRIMARY, result[0].RoadClass);
        Assert.Equal(SurfaceType.ASPHALT, result[0].Surface);
        Assert.Equal(TrackType.GRADE1, result[0].TrackType);
    }

    [Fact]
    public void Build_RoadClassChangesMidway_CreatesTwoSegments()
    {
        // Arrange
        var geometry = CreateGeometry(10);
        var roadClassIntervals = new[]
        {
            new Interval<RoadClassType>(0, 5, RoadClassType.PRIMARY),
            new Interval<RoadClassType>(5, 9, RoadClassType.SECONDARY)
        };
        var surfaceIntervals = new[] { new Interval<SurfaceType>(0, 9, SurfaceType.ASPHALT) };
        var trackTypeIntervals = new[] { new Interval<TrackType>(0, 9, TrackType.GRADE1) };

        // Act
        var result = SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals, trackTypeIntervals);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(RoadClassType.PRIMARY, result[0].RoadClass);
        Assert.Equal(RoadClassType.SECONDARY, result[1].RoadClass);
    }

    [Fact]
    public void Build_SurfaceChangesMidway_CreatesTwoSegments()
    {
        // Arrange
        var geometry = CreateGeometry(10);
        var roadClassIntervals = new[] { new Interval<RoadClassType>(0, 9, RoadClassType.PRIMARY) };
        var surfaceIntervals = new[]
        {
            new Interval<SurfaceType>(0, 5, SurfaceType.ASPHALT),
            new Interval<SurfaceType>(5, 9, SurfaceType.GRAVEL)
        };
        var trackTypeIntervals = new[] { new Interval<TrackType>(0, 9, TrackType.GRADE1) };

        // Act
        var result = SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals, trackTypeIntervals);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(SurfaceType.ASPHALT, result[0].Surface);
        Assert.Equal(SurfaceType.GRAVEL, result[1].Surface);
    }

    [Fact]
    public void Build_TrackTypeChangesMidway_CreatesTwoSegments()
    {
        // Arrange
        var geometry = CreateGeometry(10);
        var roadClassIntervals = new[] { new Interval<RoadClassType>(0, 9, RoadClassType.TRACK) };
        var surfaceIntervals = new[] { new Interval<SurfaceType>(0, 9, SurfaceType.GRAVEL) };
        var trackTypeIntervals = new[]
        {
            new Interval<TrackType>(0, 5, TrackType.GRADE2),
            new Interval<TrackType>(5, 9, TrackType.GRADE4)
        };

        // Act
        var result = SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals, trackTypeIntervals);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(TrackType.GRADE2, result[0].TrackType);
        Assert.Equal(TrackType.GRADE4, result[1].TrackType);
    }

    [Fact]
    public void Build_BothTypesChangeAtDifferentPoints_CreatesCorrectSegments()
    {
        // Arrange
        var geometry = CreateGeometry(10);
        var roadClassIntervals = new[]
        {
            new Interval<RoadClassType>(0, 5, RoadClassType.PRIMARY),
            new Interval<RoadClassType>(5, 9, RoadClassType.SECONDARY)
        };
        var surfaceIntervals = new[]
        {
            new Interval<SurfaceType>(0, 3, SurfaceType.ASPHALT),
            new Interval<SurfaceType>(3, 9, SurfaceType.GRAVEL)
        };
        var trackTypeIntervals = new[] { new Interval<TrackType>(0, 9, TrackType.GRADE1) };

        // Act
        var result = SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals, trackTypeIntervals);

        // Assert
        // Boundaries: 0, 3, 5, 9 => 3 segments
        Assert.Equal(3, result.Count);

        // [0-3]: PRIMARY, ASPHALT
        Assert.Equal(RoadClassType.PRIMARY, result[0].RoadClass);
        Assert.Equal(SurfaceType.ASPHALT, result[0].Surface);

        // [3-5]: PRIMARY, GRAVEL
        Assert.Equal(RoadClassType.PRIMARY, result[1].RoadClass);
        Assert.Equal(SurfaceType.GRAVEL, result[1].Surface);

        // [5-9]: SECONDARY, GRAVEL
        Assert.Equal(RoadClassType.SECONDARY, result[2].RoadClass);
        Assert.Equal(SurfaceType.GRAVEL, result[2].Surface);
    }

    [Fact]
    public void Build_AllThreeTypesChangeAtDifferentPoints_CreatesCorrectSegments()
    {
        // Arrange
        var geometry = CreateGeometry(12);
        var roadClassIntervals = new[]
        {
            new Interval<RoadClassType>(0, 6, RoadClassType.PRIMARY),
            new Interval<RoadClassType>(6, 11, RoadClassType.TRACK)
        };
        var surfaceIntervals = new[]
        {
            new Interval<SurfaceType>(0, 4, SurfaceType.ASPHALT),
            new Interval<SurfaceType>(4, 11, SurfaceType.DIRT)
        };
        var trackTypeIntervals = new[]
        {
            new Interval<TrackType>(0, 8, TrackType.GRADE1),
            new Interval<TrackType>(8, 11, TrackType.GRADE4)
        };

        // Act
        var result = SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals, trackTypeIntervals);

        // Assert
        // Boundaries: 0, 4, 6, 8, 11 => 4 segments
        Assert.Equal(4, result.Count);

        Assert.Equal(RoadClassType.PRIMARY, result[0].RoadClass);
        Assert.Equal(SurfaceType.ASPHALT, result[0].Surface);
        Assert.Equal(TrackType.GRADE1, result[0].TrackType);

        Assert.Equal(RoadClassType.PRIMARY, result[1].RoadClass);
        Assert.Equal(SurfaceType.DIRT, result[1].Surface);
        Assert.Equal(TrackType.GRADE1, result[1].TrackType);

        Assert.Equal(RoadClassType.TRACK, result[2].RoadClass);
        Assert.Equal(SurfaceType.DIRT, result[2].Surface);
        Assert.Equal(TrackType.GRADE1, result[2].TrackType);

        Assert.Equal(RoadClassType.TRACK, result[3].RoadClass);
        Assert.Equal(SurfaceType.DIRT, result[3].Surface);
        Assert.Equal(TrackType.GRADE4, result[3].TrackType);
    }

    [Fact]
    public void Build_BothTypesChangeAtSamePoint_CreatesTwoSegments()
    {
        // Arrange
        var geometry = CreateGeometry(10);
        var roadClassIntervals = new[]
        {
            new Interval<RoadClassType>(0, 5, RoadClassType.PRIMARY),
            new Interval<RoadClassType>(5, 9, RoadClassType.TRACK)
        };
        var surfaceIntervals = new[]
        {
            new Interval<SurfaceType>(0, 5, SurfaceType.ASPHALT),
            new Interval<SurfaceType>(5, 9, SurfaceType.DIRT)
        };
        var trackTypeIntervals = new[]
        {
            new Interval<TrackType>(0, 5, TrackType.GRADE1),
            new Interval<TrackType>(5, 9, TrackType.GRADE5)
        };

        // Act
        var result = SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals, trackTypeIntervals);

        // Assert
        Assert.Equal(2, result.Count);

        Assert.Equal(RoadClassType.PRIMARY, result[0].RoadClass);
        Assert.Equal(SurfaceType.ASPHALT, result[0].Surface);
        Assert.Equal(TrackType.GRADE1, result[0].TrackType);

        Assert.Equal(RoadClassType.TRACK, result[1].RoadClass);
        Assert.Equal(SurfaceType.DIRT, result[1].Surface);
        Assert.Equal(TrackType.GRADE5, result[1].TrackType);
    }

    #endregion

    #region Geometry Tests

    [Fact]
    public void Build_SegmentGeometry_ContainsCorrectCoordinates()
    {
        // Arrange
        var geometry = CreateGeometry(5);
        var roadClassIntervals = new[]
        {
            new Interval<RoadClassType>(0, 2, RoadClassType.PRIMARY),
            new Interval<RoadClassType>(2, 4, RoadClassType.SECONDARY)
        };
        var surfaceIntervals = new[] { new Interval<SurfaceType>(0, 4, SurfaceType.ASPHALT) };
        var trackTypeIntervals = new[] { new Interval<TrackType>(0, 4, TrackType.GRADE1) };

        // Act
        var result = SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals, trackTypeIntervals);

        // Assert
        Assert.Equal(2, result.Count);

        // First segment [0-2]
        Assert.Equal(0, result[0].FromIndex);
        Assert.Equal(2, result[0].ToIndex);
        Assert.Equal(geometry[0], result[0].Start);
        Assert.Equal(geometry[2], result[0].End);

        // Second segment [2-4]
        Assert.Equal(2, result[1].FromIndex);
        Assert.Equal(4, result[1].ToIndex);
        Assert.Equal(geometry[2], result[1].Start);
        Assert.Equal(geometry[4], result[1].End);
    }

    [Fact]
    public void Build_SegmentStartAndEnd_MatchFirstAndLastGeometryPoints()
    {
        // Arrange
        var geometry = CreateGeometry(10);
        var roadClassIntervals = new[] { new Interval<RoadClassType>(0, 9, RoadClassType.PRIMARY) };
        var surfaceIntervals = new[] { new Interval<SurfaceType>(0, 9, SurfaceType.ASPHALT) };
        var trackTypeIntervals = new[] { new Interval<TrackType>(0, 9, TrackType.GRADE1) };

        // Act
        var result = SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals, trackTypeIntervals);

        // Assert
        var segment = Assert.Single(result);
        Assert.Equal(0, segment.FromIndex);
        Assert.Equal(9, segment.ToIndex);
        Assert.Equal(geometry[0], segment.Start);
        Assert.Equal(geometry[9], segment.End);
    }

    #endregion

    #region Contract Violation Tests

    [Fact]
    public void Build_EmptyRoadClassIntervals_ThrowsContractViolationException()
    {
        // Arrange
        var geometry = CreateGeometry(5);
        var roadClassIntervals = Array.Empty<Interval<RoadClassType>>();
        var surfaceIntervals = new[] { new Interval<SurfaceType>(0, 4, SurfaceType.ASPHALT) };
        var trackTypeIntervals = new[] { new Interval<TrackType>(0, 4, TrackType.GRADE1) };

        // Act & Assert
        Assert.Throws<ContractViolationException>(() =>
            SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals, trackTypeIntervals));
    }

    [Fact]
    public void Build_EmptySurfaceIntervals_ThrowsContractViolationException()
    {
        // Arrange
        var geometry = CreateGeometry(5);
        var roadClassIntervals = new[] { new Interval<RoadClassType>(0, 4, RoadClassType.PRIMARY) };
        var surfaceIntervals = Array.Empty<Interval<SurfaceType>>();
        var trackTypeIntervals = new[] { new Interval<TrackType>(0, 4, TrackType.GRADE1) };

        // Act & Assert
        Assert.Throws<ContractViolationException>(() =>
            SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals, trackTypeIntervals));
    }

    [Fact]
    public void Build_EmptyTrackTypeIntervals_ThrowsContractViolationException()
    {
        // Arrange
        var geometry = CreateGeometry(5);
        var roadClassIntervals = new[] { new Interval<RoadClassType>(0, 4, RoadClassType.PRIMARY) };
        var surfaceIntervals = new[] { new Interval<SurfaceType>(0, 4, SurfaceType.ASPHALT) };
        var trackTypeIntervals = Array.Empty<Interval<TrackType>>();

        // Act & Assert
        Assert.Throws<ContractViolationException>(() =>
            SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals, trackTypeIntervals));
    }

    [Fact]
    public void Build_GapInRoadClassCoverage_ThrowsContractViolationException()
    {
        // Arrange
        var geometry = CreateGeometry(10);
        var roadClassIntervals = new[]
        {
            new Interval<RoadClassType>(0, 3, RoadClassType.PRIMARY),
            new Interval<RoadClassType>(5, 9, RoadClassType.SECONDARY)
        };
        var surfaceIntervals = new[] { new Interval<SurfaceType>(0, 9, SurfaceType.ASPHALT) };
        var trackTypeIntervals = new[] { new Interval<TrackType>(0, 9, TrackType.GRADE1) };

        // Act & Assert
        var exception = Assert.Throws<ContractViolationException>(() =>
            SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals, trackTypeIntervals));

        Assert.Contains("RoadClassIntervals", exception.Message);
        Assert.Contains("index 3", exception.Message);
    }

    [Fact]
    public void Build_GapInSurfaceCoverage_ThrowsContractViolationException()
    {
        // Arrange
        var geometry = CreateGeometry(10);
        var roadClassIntervals = new[] { new Interval<RoadClassType>(0, 9, RoadClassType.PRIMARY) };
        var surfaceIntervals = new[]
        {
            new Interval<SurfaceType>(0, 3, SurfaceType.ASPHALT),
            new Interval<SurfaceType>(6, 9, SurfaceType.GRAVEL)
        };
        var trackTypeIntervals = new[] { new Interval<TrackType>(0, 9, TrackType.GRADE1) };

        // Act & Assert
        var exception = Assert.Throws<ContractViolationException>(() =>
            SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals, trackTypeIntervals));

        Assert.Contains("SurfaceIntervals", exception.Message);
        Assert.Contains("index 3", exception.Message);
    }

    [Fact]
    public void Build_GapInTrackTypeCoverage_ThrowsContractViolationException()
    {
        // Arrange
        var geometry = CreateGeometry(10);
        var roadClassIntervals = new[] { new Interval<RoadClassType>(0, 9, RoadClassType.PRIMARY) };
        var surfaceIntervals = new[] { new Interval<SurfaceType>(0, 9, SurfaceType.ASPHALT) };
        var trackTypeIntervals = new[]
        {
            new Interval<TrackType>(0, 3, TrackType.GRADE1),
            new Interval<TrackType>(6, 9, TrackType.GRADE4)
        };

        // Act & Assert
        var exception = Assert.Throws<ContractViolationException>(() =>
            SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals, trackTypeIntervals));

        Assert.Contains("TrackTypeIntervals", exception.Message);
        Assert.Contains("index 3", exception.Message);
    }

    #endregion

    #region Helper Methods

    private static List<Coordinate> CreateGeometry(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => new Coordinate(i * 0.001, i * 0.001))
            .ToList();
    }

    #endregion
}
