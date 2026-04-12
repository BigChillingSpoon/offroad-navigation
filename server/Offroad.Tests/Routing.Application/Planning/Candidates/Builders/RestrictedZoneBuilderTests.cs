using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Routing.Application.Contracts;
using Routing.Application.Planning.Candidates.Builders;
using Routing.Domain.Enums;
using Routing.Domain.ValueObjects;
using Coordinate = Routing.Domain.ValueObjects.Coordinate;

namespace Offroad.Tests.Routing.Application.Planning.Candidates.Builders;

public class RestrictedZoneBuilderTests
{
    #region Empty / No Restriction Tests

    [Fact]
    public async Task Build_EmptyGeometry_ReturnsEmptyList()
    {
        // Arrange
        var sut = new RestrictedZoneBuilder(new FakeGisService(new FeatureCollection()));
        var geometry = new List<Coordinate>();
        var roadAccessIntervals = Array.Empty<Interval<RoadAccessType>>();

        // Act
        var result = await sut.BuildAsync(roadAccessIntervals, geometry);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task Build_NoRestrictions_ReturnsEmptyList()
    {
        // Arrange
        var sut = new RestrictedZoneBuilder(new FakeGisService(CreateMockParksCollection(100.0, 100.0, 101.0, 101.0)));
        var geometry = CreateGeometry(10);
        var roadAccessIntervals = new[]
        {
            new Interval<RoadAccessType>(0, 9, RoadAccessType.Yes)
        };

        // Act
        var result = await sut.BuildAsync(roadAccessIntervals, geometry);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Base Layer (Road Access) Tests

    [Fact]
    public async Task Build_OnlyBaseLayer_ReturnsCorrectZones()
    {
        // Arrange
        var sut = new RestrictedZoneBuilder(new FakeGisService(new FeatureCollection()));
        var geometry = CreateGeometry(10);
        var roadAccessIntervals = new[]
        {
            new Interval<RoadAccessType>(0, 1, RoadAccessType.Yes),
            new Interval<RoadAccessType>(2, 5, RoadAccessType.Forestry),
            new Interval<RoadAccessType>(6, 9, RoadAccessType.Yes)
        };

        // Act
        var result = await sut.BuildAsync(roadAccessIntervals, geometry);

        // Assert
        Assert.Single(result);
        Assert.Equal(RestrictionType.Forestry, result[0].Value);
        Assert.Equal(2, result[0].FromIndex);
        Assert.Equal(5, result[0].ToIndex);
    }

    [Fact]
    public async Task Build_RouteEndsInsideRestriction_ClosesZoneCorrectly()
    {
        // Arrange
        var sut = new RestrictedZoneBuilder(new FakeGisService(new FeatureCollection()));
        var geometry = CreateGeometry(11);
        var roadAccessIntervals = new[]
        {
            new Interval<RoadAccessType>(0, 4, RoadAccessType.Yes),
            new Interval<RoadAccessType>(5, 10, RoadAccessType.Private)
        };

        // Act
        var result = await sut.BuildAsync(roadAccessIntervals, geometry);

        // Assert
        Assert.Single(result);
        Assert.Equal(RestrictionType.Private, result[0].Value);
        Assert.Equal(5, result[0].FromIndex);
        Assert.Equal(10, result[0].ToIndex);
    }

    [Fact]
    public async Task Build_UnknownAccess_MappedAsUnknown()
    {
        // Arrange
        var sut = new RestrictedZoneBuilder(new FakeGisService(new FeatureCollection()));
        var geometry = CreateGeometry(5);
        var roadAccessIntervals = new[]
        {
            new Interval<RoadAccessType>(0, 4, RoadAccessType.Unknown)
        };

        // Act
        var result = await sut.BuildAsync(roadAccessIntervals, geometry);

        // Assert
        Assert.Single(result);
        Assert.Equal(RestrictionType.Unknown, result[0].Value);
        Assert.Equal(0, result[0].FromIndex);
        Assert.Equal(4, result[0].ToIndex);
    }

    #endregion

    #region Top Layer (National Parks) Tests

    [Fact]
    public async Task Build_OnlyTopLayer_ReturnsCorrectZones()
    {
        // Arrange
        var sut = new RestrictedZoneBuilder(new FakeGisService(CreateMockParksCollection(
            minLon: 0.0035, minLat: 0.5,
            maxLon: 0.0065, maxLat: 1.5)));
        var geometry = CreateGeometryOnLine(11, latitude: 1.0, startLongitude: 0.0, step: 0.001);
        var roadAccessIntervals = new[]
        {
            new Interval<RoadAccessType>(0, 10, RoadAccessType.Yes)
        };

        // Act
        var result = await sut.BuildAsync(roadAccessIntervals, geometry);

        // Assert
        Assert.Single(result);
        Assert.Equal(RestrictionType.NationalPark, result[0].Value);
        Assert.Equal(4, result[0].FromIndex);
        Assert.Equal(6, result[0].ToIndex);
    }

    #endregion

    #region Painter's Algorithm Tests

    [Fact]
    public async Task Build_PaintersAlgorithm_Overlap_OverwritesCorrectly()
    {
        // Arrange
        var sut = new RestrictedZoneBuilder(new FakeGisService(CreateMockParksCollection(
            minLon: 0.0035, minLat: 0.5,
            maxLon: 0.0065, maxLat: 1.5)));
        var geometry = CreateGeometryOnLine(11, latitude: 1.0, startLongitude: 0.0, step: 0.001);
        var roadAccessIntervals = new[]
        {
            new Interval<RoadAccessType>(0, 10, RoadAccessType.Forestry)
        };

        // Act
        var result = await sut.BuildAsync(roadAccessIntervals, geometry);

        // Assert
        Assert.Equal(3, result.Count);

        // Forestry before park
        Assert.Equal(RestrictionType.Forestry, result[0].Value);
        Assert.Equal(0, result[0].FromIndex);
        Assert.Equal(3, result[0].ToIndex);

        // National Park overlay
        Assert.Equal(RestrictionType.NationalPark, result[1].Value);
        Assert.Equal(4, result[1].FromIndex);
        Assert.Equal(6, result[1].ToIndex);

        // Forestry after park
        Assert.Equal(RestrictionType.Forestry, result[2].Value);
        Assert.Equal(7, result[2].FromIndex);
        Assert.Equal(10, result[2].ToIndex);
    }

    [Fact]
    public async Task Build_PaintersAlgorithm_ParkCoversEntireBaseLayer_ReturnsOnlyParkZone()
    {
        // Arrange
        var sut = new RestrictedZoneBuilder(new FakeGisService(CreateMockParksCollection(
            minLon: -1.0, minLat: 0.0,
            maxLon: 1.0, maxLat: 2.0)));
        var geometry = CreateGeometryOnLine(5, latitude: 1.0, startLongitude: 0.0, step: 0.001);
        var roadAccessIntervals = new[]
        {
            new Interval<RoadAccessType>(0, 4, RoadAccessType.Private)
        };

        // Act
        var result = await sut.BuildAsync(roadAccessIntervals, geometry);

        // Assert
        Assert.Single(result);
        Assert.Equal(RestrictionType.NationalPark, result[0].Value);
        Assert.Equal(0, result[0].FromIndex);
        Assert.Equal(4, result[0].ToIndex);
    }

    #endregion

    #region Helper Methods

    private static List<Coordinate> CreateGeometry(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => new Coordinate(i * 0.001, i * 0.001))
            .ToList();
    }

    private static List<Coordinate> CreateGeometryOnLine(int count, double latitude, double startLongitude, double step)
    {
        return Enumerable.Range(0, count)
            .Select(i => new Coordinate(latitude, startLongitude + i * step))
            .ToList();
    }

    private static FeatureCollection CreateMockParksCollection(double minLon, double minLat, double maxLon, double maxLat)
    {
        var factory = new GeometryFactory();
        var ring = factory.CreateLinearRing(new[]
        {
            new NetTopologySuite.Geometries.Coordinate(minLon, minLat),
            new NetTopologySuite.Geometries.Coordinate(maxLon, minLat),
            new NetTopologySuite.Geometries.Coordinate(maxLon, maxLat),
            new NetTopologySuite.Geometries.Coordinate(minLon, maxLat),
            new NetTopologySuite.Geometries.Coordinate(minLon, minLat)
        });
        var polygon = factory.CreatePolygon(ring);

        var collection = new FeatureCollection();
        collection.Add(new Feature(polygon, new AttributesTable()));
        return collection;
    }

    #endregion

    #region Fakes

    // Tato tøída simuluje chování PostGISu pøímo v pamìti pro úèely testování
    private class FakeGisService : IGisService
    {
        private readonly List<Polygon> _parks;

        public FakeGisService(FeatureCollection features)
        {
            _parks = features.Select(f => f.Geometry as Polygon).Where(p => p != null).ToList()!;
        }

        public Task<List<Polygon>> GetRestrictedZonesInAreaAsync(Geometry routeBoundingBox)
        {
            // Vrací pouze ty parky, které se protnou s trasou
            var intersectingParks = _parks.Where(p => p.Intersects(routeBoundingBox)).ToList();
            return Task.FromResult(intersectingParks);
        }
    }

    #endregion
}