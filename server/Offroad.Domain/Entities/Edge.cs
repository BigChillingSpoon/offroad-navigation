
using Routing.Domain.Common;
using Routing.Domain.ValueObjects;

namespace Routing.Domain.Entities;

/// <summary>
/// Represents a routable segment connecting two nodes. This is an Entity in DDD terms.
/// </summary>
public class Edge : BaseEntity
{
    /// <summary>OSM highway class that marks a forestry/field track (the routing provider's TRACK road_class).</summary>
    private const string TrackHighwayClass = "track";

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
    /// This edge's length when it is offroad, else 0 - the offroad contribution to a path's total.
    /// </summary>
    public double OffroadLengthMeters => IsOffroad ? LengthMeters : 0;

    /// <summary>
    /// OSM tracktype difficulty, 1 (smooth/solid) to 5 (barely passable); 0 when unknown/not a graded
    /// track. Used by the skeleton search as a hard vehicle limit (a normal car cannot take grade-5)
    /// and as a soft terrain preference. Parsed from gis.edges.tracktype ('grade1'..'grade5').
    /// </summary>
    public byte Grade { get; private set; }

    /// <summary>
    /// OSM highway class ('track', 'unclassified', ...) - the routing provider's road_class. Used to
    /// match its national-park and car-access rules, which key off the track class. Empty when unknown.
    /// </summary>
    public string Highway { get; private set; } = string.Empty;

    /// <summary>
    /// True when this edge is an OSM highway=track. Deliberately distinct from <see cref="IsOffroad"/>
    /// (a broader surface/grade flag): the routing provider's national-park and car-access rules forbid
    /// specifically the track road_class, so the skeleton graph must match on the class, not on terrain.
    /// </summary>
    public bool IsTrack => string.Equals(Highway, TrackHighwayClass, StringComparison.OrdinalIgnoreCase);

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
        IReadOnlyList<Coordinate>? geometry = null,
        byte grade = 0,
        string? highway = null)
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
        Grade = grade;
        Highway = highway ?? string.Empty;
    }
}
