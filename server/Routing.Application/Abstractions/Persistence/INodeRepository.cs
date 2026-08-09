using Routing.Domain.Entities;
using Routing.Domain.ValueObjects;

namespace Routing.Application.Abstractions.Persistence;

public interface INodeRepository
{
    Task<IReadOnlyList<Node>> GetNodesNearAsync(
        Coordinate center,
        double radiusMeters,
        int maxNodes,
        CancellationToken ct);

    /// <summary>
    /// Ranks offroad "arena" entrances near the user by how much offroad terrain surrounds them, using
    /// the pre-aggregated <c>gis.heatmap</c> density grid so a single cheap query replaces scanning the
    /// raw edge geometry. Candidates are <c>is_entry_point</c> nodes within <paramref name="driveRadiusMeters"/>
    /// of <paramref name="center"/>; each is scored by the summed offroad length of grid cells within
    /// <paramref name="loopReachMeters"/>. The user's own start (<paramref name="center"/>) is always
    /// included as a candidate so the caller can detect the "user already inside offroad" case. Only
    /// entrances whose score reaches <paramref name="minOffroadLengthMeters"/> are returned, ordered by
    /// descending score and capped at <paramref name="limit"/>.
    /// </summary>
    Task<IReadOnlyList<ArenaEntrance>> GetCandidateArenaEntrancesAsync(
        Coordinate center,
        double driveRadiusMeters,
        double loopReachMeters,
        double minOffroadLengthMeters,
        double rankingLengthBlend,
        int limit,
        CancellationToken ct);
}
