using Routing.Domain.Models;
using Routing.Domain.ValueObjects;

namespace Routing.Application.Abstractions.Persistence;

/// <summary>
/// Defines the contract for accessing hookpoint data from the persistence layer.
/// </summary>
public interface IHookpointRepository
{
    /// <summary>
    /// Fetches all hookpoints within a specified radius from a central point.
    /// </summary>
    /// <param name="center">The center coordinate for the spatial query.</param>
    /// <param name="radiusMeters">The radius in meters.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A read-only list of hookpoints found within the area.</returns>
    Task<IReadOnlyList<Hookpoint>> GetHookpointsNearAsync(Coordinate center, double radiusMeters, CancellationToken ct);
}
