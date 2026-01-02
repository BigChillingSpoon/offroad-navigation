using Routing.Application.Contracts.Models;
using Offroad.Core;

namespace Routing.Application.Contracts
{
    public interface IRoutesModule
    {
        Task<Result<TripInfo>> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Result<IReadOnlyList<TripInfo>>> GetAllAsync(CancellationToken ct = default);
        Task<Result<Guid>> SaveAsync(SaveRouteRequest request, CancellationToken ct = default);
        Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
        Task<Result<TripInfo>> PlanAsync(PlanRouteRequest request, CancellationToken ct = default);
    }
}
