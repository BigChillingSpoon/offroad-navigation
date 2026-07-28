
using Routing.Domain.Common;
using Routing.Domain.ValueObjects;

namespace Routing.Domain.Entities;

/// <summary>
/// Represents a routable segment connecting two nodes. This is an Entity in DDD terms.
/// </summary>
public class Edge : BaseEntity
{
    /// <summary>
    /// The ID of the node where the edge starts.
    /// </summary>
    public long SourceNodeId { get; private set; }

    /// <summary>
    /// The ID of the node where the edge ends.
    /// </summary>
    public long TargetNodeId { get; private set; }

    /// <summary>
    /// The length of the edge in meters.
    /// </summary>
    public double LengthMeters { get; private set; }

    /// <summary>
    /// Indicates whether any raw OSM way merged into this edge carried a barrier (the "infection rule").
    /// </summary>
    public bool HasBarrier { get; private set; }

    /// <summary>
    /// Indicates whether any raw OSM way merged into this edge carried a no-entry restriction (the "infection rule").
    /// </summary>
    public bool HasNoEntry { get; private set; }

    /// <summary>
    /// Indicates whether this edge lies directly inside a National Park or strict reserve.
    /// </summary>
    public bool IsRestricted { get; private set; }

    /// <summary>
    /// The elevation gain in meters when traversing from Source to Target.
    /// </summary>
    public double ElevationGainMeters { get; private set; }

    /// <summary>
    /// Whether this edge counts as offroad terrain (unpaved surface, forestry track grade, etc. -
    /// see build_routing_graph.sh's is_offroad computation for the exact rule).
    /// </summary>
    public bool IsOffroad { get; private set; }

    /// <summary>
    /// The edge's full geometry, ordered from Source to Target (matching how gis.edges.geom is stored -
    /// pgr_createTopology and the merge step in build_routing_graph.sh both guarantee ST_StartPoint
    /// corresponds to SourceNodeId and ST_EndPoint to TargetNodeId). Used to derive the real
    /// departure/arrival bearing at a junction instead of a straight chord between the two endpoint
    /// nodes, which is wrong for a long or curved multi-vertex edge. Empty if not supplied (e.g. tests
    /// constructing a synthetic straight edge), in which case callers fall back to the node-to-node chord.
    /// </summary>
    public IReadOnlyList<Coordinate> Geometry { get; private set; } = Array.Empty<Coordinate>();

    // Private constructor for EF Core
    private Edge() { }

    public Edge(
        long id,
        long sourceNodeId,
        long targetNodeId,
        double lengthMeters,
        bool hasBarrier,
        bool hasNoEntry,
        bool isRestricted,
        double elevationGainMeters,
        bool isOffroad,
        IReadOnlyList<Coordinate>? geometry = null)
    {
        Id = id;
        SourceNodeId = sourceNodeId;
        TargetNodeId = targetNodeId;
        LengthMeters = lengthMeters;
        HasBarrier = hasBarrier;
        HasNoEntry = hasNoEntry;
        IsRestricted = isRestricted;
        ElevationGainMeters = elevationGainMeters;
        IsOffroad = isOffroad;
        Geometry = geometry ?? Array.Empty<Coordinate>();
    }
}
