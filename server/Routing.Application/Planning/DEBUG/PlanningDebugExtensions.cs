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
        internal static void LogToGPX(this IReadOnlyList<Coordinate> geometry, string filePath, string? name = null, string? description = null)
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
            // <metadata> and the track <name>/<desc> let QGIS (and any GPX viewer) show which loop this
            // is - distance, offroad %, whether it survives the goal - straight in the layer list.
            if (!string.IsNullOrEmpty(name))
                writer.WriteLine($"  <metadata><name>{Escape(name)}</name><desc>{Escape(description)}</desc></metadata>");

            writer.WriteLine("  <trk>");
            if (!string.IsNullOrEmpty(name))
                writer.WriteLine($"    <name>{Escape(name)}</name>");
            if (!string.IsNullOrEmpty(description))
                writer.WriteLine($"    <desc>{Escape(description)}</desc>");
            writer.WriteLine("    <trkseg>");

            foreach (var coord in geometry)
            {
                var lat = coord.Latitude.ToString(CultureInfo.InvariantCulture);
                var lon = coord.Longitude.ToString(CultureInfo.InvariantCulture);

                writer.WriteLine($"      <trkpt lat=\"{lat}\" lon=\"{lon}\"></trkpt>");
            }

            writer.WriteLine("    </trkseg>");
            writer.WriteLine("  </trk>");
            writer.WriteLine("</gpx>");
        }

        private static string Escape(string? s) =>
            (s ?? string.Empty).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }
}

