using NetTopologySuite.Geometries;

namespace Routing.Infrastructure.Persistence.Entities;

/// <summary>
/// EF mapping for a row in gis.nodes - a strict intersection (degree >= 3, or a degree-2 grade/surface
/// transition) in the pre-processed offroad topology graph.
/// </summary>
public sealed class NodeEntity
{
    public long Id { get; set; }
    public Point Geom { get; set; } = default!;
    public bool IsEntryPoint { get; set; }
    public double? Altitude { get; set; }
}
