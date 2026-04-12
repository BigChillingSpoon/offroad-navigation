using NetTopologySuite.Geometries;

namespace Routing.Domain.Models;

public enum ZoneType
{
    OffroadArea = 1,
    RestrictedArea = 2
}

public class GeoZone
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ZoneType Type { get; set; }

    public Polygon Geometry { get; set; } = null!;
}