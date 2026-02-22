using Routing.Application.Planning.Encoding;
using Routing.Domain.ValueObjects;

namespace Offroad.Tests.Routing.Application.Planning.Encoding;

public class PolylineEncoderTests
{
    private const double DefaultMultiplier = 1e5;
    private const double DefaultElevationMultiplier = 100.0;
    private const double CoordinatePrecision = 0.00001;
    private const double ElevationPrecision = 0.01;

    #region Basic Encoding Tests

    [Fact]
    public void Encode_EmptyCoordinates_ReturnsEmptyPoints()
    {
        // Arrange
        var coordinates = Array.Empty<Coordinate>();

        // Act
        var result = PolylineEncoder.Encode(coordinates, DefaultMultiplier, DefaultElevationMultiplier, hasElevation: false);

        // Assert
        Assert.Equal(string.Empty, result.Points);
    }

    [Fact]
    public void Encode_NullCoordinates_ReturnsEmptyPoints()
    {
        // Act
        var result = PolylineEncoder.Encode(null!, DefaultMultiplier, DefaultElevationMultiplier, hasElevation: false);

        // Assert
        Assert.Equal(string.Empty, result.Points);
    }

    [Fact]
    public void Encode_SingleCoordinate_ReturnsNonEmptyPoints()
    {
        // Arrange
        var coordinates = new[] { new Coordinate(38.5, -120.2) };

        // Act
        var result = PolylineEncoder.Encode(coordinates, DefaultMultiplier, DefaultElevationMultiplier, hasElevation: false);

        // Assert
        Assert.NotEmpty(result.Points);
    }

    [Fact]
    public void Encode_ReturnsCorrectMetadata()
    {
        // Arrange
        var coordinates = new[] { new Coordinate(50.0, 14.0, 250.0) };

        // Act
        var result = PolylineEncoder.Encode(coordinates, DefaultMultiplier, DefaultElevationMultiplier, hasElevation: true);

        // Assert
        Assert.Equal(DefaultMultiplier, result.Multiplier);
        Assert.Equal(DefaultElevationMultiplier, result.ElevationMultiplier);
        Assert.True(result.HasElevation);
    }

    #endregion

    #region 2D Round-Trip Tests

    [Fact]
    public void Encode_TwoDimensional_RoundTrip_ReturnsOriginalCoordinates()
    {
        // Arrange
        var original = new[]
        {
            new Coordinate(38.5, -120.2),
            new Coordinate(40.7, -120.95),
            new Coordinate(43.252, -126.453)
        };

        // Act
        var encoded = PolylineEncoder.Encode(original, DefaultMultiplier, DefaultElevationMultiplier, hasElevation: false);
        var decoded = PolylineDecoder.Decode(encoded);

        // Assert
        Assert.Equal(original.Length, decoded.Count);
        for (int i = 0; i < original.Length; i++)
        {
            Assert.Equal(original[i].Latitude, decoded[i].Latitude, CoordinatePrecision);
            Assert.Equal(original[i].Longitude, decoded[i].Longitude, CoordinatePrecision);
        }
    }

    [Fact]
    public void Encode_TwoDimensional_NegativeCoordinates_RoundTrip_ReturnsOriginalCoordinates()
    {
        // Arrange
        var original = new[]
        {
            new Coordinate(-33.8688, 151.2093), // Sydney
            new Coordinate(-37.8136, 144.9631)  // Melbourne
        };

        // Act
        var encoded = PolylineEncoder.Encode(original, DefaultMultiplier, DefaultElevationMultiplier, hasElevation: false);
        var decoded = PolylineDecoder.Decode(encoded);

        // Assert
        Assert.Equal(original.Length, decoded.Count);
        for (int i = 0; i < original.Length; i++)
        {
            Assert.Equal(original[i].Latitude, decoded[i].Latitude, CoordinatePrecision);
            Assert.Equal(original[i].Longitude, decoded[i].Longitude, CoordinatePrecision);
        }
    }

    #endregion

    #region 3D Round-Trip Tests

    [Fact]
    public void Encode_ThreeDimensional_RoundTrip_ReturnsOriginalCoordinates()
    {
        // Arrange
        var original = new[]
        {
            new Coordinate(50.0, 14.0, 250.0),
            new Coordinate(50.1, 14.1, 300.0),
            new Coordinate(50.2, 14.2, 275.5)
        };

        // Act
        var encoded = PolylineEncoder.Encode(original, DefaultMultiplier, DefaultElevationMultiplier, hasElevation: true);
        var decoded = PolylineDecoder.Decode(encoded);

        // Assert
        Assert.Equal(original.Length, decoded.Count);
        for (int i = 0; i < original.Length; i++)
        {
            Assert.Equal(original[i].Latitude, decoded[i].Latitude, CoordinatePrecision);
            Assert.Equal(original[i].Longitude, decoded[i].Longitude, CoordinatePrecision);
            Assert.Equal(original[i].Elevation!.Value, decoded[i].Elevation!.Value, ElevationPrecision);
        }
    }

