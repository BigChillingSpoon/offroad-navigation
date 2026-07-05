using Routing.Domain.Models;

namespace Routing.Application.Planning.Skeletons.Models;

/// <summary>
/// A lightweight, memory-efficient struct representing a straight-line connection between two hookpoints in a skeleton.
/// </summary>
public readonly struct SkeletonStep
{
    public Hookpoint DestinationNode { get; }
    public double EuclideanDistanceFromPreviousStep { get; }
    public double HeadingAngleFromPreviousStep { get; } 

    public SkeletonStep(Hookpoint node, double euclideanDistance, double headingAngle)
    {
        DestinationNode = node;
        EuclideanDistanceFromPreviousStep = euclideanDistance;
        HeadingAngleFromPreviousStep = headingAngle;
    }
}
