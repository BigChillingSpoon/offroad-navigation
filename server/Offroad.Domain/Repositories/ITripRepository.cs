using Routing.Domain.Models;

namespace Routing.Domain.Repositories
{
    public interface ITripRepository
    {
        Task<Trip?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<Trip>> GetAllAsync(CancellationToken ct = default);
        Task AddAsync(Trip trip, CancellationToken ct = default);
        Task UpdateAsync(Trip trip, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
