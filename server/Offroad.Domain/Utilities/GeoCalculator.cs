using Routing.Domain.ValueObjects;

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
        /// Haversine is not perfectly precise but for our purposes its okay, could be changed in the future
        /// </summary>
        /// <returns>Distance in meters</returns>
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

        private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
    }
}
