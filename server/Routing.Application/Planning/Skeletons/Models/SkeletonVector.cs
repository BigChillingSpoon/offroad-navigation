using Routing.Domain.Entities;

namespace Routing.Application.Planning.Skeletons.Models;

/// <summary>
/// A lightweight, memory-efficient struct representing one traversed edge in a loop skeleton's VectorChain.
/// </summary>
public readonly struct SkeletonVector
{
    public Node FromNode { get; }
    public Node ToNode { get; }
    public double EdgeLengthMeters { get; }
    public double HeadingAngleDegrees { get; }

    /// <summary>The actual edge traversed on this hop - kept so the loop can be handed to the routing
    /// provider as the real roads (its geometry), not just the junction endpoints it would otherwise reroute.</summary>
    public Edge Edge { get; }

    public SkeletonVector(Node fromNode, Node toNode, double edgeLengthMeters, double headingAngleDegrees, Edge edge)
    {
        FromNode = fromNode;
        ToNode = toNode;
        EdgeLengthMeters = edgeLengthMeters;
        HeadingAngleDegrees = headingAngleDegrees;
        Edge = edge;
    }
}
