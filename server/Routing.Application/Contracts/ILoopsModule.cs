using Routing.Application.Contracts.Models;
using Offroad.Core;

namespace Routing.Application.Contracts
{
    public interface ILoopsModule
    {
        Task<Result<TripInfo>> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Result<IReadOnlyList<TripInfo>>> GetAllAsync(CancellationToken ct = default);
        Task<Result<Guid>> SaveAsync(SaveLoopRequest request, CancellationToken ct = default);
        Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
        Task<Result<IReadOnlyList<TripInfo>>> FindAsync(FindLoopsRequest request, CancellationToken ct = default);
    }
}