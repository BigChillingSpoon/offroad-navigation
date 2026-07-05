using Routing.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace Routing.Domain.Utilities
{
    /// <summary>
    /// Provides geographic calculations for coordinates.
    /// </summary>
    public static class GeoCalculator
    {
        private const double EarthRadiusMeters = 6_371_000;

        /// <summary>
        /// Calculates the total distance of a path defined by a sequence of coordinates.
        /// </summary>
        public static double CalculatePathDistance(IReadOnlyList<Coordinate> coordinates)
        {
            if (coordinates == null || coordinates.Count < 2)
                return 0;

            double totalDistance = 0;

            for (int i = 0; i < coordinates.Count - 1; i++)
            {
                totalDistance += CalculateDistance(coordinates[i], coordinates[i + 1]);
            }

            return totalDistance;
        }

        /// <summary>
        /// Calculates the distance of a sub-path within a geometry, defined by an index range [fromIndex, toIndex].
        /// </summary>
        public static double CalculateRangeDistance(IReadOnlyList<Coordinate> coordinates, int fromIndex, int toIndex)
        {
            if (coordinates == null || fromIndex >= toIndex)
                return 0;

            double totalDistance = 0;

            for (int i = fromIndex; i < toIndex; i++)
            {
                totalDistance += CalculateDistance(coordinates[i], coordinates[i + 1]);
            }

            return totalDistance;
        }

        /// <summary>
        /// Calculates the steepest gradient (%) along a path using a sliding window approach.
        /// Accumulates 2D distance from a start point until it exceeds the minimum chunk distance,
        /// then calculates gradient = |elevationDiff / horizontalDist| * 100.
        /// This avoids GPS jitter on short point-to-point distances.
        /// </summary>
        public static double CalculateMaxGradientPercentage(IReadOnlyList<Coordinate> coordinates, double minChunkDistanceMeters = 50.0)
        {
            if (coordinates == null || coordinates.Count < 2)
                return 0;

            double maxGradient = 0;

            for (int startIdx = 0; startIdx < coordinates.Count - 1; startIdx++)
            {
                if (!coordinates[startIdx].Elevation.HasValue)
                    continue;

                double accumulatedDistance = 0;

                for (int endIdx = startIdx + 1; endIdx < coordinates.Count; endIdx++)
                {
                    accumulatedDistance += CalculateDistance(coordinates[endIdx - 1], coordinates[endIdx]);

                    if (accumulatedDistance < minChunkDistanceMeters)
                        continue;

                    if (!coordinates[endIdx].Elevation.HasValue)
                        break;

                    var elevationDiff = coordinates[endIdx].Elevation!.Value - coordinates[startIdx].Elevation!.Value;
                    var gradient = Math.Abs(elevationDiff / accumulatedDistance) * 100.0;

                    if (gradient > maxGradient)
                        maxGradient = gradient;

                    break;
                }
            }

            return maxGradient;
        }

        /// <summary>
        /// Calculates the great-circle distance between two coordinates using the Haversine formula.
        /// </summary>
        public static double CalculateDistance(Coordinate from, Coordinate to)
        {
            var lat1 = DegreesToRadians(from.Latitude);
            var lat2 = DegreesToRadians(to.Latitude);
            var deltaLat = DegreesToRadians(to.Latitude - from.Latitude);
            var deltaLon = DegreesToRadians(to.Longitude - from.Longitude);

            var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                    Math.Cos(lat1) * Math.Cos(lat2) *
                    Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusMeters * c;
        }

        /// <summary>
        /// Calculates the initial bearing (forward azimuth) from point A to point B.
        /// Returns degrees from 0 to 360.
        /// </summary>
        public static double CalculateBearing(Coordinate from, Coordinate to)
        {
            var lat1 = DegreesToRadians(from.Latitude);
            var lat2 = DegreesToRadians(to.Latitude);
            var dLon = DegreesToRadians(to.Longitude - from.Longitude);

            var y = Math.Sin(dLon) * Math.Cos(lat2);
            var x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);

            var bearingRadians = Math.Atan2(y, x);
            var bearingDegrees = RadiansToDegrees(bearingRadians);

            return (bearingDegrees + 360) % 360;
        }

        /// <summary>
        /// Calculates the absolute difference between two angles (0-360), returning the shortest turn (0-180).
        /// </summary>
        public static double GetAngleDifference(double angle1, double angle2)
        {
            double diff = Math.Abs(angle1 - angle2) % 360;
            return diff > 180 ? 360 - diff : diff;
        }

        /// <summary>
        /// Calculates the cross-track distance (in meters) from a point P to a line segment AB.
        /// Uses a highly optimized Local Flat-Earth projection for fast RAM scoring.
        /// </summary>
        public static bool IsPointInEllipse(Coordinate p, Coordinate a, Coordinate b, double minorAxisRadiusMeters = 250.0)
        {
            // 1. Rychlá Flat-Earth projekce (na metry)
            double latMid = DegreesToRadians(a.Latitude);
            double metersPerDegreeLat = 111132.92;
            double metersPerDegreeLon = 111412.84 * Math.Cos(latMid);

            // Kartézské souøadnice (A je poèátek [0,0])
            double bx = (b.Longitude - a.Longitude) * metersPerDegreeLon;
            double by = (b.Latitude - a.Latitude) * metersPerDegreeLat;
            double px = (p.Longitude - a.Longitude) * metersPerDegreeLon;
            double py = (p.Latitude - a.Latitude) * metersPerDegreeLat;

            // 2. Výpoèet vzdáleností (A->B, A->P, P->B)
            double distAB = Math.Sqrt(bx * bx + by * by);
            double distAP = Math.Sqrt(px * px + py * py);
            double distPB = Math.Sqrt((px - bx) * (px - bx) + (py - by) * (py - by));

            if (distAB == 0) return distAP <= minorAxisRadiusMeters;

            // 3. Matematika Elipsy (Hledáme maximální délku cesty pøes bod P)
            double c = distAB / 2.0; // Vzdálenost od støedu k ohnisku
            double bRadius = minorAxisRadiusMeters; // Tvoje požadovaná šíøka v nejširším bodì

            // Vzorec pro elipsu: a^2 = b^2 + c^2 (kde 'a' je polovina délky provázku)
            double aAxis = Math.Sqrt(bRadius * bRadius + c * c);

            // Maximální povolený souèet vzdáleností (Délka "provázku" elipsy)
            double maxAllowedPathLength = 2 * aAxis;

            // 4. Finální zhodnocení: Je zajížïka pøes bod P v rámci budgetu naší elipsy?
            return (distAP + distPB) <= maxAllowedPathLength;
        }
        private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
        private static double RadiansToDegrees(double radians) => radians * 180 / Math.PI;
    }
}