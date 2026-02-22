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
        var roadClassIntervals = new[]
        {
            new RoadClassInterval { FromIndex = 0, ToIndex = 4, RoadClass = RoadClassType.Primary }
        };
        var surfaceIntervals = new[]
        {
            new SurfaceInterval { FromIndex = 0, ToIndex = 4, Surface = SurfaceType.Asphalt }
        };

        // Act
        var result = SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals);

        // Assert
        Assert.Single(result);
        Assert.Equal(RoadClassType.Primary, result[0].RoadClass);
        Assert.Equal(SurfaceType.Asphalt, result[0].Surface);
    }

    [Fact]
    public void Build_RoadClassChangesMidway_CreatesTwoSegments()
    {
        // Arrange
        var geometry = CreateGeometry(10);
        var roadClassIntervals = new[]
        {
            new RoadClassInterval { FromIndex = 0, ToIndex = 5, RoadClass = RoadClassType.Primary },
            new RoadClassInterval { FromIndex = 5, ToIndex = 9, RoadClass = RoadClassType.Secondary }
        };
        var surfaceIntervals = new[]
        {
            new SurfaceInterval { FromIndex = 0, ToIndex = 9, Surface = SurfaceType.Asphalt }
        };

        // Act
        var result = SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals);

        // Assert
        Assert.Equal(2, result.Count);

        Assert.Equal(RoadClassType.Primary, result[0].RoadClass);
        Assert.Equal(SurfaceType.Asphalt, result[0].Surface);

        Assert.Equal(RoadClassType.Secondary, result[1].RoadClass);
        Assert.Equal(SurfaceType.Asphalt, result[1].Surface);
    }

    [Fact]
    public void Build_SurfaceChangesMidway_CreatesTwoSegments()
    {
        // Arrange
        var geometry = CreateGeometry(10);
        var roadClassIntervals = new[]
        {
            new RoadClassInterval { FromIndex = 0, ToIndex = 9, RoadClass = RoadClassType.Primary }
        };
        var surfaceIntervals = new[]
        {
            new SurfaceInterval { FromIndex = 0, ToIndex = 5, Surface = SurfaceType.Asphalt },
            new SurfaceInterval { FromIndex = 5, ToIndex = 9, Surface = SurfaceType.Gravel }
        };

        // Act
        var result = SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals);

        // Assert
        Assert.Equal(2, result.Count);

        Assert.Equal(SurfaceType.Asphalt, result[0].Surface);
        Assert.Equal(RoadClassType.Primary, result[0].RoadClass);

        Assert.Equal(SurfaceType.Gravel, result[1].Surface);
        Assert.Equal(RoadClassType.Primary, result[1].RoadClass);
    }

    [Fact]
    public void Build_BothTypesChangeAtDifferentPoints_CreatesCorrectSegments()
    {
        // Arrange
        var geometry = CreateGeometry(10);
        var roadClassIntervals = new[]
        {
            new RoadClassInterval { FromIndex = 0, ToIndex = 5, RoadClass = RoadClassType.Primary },
            new RoadClassInterval { FromIndex = 5, ToIndex = 9, RoadClass = RoadClassType.Secondary }
        };
        var surfaceIntervals = new[]
        {
            new SurfaceInterval { FromIndex = 0, ToIndex = 3, Surface = SurfaceType.Asphalt },
            new SurfaceInterval { FromIndex = 3, ToIndex = 9, Surface = SurfaceType.Gravel }
        };

        // Act
        var result = SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals);

        // Assert
        // Boundaries: 0, 3, 5, 9 => 3 segments
        Assert.Equal(3, result.Count);

        // [0-3]: Primary, Asphalt
        Assert.Equal(RoadClassType.Primary, result[0].RoadClass);
        Assert.Equal(SurfaceType.Asphalt, result[0].Surface);

        // [3-5]: Primary, Gravel
        Assert.Equal(RoadClassType.Primary, result[1].RoadClass);
        Assert.Equal(SurfaceType.Gravel, result[1].Surface);

        // [5-9]: Secondary, Gravel
        Assert.Equal(RoadClassType.Secondary, result[2].RoadClass);
        Assert.Equal(SurfaceType.Gravel, result[2].Surface);
    }

    [Fact]
    public void Build_BothTypesChangeAtSamePoint_CreatesTwoSegments()
    {
        // Arrange
        var geometry = CreateGeometry(10);
        var roadClassIntervals = new[]
        {
            new RoadClassInterval { FromIndex = 0, ToIndex = 5, RoadClass = RoadClassType.Primary },
            new RoadClassInterval { FromIndex = 5, ToIndex = 9, RoadClass = RoadClassType.Track }
        };
        var surfaceIntervals = new[]
        {
            new SurfaceInterval { FromIndex = 0, ToIndex = 5, Surface = SurfaceType.Asphalt },
            new SurfaceInterval { FromIndex = 5, ToIndex = 9, Surface = SurfaceType.Dirt }
        };

        // Act
        var result = SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals);

        // Assert
        // Boundaries: 0, 5, 9 => 2 segments (5 is deduplicated)
        Assert.Equal(2, result.Count);

        Assert.Equal(RoadClassType.Primary, result[0].RoadClass);
        Assert.Equal(SurfaceType.Asphalt, result[0].Surface);

        Assert.Equal(RoadClassType.Track, result[1].RoadClass);
        Assert.Equal(SurfaceType.Dirt, result[1].Surface);
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
            new RoadClassInterval { FromIndex = 0, ToIndex = 2, RoadClass = RoadClassType.Primary },
            new RoadClassInterval { FromIndex = 2, ToIndex = 4, RoadClass = RoadClassType.Secondary }
        };
        var surfaceIntervals = new[]
        {
            new SurfaceInterval { FromIndex = 0, ToIndex = 4, Surface = SurfaceType.Asphalt }
        };

        // Act
        var result = SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals);

        // Assert
        Assert.Equal(2, result.Count);

        // First segment [0-2] should have coordinates 0, 1, 2 (3 points)
        Assert.Equal(3, result[0].Geometry.Count);
        Assert.Equal(geometry[0], result[0].Start);
        Assert.Equal(geometry[2], result[0].End);

        // Second segment [2-4] should have coordinates 2, 3, 4 (3 points)
        Assert.Equal(3, result[1].Geometry.Count);
        Assert.Equal(geometry[2], result[1].Start);
        Assert.Equal(geometry[4], result[1].End);
    }

    [Fact]
    public void Build_SegmentStartAndEnd_MatchFirstAndLastGeometryPoints()
    {
        // Arrange
        var geometry = CreateGeometry(10);
        var roadClassIntervals = new[]
        {
            new RoadClassInterval { FromIndex = 0, ToIndex = 9, RoadClass = RoadClassType.Primary }
        };
        var surfaceIntervals = new[]
        {
            new SurfaceInterval { FromIndex = 0, ToIndex = 9, Surface = SurfaceType.Asphalt }
        };

        // Act
        var result = SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals);

        // Assert
        var segment = Assert.Single(result);
        Assert.Equal(segment.Geometry[0], segment.Start);
        Assert.Equal(segment.Geometry[^1], segment.End);
    }

    #endregion

    #region Contract Violation Tests

    [Fact]
    public void Build_EmptyRoadClassIntervals_ThrowsContractViolationException()
    {
        // Arrange
        var geometry = CreateGeometry(5);
        var roadClassIntervals = Array.Empty<RoadClassInterval>();
        var surfaceIntervals = new[]
        {
            new SurfaceInterval { FromIndex = 0, ToIndex = 4, Surface = SurfaceType.Asphalt }
        };

        // Act & Assert
        Assert.Throws<ContractViolationException>(() =>
            SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals));
    }

    [Fact]
    public void Build_EmptySurfaceIntervals_ThrowsContractViolationException()
    {
        // Arrange
        var geometry = CreateGeometry(5);
        var roadClassIntervals = new[]
        {
            new RoadClassInterval { FromIndex = 0, ToIndex = 4, RoadClass = RoadClassType.Primary }
        };
        var surfaceIntervals = Array.Empty<SurfaceInterval>();

        // Act & Assert
        Assert.Throws<ContractViolationException>(() =>
            SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals));
    }

    [Fact]
    public void Build_GapInRoadClassCoverage_ThrowsContractViolationException()
    {
        // Arrange
        var geometry = CreateGeometry(10);
        var roadClassIntervals = new[]
        {
            new RoadClassInterval { FromIndex = 0, ToIndex = 3, RoadClass = RoadClassType.Primary },
            // Gap between 3 and 5
            new RoadClassInterval { FromIndex = 5, ToIndex = 9, RoadClass = RoadClassType.Secondary }
        };
        var surfaceIntervals = new[]
        {
            new SurfaceInterval { FromIndex = 0, ToIndex = 9, Surface = SurfaceType.Asphalt }
        };

        // Act & Assert
        var exception = Assert.Throws<ContractViolationException>(() =>
            SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals));

        Assert.Contains("RoadClassIntervals", exception.Message);
        Assert.Contains("index 3", exception.Message);
    }

    [Fact]
    public void Build_GapInSurfaceCoverage_ThrowsContractViolationException()
    {
        // Arrange
        var geometry = CreateGeometry(10);
        var roadClassIntervals = new[]
        {
            new RoadClassInterval { FromIndex = 0, ToIndex = 9, RoadClass = RoadClassType.Primary }
        };
        var surfaceIntervals = new[]
        {
            new SurfaceInterval { FromIndex = 0, ToIndex = 3, Surface = SurfaceType.Asphalt },
            // Gap between 3 and 6
            new SurfaceInterval { FromIndex = 6, ToIndex = 9, Surface = SurfaceType.Gravel }
        };

        // Act & Assert
        var exception = Assert.Throws<ContractViolationException>(() =>
            SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals));

        Assert.Contains("SurfaceIntervals", exception.Message);
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
