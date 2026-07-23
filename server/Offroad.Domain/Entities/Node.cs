
using Routing.Domain.Common;
using Routing.Domain.ValueObjects;

namespace Routing.Domain.Entities;

/// <summary>
/// Represents a node (strict intersection) in the routing graph. This is an Entity in DDD terms.
/// </summary>
public class Node : BaseEntity
{
    /// <summary>
    /// The geographic location of this node.
    /// </summary>
    public Coordinate Coordinate { get; private set; }

    /// <summary>
    /// Indicates if this node is a potential entry point for generating routes (a paved road entering the forest).
    /// </summary>
    public bool IsEntryPoint { get; private set; }

    private readonly List<EdgeLink> _edgeLinks = new();

    /// <summary>
    /// A collection of links to connected edges.
    /// </summary>
    public IReadOnlyList<EdgeLink> EdgeLinks => _edgeLinks.AsReadOnly();

    // Private constructor for EF Core
    private Node() { }

    public Node(long id, Coordinate coordinate, bool isEntryPoint)
    {
        Id = id;
        Coordinate = coordinate;
        IsEntryPoint = isEntryPoint;
    }

    public void AddEdgeLink(long edgeId, long connectedNodeId)
    {
        if (_edgeLinks.Any(el => el.EdgeId == edgeId))
        {
            // or throw an exception, depending on desired behavior
            return;
        }
        _edgeLinks.Add(new EdgeLink(edgeId, connectedNodeId));
    }
}
