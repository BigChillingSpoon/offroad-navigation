using Routing.Domain.Entities;
using Routing.Domain.ValueObjects;

namespace Routing.Application.Abstractions.Persistence;

public interface INodeRepository
{
    Task<IReadOnlyList<Node>> GetNodesNearAsync(
        Coordinate center,
        double radiusMeters,
        CancellationToken ct);
}
