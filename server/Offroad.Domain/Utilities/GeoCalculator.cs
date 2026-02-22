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