    [Fact]
    public void Encode_ThreeDimensional_WithNegativeElevation_RoundTrip_ReturnsOriginalCoordinates()
    {
        // Arrange - Dead Sea area
        var original = new[]
        {
            new Coordinate(31.5, 35.5, -400.0),
            new Coordinate(31.6, 35.6, -380.0)
        };

        // Act
        var encoded = PolylineEncoder.Encode(original, DefaultMultiplier, DefaultElevationMultiplier, hasElevation: true);
        var decoded = PolylineDecoder.Decode(encoded);

        // Assert
        Assert.Equal(original.Length, decoded.Count);
        for (int i = 0; i < original.Length; i++)
        {
            Assert.Equal(original[i].Latitude, decoded[i].Latitude, CoordinatePrecision);
            Assert.Equal(original[i].Longitude, decoded[i].Longitude, CoordinatePrecision);
            Assert.Equal(original[i].Elevation!.Value, decoded[i].Elevation!.Value, ElevationPrecision);
        }
    }

    [Fact]
    public void Encode_ThreeDimensional_ManyPoints_RoundTrip_ReturnsOriginalCoordinates()
    {
        // Arrange - Simulate a route with varying elevation
        var original = Enumerable.Range(0, 100)
            .Select(i => new Coordinate(
                50.0 + i * 0.01,
                14.0 + i * 0.01,
                200.0 + Math.Sin(i * 0.1) * 50.0))
            .ToArray();

        // Act
        var encoded = PolylineEncoder.Encode(original, DefaultMultiplier, DefaultElevationMultiplier, hasElevation: true);
        var decoded = PolylineDecoder.Decode(encoded);

        // Assert
        Assert.Equal(original.Length, decoded.Count);
        for (int i = 0; i < original.Length; i++)
        {
            Assert.Equal(original[i].Latitude, decoded[i].Latitude, CoordinatePrecision);
            Assert.Equal(original[i].Longitude, decoded[i].Longitude, CoordinatePrecision);
            Assert.Equal(original[i].Elevation!.Value, decoded[i].Elevation!.Value, ElevationPrecision);
        }
    }

    #endregion

    #region Multiplier Tests

    [Theory]
    [InlineData(1e5)]
    [InlineData(1e6)]
    [InlineData(1e7)]
    public void Encode_DifferentMultipliers_RoundTrip_ReturnsOriginalCoordinates(double multiplier)
    {
        // Arrange
        var original = new[]
        {
            new Coordinate(50.123456, 14.654321),
            new Coordinate(50.234567, 14.765432)
        };

        // Act
        var encoded = PolylineEncoder.Encode(original, multiplier, DefaultElevationMultiplier, hasElevation: false);
        var decoded = PolylineDecoder.Decode(encoded);

        // Assert
        double precision = 1.0 / multiplier;
        Assert.Equal(original.Length, decoded.Count);
        for (int i = 0; i < original.Length; i++)
        {
            Assert.Equal(original[i].Latitude, decoded[i].Latitude, precision);
            Assert.Equal(original[i].Longitude, decoded[i].Longitude, precision);
        }
    }

    #endregion

    #region Encoding Consistency Tests

    [Fact]
    public void Encode_WithoutElevation_ProducesShorterPointsThan_WithElevation()
    {
        // Arrange
        var coordinates = new[]
        {
            new Coordinate(50.0, 14.0, 250.0),
            new Coordinate(50.1, 14.1, 300.0)
        };

        // Act
        var encoded2D = PolylineEncoder.Encode(coordinates, DefaultMultiplier, DefaultElevationMultiplier, hasElevation: false);
        var encoded3D = PolylineEncoder.Encode(coordinates, DefaultMultiplier, DefaultElevationMultiplier, hasElevation: true);

        // Assert
        Assert.True(encoded2D.Points.Length < encoded3D.Points.Length,
            $"2D encoding ({encoded2D.Points.Length} chars) should be shorter than 3D ({encoded3D.Points.Length} chars)");
    }

    [Fact]
    public void Encode_WithoutElevation_IgnoresElevationValues()
    {
        // Arrange
        var withElevation = new[] { new Coordinate(50.0, 14.0, 500.0) };
        var withoutElevation = new[] { new Coordinate(50.0, 14.0, 0.0) };

        // Act
        var encodedWith = PolylineEncoder.Encode(withElevation, DefaultMultiplier, DefaultElevationMultiplier, hasElevation: false);
        var encodedWithout = PolylineEncoder.Encode(withoutElevation, DefaultMultiplier, DefaultElevationMultiplier, hasElevation: false);

        // Assert
        Assert.Equal(encodedWith.Points, encodedWithout.Points);
    }

    #endregion
}
