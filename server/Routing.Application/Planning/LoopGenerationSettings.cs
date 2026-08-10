namespace Routing.Application.Planning;

/// <summary>
/// Tuning knobs for turning selected arenas into routed loop candidates
/// (<see cref="Candidates.Generators.LoopCandidateGenerator"/>) and for accepting them
/// (<see cref="Goals.LoopGoal"/>). Grouped here so they can be tuned in one place.
/// </summary>
public static class LoopGenerationSettings
{
    /// <summary>
    /// Accepted deviation band around the requested length, as a fraction of it, enforced on the real
    /// routed distance. The skeleton search aims a bit tighter than this (see
    /// <see cref="Skeletons.SkeletonSearchSettings.MaxTargetFraction"/>), since the routed distance runs
    /// slightly longer than the skeleton's own.
    /// </summary>
    public const double GoalMinFraction = 0.85;
    public const double GoalMaxFraction = 1.15;

    /// <summary>
    /// How close to the requested entrance the (well-connected) start node must be. Keeps the loop
    /// anchored near the chosen arena while allowing a better-connected junction than the nearest node.
    /// </summary>
    public const double StartNodeSearchRadiusMeters = 300.0;

    /// <summary>
    /// Cap on the number of (nearest) nodes fetched per arena. A round loop stays near its start, so for
    /// big loops the fetch radius would otherwise pull thousands of far nodes that only slow the search.
    /// </summary>
    public const int ArenaMaxNodes = 700;

    /// <summary>
    /// The routing provider follows the points it is given, in order. We pass the skeleton's full
    /// traversed geometry downsampled to a point every ~this many metres (capped at
    /// <see cref="MaxRoutePoints"/>) so the provider is pinned to the roads the search chose.
    /// </summary>
    public const double RoutePointSpacingMeters = 60.0;
    public const int MaxRoutePoints = 120;
}
