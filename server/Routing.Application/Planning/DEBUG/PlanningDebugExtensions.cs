using Routing.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.DEBUG
{
    internal static class PlanningDebugExtensions
    {
        [Conditional("DEBUG")]
        internal static void LogToGPX(this IReadOnlyList<Coordinate> geometry, string filePath)
        {
            if (geometry is null || geometry.Count == 0)
                return;

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var writer = new StreamWriter(filePath);

            writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            writer.WriteLine("<gpx version=\"1.1\" creator=\"MyOffroadApp\">");
            writer.WriteLine("  <trk><trkseg>");

            foreach (var coord in geometry)
            {
                var lat = coord.Latitude.ToString(CultureInfo.InvariantCulture);
                var lon = coord.Longitude.ToString(CultureInfo.InvariantCulture);

                writer.WriteLine($"    <trkpt lat=\"{lat}\" lon=\"{lon}\"></trkpt>");
            }

            writer.WriteLine("  </trkseg></trk>");
            writer.WriteLine("</gpx>");
        }
    }
}

