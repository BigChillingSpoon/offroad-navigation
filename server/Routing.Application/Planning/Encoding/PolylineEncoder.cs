using Routing.Domain.ValueObjects;
using System.Text;

namespace Routing.Application.Planning.Encoding
{
    public static class PolylineEncoder
    {
        public static EncodedPolyline Encode(
            IReadOnlyList<Coordinate> coordinates,
            double multiplier,
            double elevationMultiplier,
            bool hasElevation)
        {
            var points = EncodePoints(coordinates, multiplier, elevationMultiplier, hasElevation);

            return new EncodedPolyline
            {
                Points = points,
                Multiplier = multiplier,
                ElevationMultiplier = elevationMultiplier,
                HasElevation = hasElevation
            };
        }

        private static string EncodePoints(
            IReadOnlyList<Coordinate> coordinates,
            double multiplier,
            double elevationMultiplier,
            bool hasElevation)
        {
            if (coordinates == null || coordinates.Count == 0)
                return string.Empty;

            var result = new StringBuilder();
            int prevLat = 0, prevLng = 0, prevElev = 0;

            foreach (var coord in coordinates)
            {
                int lat = (int)Math.Round(coord.Latitude * multiplier);
                int lng = (int)Math.Round(coord.Longitude * multiplier);

                EncodeValue(lat - prevLat, result);
                EncodeValue(lng - prevLng, result);

                if (hasElevation)
                {
                    int elev = (int)Math.Round((coord.Elevation ?? 0) * elevationMultiplier);
                    EncodeValue(elev - prevElev, result);
                    prevElev = elev;
                }

                prevLat = lat;
                prevLng = lng;
            }

            return result.ToString();
        }

        private static void EncodeValue(int value, StringBuilder result)
        {
            value <<= 1;
            if (value < 0)
                value = ~value;

            while (value >= 0x20)
            {
                result.Append((char)((0x20 | value & 0x1f) + 63));
                value >>= 5;
            }

            result.Append((char)(value + 63));
        }
    }
}
