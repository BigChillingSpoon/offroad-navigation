namespace Routing.Application.Planning.Skeletons.Models;

/// <summary>
/// User-preference pruning rules applied while traversing the graph, mirroring the "infection" flags on edges.
/// </summary>
/// <param name="AllowGates">If false, edges with HasBarrier are excluded from the search.</param>
/// <param name="AllowPrivateRoads">
/// The single "avoid private/restricted" switch. If false it excludes both private/forestry edges
/// (HasNoEntry) and national-park tracks (IsRestricted &amp;&amp; IsTrack). There is no
/// separate restricted-zones toggle.
/// </param>
/// <param name="MaxGrade">
/// Highest OSM tracktype grade (1-5) the user's vehicle can take; edges with a higher grade are
/// excluded. Defaults to 5 (no-op) until the vehicle profile is wired - a normal car would pass
/// e.g. 3 so grade-4/5 tracks are dropped. Grade 0 (unknown/non-track) is never excluded.
/// </param>
public readonly record struct SkeletonSearchOptions(
    bool AllowGates,
    bool AllowPrivateRoads,
    byte MaxGrade = 5);
