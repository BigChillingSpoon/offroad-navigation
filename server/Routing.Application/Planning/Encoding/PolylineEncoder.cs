using Routing.Domain.ValueObjects;
using System.Text;

namespace Routing.Application.Planning.Encoding
{
    public static class PolylineEncoder
    {
        // Google/GraphHopper compatible polyline encoding
        public static string Encode(IReadOnlyList<Coordinate> points)
        {
            if (points == null || points.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            int prevLat = 0, prevLng = 0;

            foreach (var p in points)
            {
                int lat = (int)Math.Round(p.Latitude * 1e5);
                int lng = (int)Math.Round(p.Longitude * 1e5);

                EncodeValue(lat - prevLat, sb);
                EncodeValue(lng - prevLng, sb);

                prevLat = lat;
                prevLng = lng;
            }

            return sb.ToString();
        }

        private static void EncodeValue(int value, StringBuilder sb)
        {
            value <<= 1;
            if (value < 0)
                value = ~value;

            while (value >= 0x20)
            {
                sb.Append((char)((0x20 | value & 0x1f) + 63));
                value >>= 5;
            }

            sb.Append((char)(value + 63));
        }
    }
}
