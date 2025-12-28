using Routing.Application.Contracts.Models;
using Offroad.Core;

namespace Routing.Application.Contracts
{
    public interface IRoutesModule
    {
        Task<Result<RouteInfo>> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Result<IReadOnlyList<RouteInfo>>> GetAllAsync(CancellationToken ct = default);
        Task<Result<Guid>> SaveAsync(SaveRouteRequest request, CancellationToken ct = default);
        Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
        Task<Result<RouteInfo>> PlanAsync(PlanRouteRequest request, CancellationToken ct = default);
    }
}


