using Routing.Domain.Models;

namespace Routing.Domain.Repositories
{
    public interface ITripRepository
    {
        Task<Route?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<Route>> GetAllAsync(CancellationToken ct = default);
        Task AddAsync(Route route, CancellationToken ct = default);
        Task UpdateAsync(Route route, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
