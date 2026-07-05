using Routing.Domain.ValueObjects;
using System;

namespace Routing.Application.Planning.Skeletons.Helpers;

/// <summary>
/// Provides pure, static methods for geometric and trigonometric calculations required by the skeleton finder.
/// </summary>
public static class GeometryHelpers
{
    private const double EarthRadiusMeters = 6371000;

    /// <summary>
    /// Calculates the Haversine (great-circle) distance between two coordinates in meters.
    /// </summary>
    public static double CalculateEuclideanDistance(Coordinate p1, Coordinate p2)
    {
        var dLat = ToRadians(p2.Latitude - p1.Latitude);
        var dLon = ToRadians(p2.Longitude - p1.Longitude);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(p1.Latitude)) * Math.Cos(ToRadians(p2.Latitude)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    /// <summary>
    /// Calculates the initial bearing (heading) when moving from p1 to p2 in degrees (0-360).
    /// </summary>
    public static double CalculateBearing(Coordinate p1, Coordinate p2)
    {
        var lat1 = ToRadians(p1.Latitude);
        var lat2 = ToRadians(p2.Latitude);
        var dLon = ToRadians(p2.Longitude - p1.Longitude);

        var y = Math.Sin(dLon) * Math.Cos(lat2);
        var x = Math.Cos(lat1) * Math.Sin(lat2) -
                Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);

        var bearing = Math.Atan2(y, x);
        return (ToDegrees(bearing) + 360) % 360;
    }

    /// <summary>
    /// Calculates the absolute difference between two bearings, handling wraparound at 360 degrees.
    /// </summary>
    public static double CalculateAngleDifference(double bearing1, double bearing2)
    {
        var diff = Math.Abs(bearing1 - bearing2) % 360;
        return diff > 180 ? 360 - diff : diff;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
    private static double ToDegrees(double radians) => radians * 180 / Math.PI;
}
