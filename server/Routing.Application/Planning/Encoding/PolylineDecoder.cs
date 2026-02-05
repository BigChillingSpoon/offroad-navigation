using Routing.Application.Planning.Exceptions;
using Routing.Domain.ValueObjects;

namespace Routing.Application.Planning.Encoding
{
    public static class PolylineDecoder
    {
        // Standard Google Polyline algorithm, GraphHopper compatible
        public static IReadOnlyList<Coordinate> Decode(string encoded)
        {
            var poly = new List<Coordinate>();
            int index = 0, lat = 0, lng = 0;
            try
            {
                while (index < encoded.Length)
                {
                    lat += DecodeNext(encoded, ref index);
                    lng += DecodeNext(encoded, ref index);

                    poly.Add(new Coordinate(lat / 1e5, lng / 1e5));
                }

                return poly;
            }
            catch(Exception ex)
            {
                //todo log
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
