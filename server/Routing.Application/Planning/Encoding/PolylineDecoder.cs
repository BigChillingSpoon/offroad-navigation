using Routing.Application.Planning.Exceptions;
using Routing.Domain.ValueObjects;

namespace Routing.Application.Planning.Encoding
{
    public static class PolylineDecoder
    {
        public static IReadOnlyList<Coordinate> Decode(EncodedPolyline polyline)
        {
            if (polyline is null)
                throw new ArgumentNullException(nameof(polyline));

            var poly = new List<Coordinate>();
            int index = 0, lat = 0, lng = 0, elev = 0;
            try
            {
                while (index < polyline.Points.Length)
                {
                    lat += DecodeNext(polyline.Points, ref index);
                    lng += DecodeNext(polyline.Points, ref index);

                    double? elevation = null;
                    if (polyline.HasElevation)
                    {
                        elev += DecodeNext(polyline.Points, ref index);
                        elevation = elev / polyline.ElevationMultiplier;
                    }

                    poly.Add(new Coordinate(lat / polyline.Multiplier, lng / polyline.Multiplier, elevation));
                }

                return poly;
            }
            catch (Exception ex)
            {
                throw new InvalidPolylineException("Unable to decode polyline. Polyline was not valid.", ex);
            }
        }

        private static int DecodeNext(string encoded, ref int index)
        {
            int result = 0, shift = 0, b;
            do
            {
                b = encoded[index++] - 63;
                result |= (b & 0x1f) << shift;
                shift += 5;
            } while (b >= 0x20);

            return (result & 1) != 0 ? ~(result >> 1) : result >> 1;
        }
    }
}
