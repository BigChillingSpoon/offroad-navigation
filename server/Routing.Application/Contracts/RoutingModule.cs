using Routing.Application.Contracts.Models;
using Routing.Domain.Repositories;
using Offroad.Core;

namespace Routing.Application.Contracts;

internal sealed class RoutingModule : IRoutingModule
{
    private readonly IRouteRepository _repository;

    public RoutingModule(IRouteRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<RouteInfo>> GetRouteAsync(Guid id, CancellationToken ct = default)
    {
        var route = await _repository.GetByIdAsync(id, ct);

        if (route is null)
            return Error.NotFound("Route", id);

        return new RouteInfo(route.Id, route.Name);
    }
}
