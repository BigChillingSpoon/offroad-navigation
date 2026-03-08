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
            new RoadClassInterval { FromIndex = 0, ToIndex = 4, RoadClass = RoadClassType.PRIMARY }
        };
        var surfaceIntervals = new[]
        {
            new SurfaceInterval { FromIndex = 0, ToIndex = 4, Surface = SurfaceType.ASPHALT }
        };
        var trackTypeIntervals = new[]
        {
            new TrackTypeInterval { FromIndex = 0, ToIndex = 4, TrackType = TrackType.GRADE1 }
        };

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
            new RoadClassInterval { FromIndex = 0, ToIndex = 5, RoadClass = RoadClassType.PRIMARY },
            new RoadClassInterval { FromIndex = 5, ToIndex = 9, RoadClass = RoadClassType.SECONDARY }
        };
        var surfaceIntervals = new[]
        {
            new SurfaceInterval { FromIndex = 0, ToIndex = 9, Surface = SurfaceType.ASPHALT }
        };
        var trackTypeIntervals = new[]
        {
            new TrackTypeInterval { FromIndex = 0, ToIndex = 9, TrackType = TrackType.GRADE1 }
        };

        // Act
        var result = SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals, trackTypeIntervals);

        // Assert
        Assert.Equal(2, result.Count);

        Assert.Equal(RoadClassType.PRIMARY, result[0].RoadClass);
        Assert.Equal(SurfaceType.ASPHALT, result[0].Surface);

        Assert.Equal(RoadClassType.SECONDARY, result[1].RoadClass);
        Assert.Equal(SurfaceType.ASPHALT, result[1].Surface);
    }

    [Fact]
    public void Build_SurfaceChangesMidway_CreatesTwoSegments()
    {
        // Arrange
        var geometry = CreateGeometry(10);
        var roadClassIntervals = new[]
        {
            new RoadClassInterval { FromIndex = 0, ToIndex = 9, RoadClass = RoadClassType.PRIMARY }
        };
        var surfaceIntervals = new[]
        {
            new SurfaceInterval { FromIndex = 0, ToIndex = 5, Surface = SurfaceType.ASPHALT },
            new SurfaceInterval { FromIndex = 5, ToIndex = 9, Surface = SurfaceType.GRAVEL }
        };
        var trackTypeIntervals = new[]
        {
            new TrackTypeInterval { FromIndex = 0, ToIndex = 9, TrackType = TrackType.GRADE1 }
        };

        // Act
        var result = SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals, trackTypeIntervals);

        // Assert
        Assert.Equal(2, result.Count);

        Assert.Equal(SurfaceType.ASPHALT, result[0].Surface);
        Assert.Equal(RoadClassType.PRIMARY, result[0].RoadClass);

        Assert.Equal(SurfaceType.GRAVEL, result[1].Surface);
        Assert.Equal(RoadClassType.PRIMARY, result[1].RoadClass);
    }

    [Fact]
    public void Build_TrackTypeChangesMidway_CreatesTwoSegments()
    {
        // Arrange
        var geometry = CreateGeometry(10);
        var roadClassIntervals = new[]
        {
            new RoadClassInterval { FromIndex = 0, ToIndex = 9, RoadClass = RoadClassType.TRACK }
        };
        var surfaceIntervals = new[]
        {
            new SurfaceInterval { FromIndex = 0, ToIndex = 9, Surface = SurfaceType.GRAVEL }
        };
        var trackTypeIntervals = new[]
        {
            new TrackTypeInterval { FromIndex = 0, ToIndex = 5, TrackType = TrackType.GRADE2 },
            new TrackTypeInterval { FromIndex = 5, ToIndex = 9, TrackType = TrackType.GRADE4 }
        };

        // Act
        var result = SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals, trackTypeIntervals);

        // Assert
        Assert.Equal(2, result.Count);

        Assert.Equal(TrackType.GRADE2, result[0].TrackType);
        Assert.Equal(SurfaceType.GRAVEL, result[0].Surface);

        Assert.Equal(TrackType.GRADE4, result[1].TrackType);
        Assert.Equal(SurfaceType.GRAVEL, result[1].Surface);
    }

    [Fact]
    public void Build_BothTypesChangeAtDifferentPoints_CreatesCorrectSegments()
    {
        // Arrange
        var geometry = CreateGeometry(10);
        var roadClassIntervals = new[]
        {
            new RoadClassInterval { FromIndex = 0, ToIndex = 5, RoadClass = RoadClassType.PRIMARY },
            new RoadClassInterval { FromIndex = 5, ToIndex = 9, RoadClass = RoadClassType.SECONDARY }
        };
        var surfaceIntervals = new[]
        {
            new SurfaceInterval { FromIndex = 0, ToIndex = 3, Surface = SurfaceType.ASPHALT },
            new SurfaceInterval { FromIndex = 3, ToIndex = 9, Surface = SurfaceType.GRAVEL }
        };
        var trackTypeIntervals = new[]
        {
            new TrackTypeInterval { FromIndex = 0, ToIndex = 9, TrackType = TrackType.GRADE1 }
        };

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
            new RoadClassInterval { FromIndex = 0, ToIndex = 6, RoadClass = RoadClassType.PRIMARY },
            new RoadClassInterval { FromIndex = 6, ToIndex = 11, RoadClass = RoadClassType.TRACK }
        };
        var surfaceIntervals = new[]
        {
            new SurfaceInterval { FromIndex = 0, ToIndex = 4, Surface = SurfaceType.ASPHALT },
            new SurfaceInterval { FromIndex = 4, ToIndex = 11, Surface = SurfaceType.DIRT }
        };
        var trackTypeIntervals = new[]
        {
            new TrackTypeInterval { FromIndex = 0, ToIndex = 8, TrackType = TrackType.GRADE1 },
            new TrackTypeInterval { FromIndex = 8, ToIndex = 11, TrackType = TrackType.GRADE4 }
        };

        // Act
        var result = SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals, trackTypeIntervals);

        // Assert
        // Boundaries: 0, 4, 6, 8, 11 => 4 segments
        Assert.Equal(4, result.Count);

        // [0-4]: PRIMARY, ASPHALT, GRADE1
        Assert.Equal(RoadClassType.PRIMARY, result[0].RoadClass);
        Assert.Equal(SurfaceType.ASPHALT, result[0].Surface);
        Assert.Equal(TrackType.GRADE1, result[0].TrackType);

        // [4-6]: PRIMARY, DIRT, GRADE1
        Assert.Equal(RoadClassType.PRIMARY, result[1].RoadClass);
        Assert.Equal(SurfaceType.DIRT, result[1].Surface);
        Assert.Equal(TrackType.GRADE1, result[1].TrackType);

        // [6-8]: TRACK, DIRT, GRADE1
        Assert.Equal(RoadClassType.TRACK, result[2].RoadClass);
        Assert.Equal(SurfaceType.DIRT, result[2].Surface);
        Assert.Equal(TrackType.GRADE1, result[2].TrackType);

        // [8-11]: TRACK, DIRT, GRADE4
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
            new RoadClassInterval { FromIndex = 0, ToIndex = 5, RoadClass = RoadClassType.PRIMARY },
            new RoadClassInterval { FromIndex = 5, ToIndex = 9, RoadClass = RoadClassType.TRACK }
        };
        var surfaceIntervals = new[]
        {
            new SurfaceInterval { FromIndex = 0, ToIndex = 5, Surface = SurfaceType.ASPHALT },
            new SurfaceInterval { FromIndex = 5, ToIndex = 9, Surface = SurfaceType.DIRT }
        };
        var trackTypeIntervals = new[]
        {
            new TrackTypeInterval { FromIndex = 0, ToIndex = 5, TrackType = TrackType.GRADE1 },
            new TrackTypeInterval { FromIndex = 5, ToIndex = 9, TrackType = TrackType.GRADE5 }
        };

        // Act
        var result = SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals, trackTypeIntervals);

        // Assert
        // Boundaries: 0, 5, 9 => 2 segments (5 is deduplicated)
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
            new RoadClassInterval { FromIndex = 0, ToIndex = 2, RoadClass = RoadClassType.PRIMARY },
            new RoadClassInterval { FromIndex = 2, ToIndex = 4, RoadClass = RoadClassType.SECONDARY }
        };
        var surfaceIntervals = new[]
        {
            new SurfaceInterval { FromIndex = 0, ToIndex = 4, Surface = SurfaceType.ASPHALT }
        };
        var trackTypeIntervals = new[]
        {
            new TrackTypeInterval { FromIndex = 0, ToIndex = 4, TrackType = TrackType.GRADE1 }
        };

        // Act
        var result = SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals, trackTypeIntervals);

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
            new RoadClassInterval { FromIndex = 0, ToIndex = 9, RoadClass = RoadClassType.PRIMARY }
        };
        var surfaceIntervals = new[]
        {
            new SurfaceInterval { FromIndex = 0, ToIndex = 9, Surface = SurfaceType.ASPHALT }
        };
        var trackTypeIntervals = new[]
        {
            new TrackTypeInterval { FromIndex = 0, ToIndex = 9, TrackType = TrackType.GRADE1 }
        };

        // Act
        var result = SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals, trackTypeIntervals);

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
            new SurfaceInterval { FromIndex = 0, ToIndex = 4, Surface = SurfaceType.ASPHALT }
        };
        var trackTypeIntervals = new[]
        {
            new TrackTypeInterval { FromIndex = 0, ToIndex = 4, TrackType = TrackType.GRADE1 }
        };

        // Act & Assert
        Assert.Throws<ContractViolationException>(() =>
            SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals, trackTypeIntervals));
    }

    [Fact]
    public void Build_EmptySurfaceIntervals_ThrowsContractViolationException()
    {
        // Arrange
        var geometry = CreateGeometry(5);
        var roadClassIntervals = new[]
        {
            new RoadClassInterval { FromIndex = 0, ToIndex = 4, RoadClass = RoadClassType.PRIMARY }
        };
        var surfaceIntervals = Array.Empty<SurfaceInterval>();
        var trackTypeIntervals = new[]
        {
            new TrackTypeInterval { FromIndex = 0, ToIndex = 4, TrackType = TrackType.GRADE1 }
        };

        // Act & Assert
        Assert.Throws<ContractViolationException>(() =>
            SegmentBuilder.Build(geometry, roadClassIntervals, surfaceIntervals, trackTypeIntervals));
    }

    [Fact]
    public void Build_EmptyTrackTypeIntervals_ThrowsContractViolationException()
    {
        // Arrange
        var geometry = CreateGeometry(5);
        var roadClassIntervals = new[]
        {
            new RoadClassInterval { FromIndex = 0, ToIndex = 4, RoadClass = RoadClassType.PRIMARY }
        };
        var surfaceIntervals = new[]
        {
            new SurfaceInterval { FromIndex = 0, ToIndex = 4, Surface = SurfaceType.ASPHALT }
        };
        var trackTypeIntervals = Array.Empty<TrackTypeInterval>();

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
            new RoadClassInterval { FromIndex = 0, ToIndex = 3, RoadClass = RoadClassType.PRIMARY },
            // Gap between 3 and 5
            new RoadClassInterval { FromIndex = 5, ToIndex = 9, RoadClass = RoadClassType.SECONDARY }
        };
        var surfaceIntervals = new[]
        {
            new SurfaceInterval { FromIndex = 0, ToIndex = 9, Surface = SurfaceType.ASPHALT }
        };
        var trackTypeIntervals = new[]
        {
            new TrackTypeInterval { FromIndex = 0, ToIndex = 9, TrackType = TrackType.GRADE1 }
        };

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
        var roadClassIntervals = new[]
        {
            new RoadClassInterval { FromIndex = 0, ToIndex = 9, RoadClass = RoadClassType.PRIMARY }
        };
        var surfaceIntervals = new[]
        {
            new SurfaceInterval { FromIndex = 0, ToIndex = 3, Surface = SurfaceType.ASPHALT },
            // Gap between 3 and 6
            new SurfaceInterval { FromIndex = 6, ToIndex = 9, Surface = SurfaceType.GRAVEL }
        };
        var trackTypeIntervals = new[]
        {
            new TrackTypeInterval { FromIndex = 0, ToIndex = 9, TrackType = TrackType.GRADE1 }
        };

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
        var roadClassIntervals = new[]
        {
            new RoadClassInterval { FromIndex = 0, ToIndex = 9, RoadClass = RoadClassType.PRIMARY }
        };
        var surfaceIntervals = new[]
        {
            new SurfaceInterval { FromIndex = 0, ToIndex = 9, Surface = SurfaceType.ASPHALT }
        };
        var trackTypeIntervals = new[]
        {
            new TrackTypeInterval { FromIndex = 0, ToIndex = 3, TrackType = TrackType.GRADE1 },
            // Gap between 3 and 6
            new TrackTypeInterval { FromIndex = 6, ToIndex = 9, TrackType = TrackType.GRADE4 }
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
