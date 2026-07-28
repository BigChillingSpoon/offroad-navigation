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

    public SkeletonVector(Node fromNode, Node toNode, double edgeLengthMeters, double headingAngleDegrees)
    {
        FromNode = fromNode;
        ToNode = toNode;
        EdgeLengthMeters = edgeLengthMeters;
        HeadingAngleDegrees = headingAngleDegrees;
    }
}
