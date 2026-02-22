using Routing.Application.Planning.Encoding;
using Routing.Application.Planning.Exceptions;
using Routing.Domain.ValueObjects;

namespace Offroad.Tests.Routing.Application.Planning.Encoding;

public class PolylineDecoderTests
{
    private const double DefaultMultiplier = 1e5;
    private const double DefaultElevationMultiplier = 100.0;
    private const double CoordinatePrecision = 0.00001;
    private const double ElevationPrecision = 0.01;

    #region Basic Decoding Tests

    [Fact]
    public void Decode_EmptyPoints_ReturnsEmptyList()
    {
        // Arrange
        var polyline = new EncodedPolyline
        {
            Points = string.Empty,
            Multiplier = DefaultMultiplier,
            ElevationMultiplier = DefaultElevationMultiplier,
            HasElevation = false
        };

        // Act
        var result = PolylineDecoder.Decode(polyline);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Decode_NullPolyline_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PolylineDecoder.Decode(null!));
    }

    [Fact]
    public void Decode_KnownGooglePolyline_ReturnsCorrectCoordinates()
    {
        // Arrange - Known Google polyline example: "_p~iF~ps|U_ulLnnqC_mqNvxq`@"
        // Represents: (38.5, -120.2), (40.7, -120.95), (43.252, -126.453)
        var polyline = new EncodedPolyline
        {
            Points = "_p~iF~ps|U_ulLnnqC_mqNvxq`@",
            Multiplier = DefaultMultiplier,
            ElevationMultiplier = DefaultElevationMultiplier,
            HasElevation = false
        };

        // Act
        var result = PolylineDecoder.Decode(polyline);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(38.5, result[0].Latitude, CoordinatePrecision);
        Assert.Equal(-120.2, result[0].Longitude, CoordinatePrecision);
        Assert.Equal(40.7, result[1].Latitude, CoordinatePrecision);
        Assert.Equal(-120.95, result[1].Longitude, CoordinatePrecision);
        Assert.Equal(43.252, result[2].Latitude, CoordinatePrecision);
        Assert.Equal(-126.453, result[2].Longitude, CoordinatePrecision);
    }

    [Fact]
    public void Decode_SinglePoint_ReturnsSingleCoordinate()
    {
        // Arrange
        var original = new[] { new Coordinate(50.0, 14.0) };
        var encoded = PolylineEncoder.Encode(original, DefaultMultiplier, DefaultElevationMultiplier, hasElevation: false);

        // Act
        var result = PolylineDecoder.Decode(encoded);

        // Assert
        Assert.Single(result);
        Assert.Equal(original[0].Latitude, result[0].Latitude, CoordinatePrecision);
        Assert.Equal(original[0].Longitude, result[0].Longitude, CoordinatePrecision);
    }

    #endregion

    #region 2D Decoding Tests

    [Fact]
    public void Decode_WithoutElevation_ReturnsCoordinatesWithNullElevation()
    {
        // Arrange
        var original = new[]
        {
            new Coordinate(50.0, 14.0),
            new Coordinate(50.1, 14.1)
        };
        var encoded = PolylineEncoder.Encode(original, DefaultMultiplier, DefaultElevationMultiplier, hasElevation: false);

        // Act
        var result = PolylineDecoder.Decode(encoded);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Null(result[0].Elevation);
        Assert.Null(result[1].Elevation);
    }

    [Fact]
    public void Decode_NegativeCoordinates_ReturnsCorrectValues()
    {
        // Arrange - Southern hemisphere
        var original = new[]
        {
            new Coordinate(-33.8688, 151.2093),
            new Coordinate(-37.8136, 144.9631)
        };
        var encoded = PolylineEncoder.Encode(original, DefaultMultiplier, DefaultElevationMultiplier, hasElevation: false);

        // Act
        var result = PolylineDecoder.Decode(encoded);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(original[0].Latitude, result[0].Latitude, CoordinatePrecision);
        Assert.Equal(original[0].Longitude, result[0].Longitude, CoordinatePrecision);
        Assert.Equal(original[1].Latitude, result[1].Latitude, CoordinatePrecision);
        Assert.Equal(original[1].Longitude, result[1].Longitude, CoordinatePrecision);
    }

    #endregion

    #region 3D Decoding Tests

    [Fact]
    public void Decode_WithElevation_ReturnsCoordinatesWithElevation()
    {
        // Arrange
        var original = new[]
        {
            new Coordinate(50.0, 14.0, 250.0),
            new Coordinate(50.1, 14.1, 300.0),
            new Coordinate(50.2, 14.2, 275.5)
        };
        var encoded = PolylineEncoder.Encode(original, DefaultMultiplier, DefaultElevationMultiplier, hasElevation: true);

        // Act
        var result = PolylineDecoder.Decode(encoded);

        // Assert
        Assert.Equal(3, result.Count);
        for (int i = 0; i < original.Length; i++)
        {
            Assert.Equal(original[i].Latitude, result[i].Latitude, CoordinatePrecision);
            Assert.Equal(original[i].Longitude, result[i].Longitude, CoordinatePrecision);
            Assert.Equal(original[i].Elevation!.Value, result[i].Elevation!.Value, ElevationPrecision);
        }
    }

    [Fact]
    public void Decode_NegativeElevation_ReturnsCorrectValues()
    {
        // Arrange - Below sea level (e.g., Dead Sea)
        var original = new[]
        {
            new Coordinate(31.5, 35.5, -400.0),
            new Coordinate(31.6, 35.6, -380.0)
        };
        var encoded = PolylineEncoder.Encode(original, DefaultMultiplier, DefaultElevationMultiplier, hasElevation: true);

        // Act
        var result = PolylineDecoder.Decode(encoded);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(original[0].Elevation!.Value, result[0].Elevation!.Value, ElevationPrecision);
        Assert.Equal(original[1].Elevation!.Value, result[1].Elevation!.Value, ElevationPrecision);
    }

    [Fact]
    public void Decode_HighElevation_ReturnsCorrectValues()
    {
        // Arrange - High altitude (e.g., Himalayan route)
        var original = new[]
        {
            new Coordinate(27.9881, 86.9250, 5364.0),  // Everest Base Camp
            new Coordinate(28.0025, 86.8528, 5500.0)
        };
        var encoded = PolylineEncoder.Encode(original, DefaultMultiplier, DefaultElevationMultiplier, hasElevation: true);

        // Act
        var result = PolylineDecoder.Decode(encoded);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(original[0].Elevation!.Value, result[0].Elevation!.Value, ElevationPrecision);
        Assert.Equal(original[1].Elevation!.Value, result[1].Elevation!.Value, ElevationPrecision);
    }

    #endregion

    #region Multiplier Tests

    [Theory]
    [InlineData(1e5)]
    [InlineData(1e6)]
    [InlineData(1e7)]
    public void Decode_DifferentMultipliers_ReturnsCorrectCoordinates(double multiplier)
    {
        // Arrange
        var original = new[]
        {
            new Coordinate(50.123456, 14.654321),
            new Coordinate(50.234567, 14.765432)
        };
        var encoded = PolylineEncoder.Encode(original, multiplier, DefaultElevationMultiplier, hasElevation: false);

        // Act
        var result = PolylineDecoder.Decode(encoded);

        // Assert
        double precision = 1.0 / multiplier;
        Assert.Equal(original.Length, result.Count);
        for (int i = 0; i < original.Length; i++)
        {
            Assert.Equal(original[i].Latitude, result[i].Latitude, precision);
            Assert.Equal(original[i].Longitude, result[i].Longitude, precision);
        }
    }

    [Fact]
    public void Decode_HigherMultiplier_ProducesMorePreciseCoordinates()
    {
        // Arrange
        var original = new[] { new Coordinate(50.1234567, 14.7654321) };

        var encoded1e5 = PolylineEncoder.Encode(original, 1e5, DefaultElevationMultiplier, hasElevation: false);
        var encoded1e7 = PolylineEncoder.Encode(original, 1e7, DefaultElevationMultiplier, hasElevation: false);

        // Act
        var decoded1e5 = PolylineDecoder.Decode(encoded1e5);
        var decoded1e7 = PolylineDecoder.Decode(encoded1e7);

        // Assert - 1e7 should be closer to original
        var error1e5 = Math.Abs(original[0].Latitude - decoded1e5[0].Latitude);
        var error1e7 = Math.Abs(original[0].Latitude - decoded1e7[0].Latitude);

        Assert.True(error1e7 <= error1e5,
            $"Higher multiplier should produce equal or better precision. 1e5 error: {error1e5}, 1e7 error: {error1e7}");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void Decode_InvalidPolyline_ThrowsInvalidPolylineException()
    {
        // Arrange - String with continuation bit set but no terminating character
        var polyline = new EncodedPolyline
        {
            Points = "oooo",
            Multiplier = DefaultMultiplier,
            ElevationMultiplier = DefaultElevationMultiplier,
            HasElevation = false
        };

        // Act & Assert
        Assert.Throws<InvalidPolylineException>(() => PolylineDecoder.Decode(polyline));
    }

    [Fact]
    public void Decode_TruncatedPolyline_ThrowsInvalidPolylineException()
    {
        // Arrange - Create valid polyline and truncate it
        var original = new[]
        {
            new Coordinate(50.0, 14.0),
            new Coordinate(50.1, 14.1)
        };
        var encoded = PolylineEncoder.Encode(original, DefaultMultiplier, DefaultElevationMultiplier, hasElevation: false);
        var truncated = new EncodedPolyline
        {
            Points = encoded.Points.Substring(0, encoded.Points.Length / 2),
            Multiplier = encoded.Multiplier,
            ElevationMultiplier = encoded.ElevationMultiplier,
            HasElevation = encoded.HasElevation
        };

        // Act & Assert
        Assert.Throws<InvalidPolylineException>(() => PolylineDecoder.Decode(truncated));
    }

    [Fact]
    public void Decode_InvalidPolylineException_ContainsInnerException()
    {
        // Arrange
        var polyline = new EncodedPolyline
        {
            Points = "!!!",
            Multiplier = DefaultMultiplier,
            ElevationMultiplier = DefaultElevationMultiplier,
            HasElevation = false
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidPolylineException>(() => PolylineDecoder.Decode(polyline));
        Assert.NotNull(exception.InnerException);
    }

    #endregion

    #region Large Dataset Tests

    [Fact]
    public void Decode_LargePolyline_ReturnsAllCoordinates()
    {
        // Arrange - 1000 points
        var original = Enumerable.Range(0, 1000)
            .Select(i => new Coordinate(
                50.0 + i * 0.001,
                14.0 + i * 0.001))
            .ToArray();
        var encoded = PolylineEncoder.Encode(original, DefaultMultiplier, DefaultElevationMultiplier, hasElevation: false);

        // Act
        var result = PolylineDecoder.Decode(encoded);

        // Assert
        Assert.Equal(1000, result.Count);
    }

    [Fact]
    public void Decode_LargePolylineWithElevation_ReturnsAllCoordinatesWithElevation()
    {
        // Arrange - 500 points with elevation
        var original = Enumerable.Range(0, 500)
            .Select(i => new Coordinate(
                50.0 + i * 0.001,
                14.0 + i * 0.001,
                200.0 + Math.Sin(i * 0.05) * 100.0))
            .ToArray();
        var encoded = PolylineEncoder.Encode(original, DefaultMultiplier, DefaultElevationMultiplier, hasElevation: true);

        // Act
        var result = PolylineDecoder.Decode(encoded);

        // Assert
        Assert.Equal(500, result.Count);
        for (int i = 0; i < original.Length; i++)
        {
            Assert.Equal(original[i].Elevation!.Value, result[i].Elevation!.Value, ElevationPrecision);
        }
    }

    #endregion
}
