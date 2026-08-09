using NetTopologySuite.Geometries;

namespace Routing.Infrastructure.Persistence.Entities;

/// <summary>
/// EF mapping for a row in gis.edges - a routable segment connecting two nodes in the pre-processed
/// offroad topology graph. Dangling (dead-end) edges are recursively pruned at the DB level.
/// </summary>
public sealed class EdgeEntity
{
    public int Id { get; set; }
    public long SourceNodeId { get; set; }
    public long TargetNodeId { get; set; }
    public double LengthMeters { get; set; }

    /// <summary>
    /// Full edge geometry, ST_StartPoint = SourceNodeId, ST_EndPoint = TargetNodeId.
    /// </summary>
    public LineString Geom { get; set; } = default!;

    /// <summary>
    /// True if any raw OSM way merged into this edge carried a barrier (the "infection rule").
    /// </summary>
    public bool HasBarrier { get; set; }

    /// <summary>
    /// True if any raw OSM way merged into this edge carried a no-entry restriction (the "infection rule").
    /// </summary>
    public bool HasNoEntry { get; set; }

    public bool IsRestricted { get; set; }
    public double ElevationGainMeters { get; set; }
    public bool IsOffroad { get; set; }

    /// <summary>
    /// OSM tracktype grade ('grade1'..'grade5') or null. Parsed into <see cref="Routing.Domain.Entities.Edge.Grade"/>.
    /// </summary>
    public string? TrackType { get; set; }

    /// <summary>
    /// OSM highway class ('track', 'unclassified', ...). GraphHopper's road_class; used to apply its
    /// national-park rule (in_cz_parks &amp;&amp; road_class == TRACK) to tracks only.
    /// </summary>
    public string? Highway { get; set; }
}
