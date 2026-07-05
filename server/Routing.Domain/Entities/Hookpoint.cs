
using Offroad.Routing.Domain.Common;

namespace Offroad.Routing.Domain.Entities;

/// <summary>
/// Represents a hookpoint (intersection) in the routing graph. This is an Entity in DDD terms.
/// </summary>
public class Hookpoint : BaseEntity
{
    /// <summary>
    /// The geographic or projected X coordinate.
    /// </summary>
    public double X { get; private set; }

    /// <summary>
    /// The geographic or projected Y coordinate.
    /// </summary>
    public double Y { get; private set; }

    /// <summary>
    /// Indicates if this hookpoint is a potential entry point for generating routes.
    /// </summary>
    public bool IsEntryPoint { get; private set; }

    /// <summary>
    /// Indicates if this hookpoint is a dead end.
    /// </summary>
    public bool IsDeadEnd { get; private set; }

    private readonly List<RoadLink> _roadLinks = new();

    /// <summary>
    /// A collection of links to connected roads.
    /// </summary>
    public IReadOnlyList<RoadLink> RoadLinks => _roadLinks.AsReadOnly();

    // Private constructor for EF Core
    private Hookpoint() { }

    public Hookpoint(int id, double x, double y, bool isEntryPoint, bool isDeadEnd)
    {
        Id = id;
        X = x;
        Y = y;
        IsEntryPoint = isEntryPoint;
        IsDeadEnd = isDeadEnd;
    }

    public void AddRoadLink(int roadId, int connectedHookpointId)
    {
        if (_roadLinks.Any(rl => rl.RoadId == roadId))
        {
            // or throw an exception, depending on desired behavior
            return;
        }
        _roadLinks.Add(new RoadLink(roadId, connectedHookpointId));
    }
}
