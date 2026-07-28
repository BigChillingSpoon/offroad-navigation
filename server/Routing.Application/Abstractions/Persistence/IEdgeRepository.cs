
using Routing.Domain.Entities;

namespace Routing.Application.Abstractions.Persistence;

public interface IEdgeRepository
{
    Task<IReadOnlyList<Edge>> GetEdgesByIdsAsync(IReadOnlyList<long> edgeIds);
    Task<IReadOnlyList<Edge>> GetAllEdgesInAreaAsync();

    /// <summary>
    /// Fetches all edges whose Source and Target nodes are both within <paramref name="nodeIds"/>,
    /// i.e. the edges fully contained within an already-fetched arena of nodes.
    /// </summary>
    Task<IReadOnlyList<Edge>> GetEdgesConnectingAsync(IReadOnlyList<long> nodeIds, CancellationToken ct);
}
