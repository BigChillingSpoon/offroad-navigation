using System.Globalization;

namespace Offroad.IntegrationTests.Infrastructure;

/// <summary>
/// Builds minimal but structurally valid GraphHopper /route JSON responses.
/// The encoded polyline must be pre-generated via <c>PolylineEncoder.Encode</c>.
/// </summary>
public static class GraphHopperResponseBuilder
{
    public static string Success(
        string encodedPolyline,
        int pointCount,
        double distance = 500.0,
        double timeMs = 120_000,
        double ascend = 20.0,
        double descend = 5.0,
        string? customBarrierJson = null,
        string? roadAccessJson = null)
    {
        var last = pointCount - 1;
        var barriers = customBarrierJson ?? $"""[[0, {last}, "none"]]""";
        var access = roadAccessJson ?? $"""[[0, {last}, "yes"]]""";

        // Escape backslashes that may appear in encoded polylines (char 92 is in the valid range)
        var safePolyline = encodedPolyline.Replace(@"\", @"\\");

        return $$"""
        {
          "paths": [{
            "distance": {{F(distance)}},
            "time": {{F(timeMs)}},
            "ascend": {{F(ascend)}},
            "descend": {{F(descend)}},
            "points": "{{safePolyline}}",
            "points_encoded_multiplier": 100000,
            "details": {
              "surface":          [[0, {{last}}, "gravel"]],
              "road_class":       [[0, {{last}}, "track"]],
              "track_type":       [[0, {{last}}, "grade3"]],
              "road_access":      {{access}},
              "car_access":       [[0, {{last}}, true]],
              "road_environment": [[0, {{last}}, "road"]],
              "custom_barrier":   {{barriers}}
            }
          }]
        }
        """;
    }

    private static string F(double v) => v.ToString(CultureInfo.InvariantCulture);
}
