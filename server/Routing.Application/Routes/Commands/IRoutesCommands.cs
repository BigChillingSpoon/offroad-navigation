using Offroad.Core;
using Routing.Application.Contracts.Models;
using Routing.Application.Contracts.Responses;

namespace Routing.Application.Routes.Commands
{
    public interface IRoutesCommands
    {
        Task<Result<TripResult>> PlanAsync(PlanRouteRequest request, CancellationToken ct);
        Task<Result<Guid>> SaveAsync(SaveRouteRequest request, CancellationToken ct);
        Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct);
    }
}