using Routing.Domain.Models;

namespace Routing.Application.Planning.Skeletons.Models;

/// <summary>
/// A lightweight, memory-efficient struct representing a straight-line connection between two hookpoints in a skeleton.
/// It is a readonly struct to minimize Garbage Collection (GC) allocations during the DFS traversal.
/// </summary>
public readonly struct SkeletonVector
{
    public Hookpoint From { get; }
    public Hookpoint To { get; }
    public double EuclideanDistance { get; }
    public double HeadingAngle { get; }

    public SkeletonVector(Hookpoint from, Hookpoint to, double euclideanDistance, double headingAngle)
    {
        From = from;
        To = to;
        EuclideanDistance = euclideanDistance;
        HeadingAngle = headingAngle;
    }
}
