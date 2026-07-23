using NetTopologySuite.Geometries;

namespace Routing.Infrastructure.Persistence.Entities;

/// <summary>
/// EF mapping for a row in gis.geometric_zones - natural-area and restricted-area polygons
/// produced by the OSM import pipeline (see import_rules.lua). ZoneType follows that script's
/// numbering: 1 = Nature (forest/wood/scrub/meadow), 2 = Restricted (national park/nature
/// reserve/protected area).
/// </summary>
public sealed class GeometricZoneEntity
{
    public int Id { get; set; }
    public int ZoneType { get; set; }
    public MultiPolygon Geometry { get; set; } = default!;
    public double? AreaSqm { get; set; }
}
