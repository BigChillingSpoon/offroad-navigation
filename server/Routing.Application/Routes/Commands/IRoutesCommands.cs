using Offroad.Core;
using Routing.Application.Contracts.Models;

namespace Routing.Application.Routes.Commands
{
    public interface IRoutesCommands
    {
        Task<Result<TripResult>> PlanAsyncCommand(PlanRouteRequest request, CancellationToken ct);
        Task<Result<Guid>> SaveAsyncCommand(SaveRouteRequest request, CancellationToken ct);
        Task<Result<bool>> DeleteAsyncCommand(Guid id, CancellationToken ct);
    }
}