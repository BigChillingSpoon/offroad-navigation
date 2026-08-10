using System;
using System.Collections.Generic;

namespace Routing.Application.Planning.Skeletons;

/// <summary>
/// All tuning knobs for the loop skeleton search (<see cref="Services.LoopSkeletonFinder"/> and its
/// per-call search). Grouped here so the algorithm can be tuned from one place instead of hunting for
/// constants scattered through the code.
/// </summary>
public static class SkeletonSearchSettings
{
    /// <summary>A loop that doubles back more sharply than this at a junction is not an acceptable loop.</summary>
    public const double MaxTurnAngleDegrees = 120.0;

    /// <summary>Don't attempt to close the loop before this fraction of the target is travelled (avoids tiny loops).</summary>
    public const double MinClosureFraction = 0.5;

    /// <summary>
    /// Skeleton distance band as a fraction of the target. Deliberately tighter than the pipeline goal
    /// band: the real routed distance runs a little longer than the skeleton's own, so aiming short keeps
    /// the routed loop inside the goal band and cuts discards.
    /// </summary>
    public const double MinTargetFraction = 0.85;
    public const double MaxTargetFraction = 1.08;

    // Score weights, mirroring the shape of LoopCandidateScorer (offroad ratio dominates; elevation and
    // distance-deviation are small penalties). Only used to rank skeletons; the pipeline re-scores later.
    public const double OffroadScoreWeight = 100.0;
    public const double ElevationPenaltyPerMeter = 0.01;
    public const double DistanceDeviationPenaltyWeight = 50.0;

    /// <summary>
    /// Reward for a round loop over a thin "out-and-back" one, via the isoperimetric quotient
    /// (1 = circle, ~0 = sliver). Offroad ratio still dominates; this breaks ties toward the rounder shape.
    /// </summary>
    public const double RoundnessScoreWeight = 40.0;

    // Neighbour-ordering weights (radial "out then back" shape + offroad preference). Ordering only
    // affects how fast a good loop is found; it never changes which loops are valid.
    public const double OffroadOrderingBonus = 1.0;
    public const double RadialOrderingWeight = 1.0;

    // Anti-hang budget: on a pathological arena the search stops and returns the best loop found so far.
    public const long MaxNodeExpansions = 3_000_000;
    public static readonly TimeSpan TimeBudget = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Adaptive beam: expand only the top-K neighbours per node, K = clamp(round(BeamMaxWidth *
    /// BeamScaleNodes / (nodeCount + BeamScaleNodes)), BeamMinWidth, BeamMaxWidth). Small arenas keep K
    /// high (near-exhaustive/optimal); large dense arenas drop to a narrow, fast guided beam.
    /// </summary>
    public const int BeamMaxWidth = 6;
    public const int BeamMinWidth = 2;
    public const double BeamScaleNodes = 150.0;

    /// <summary>Keep up to this many spatially-distinct (non-overlapping) loops per arena.</summary>
    public const int MaxLoopsPerArena = 4;

    /// <summary>Two loops overlap - and only the higher-scored is kept - when they share more than this
    /// fraction of the smaller loop's interior nodes.</summary>
    public const double LoopOverlapFraction = 0.5;

    /// <summary>
    /// Once any loop is found, branches whose optimistic score can't come within this margin of the best
    /// are pruned (and kept loops that fall this far below the best are dropped). Bounds deep searches.
    /// The score scale is ~0-140, so this keeps genuinely different loops while cutting clearly-worse ones.
    /// </summary>
    public const double DiversityScoreMargin = 30.0;

    /// <summary>
    /// Highway classes our graph contains but the routing provider will NOT route (pedestrian-only,
    /// car-inaccessible, or not-yet-built). Building a loop on one would make the provider refuse the edge
    /// and detour, so they are excluded from the search graph.
    /// </summary>
    public static readonly HashSet<string> NonRoutableHighways = new(StringComparer.OrdinalIgnoreCase)
    {
        "proposed", "construction", "raceway", "platform", "busway", "bus_guideway",
        "via_ferrata", "escape", "abandoned", "no", "corridor",
        "footway", "cycleway", "path", "steps", "bridleway", "pedestrian",
    };
}
