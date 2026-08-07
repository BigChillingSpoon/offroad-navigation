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
/// radius of this entrance. Higher means more terrain available to build the requested loop from.
/// </param>
public readonly record struct ArenaEntrance(Coordinate Coordinate, double OffroadLengthMeters);
