using Routing.Application.Contracts.Models;
using Offroad.Core;
using Routing.Application.Contracts.Responses;

namespace Routing.Application.Contracts
{
    public interface ILoopsModule
    {
        Task<Result<TripResult>> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Result<IReadOnlyList<TripResult>>> GetAllAsync(CancellationToken ct = default);
        Task<Result<Guid>> SaveAsync(SaveLoopRequest request, CancellationToken ct = default);
        Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
        Task<Result<IReadOnlyList<TripResult>>> FindAsync(FindLoopsRequest request, CancellationToken ct = default);
    }
}