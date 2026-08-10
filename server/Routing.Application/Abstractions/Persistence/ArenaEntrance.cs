using Routing.Domain.ValueObjects;

namespace Routing.Application.Abstractions.Persistence;

/// <summary>
/// A candidate offroad "arena" entry point together with the amount of offroad terrain reachable
/// around it. Produced by <see cref="INodeRepository.GetCandidateArenaEntrancesAsync"/> and consumed
/// by the loop generator to pick spatially-diverse, loop-sustaining entrances.
/// </summary>
/// <param name="Coordinate">The entry-point location (a real graph node, or the user's own start).</param>
/// <param name="OffroadLengthMeters">
/// Total offroad edge length (summed from the <c>gis.heatmap</c> density grid) within the loop reach
/// radius of this entrance. Used as the qualifying threshold (enough terrain to build a loop at all).
/// </param>
/// <param name="ArenaScore">
/// Length-aware ranking key (higher = better arena), blended by loop length: for short loops it is
/// dominated by offroad-edge/intersection density; for long loops by offroad length favouring long
/// edges (fewer nodes, so the skeleton search stays fast). Computed as
/// <c>offroad_len^(2t) * edge_count^(1-2t)</c> in the density grid. Used only to rank/greedy-select.
/// </param>
public readonly record struct ArenaEntrance(Coordinate Coordinate, double OffroadLengthMeters, double ArenaScore);
