namespace Routing.Application.Planning.Skeletons.Models;

/// <summary>
/// User-preference pruning rules applied while traversing the graph, mirroring the "infection" flags on edges.
/// </summary>
/// <param name="AllowGates">If false, edges with HasBarrier are excluded from the search.</param>
/// <param name="AllowPrivateRoads">If false, edges with HasNoEntry are excluded from the search.</param>
/// <param name="AllowRestrictedZones">If false, edges with IsRestricted are excluded from the search.</param>
public readonly record struct SkeletonSearchOptions(
    bool AllowGates,
    bool AllowPrivateRoads,
    bool AllowRestrictedZones);
