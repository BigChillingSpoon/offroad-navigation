using Routing.Application.Contracts.Models;
using Offroad.Core;

namespace Routing.Application.Contracts;

public interface IRoutingModule
{
    Task<Result<RouteInfo>> GetRouteAsync(Guid id, CancellationToken ct = default);
}
