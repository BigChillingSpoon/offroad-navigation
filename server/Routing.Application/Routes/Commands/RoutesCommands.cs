using Offroad.Core;
using Routing.Application.Contracts.Models;
using Routing.Application.Mappings;
using Routing.Domain.Models;
using Routing.Domain.Repositories;

namespace Routing.Application.Routes.Commands
{
    internal sealed class RoutesCommands
    {
        private readonly IRouteRepository _repository;
        // todo: private readonly IRoutePlanner _planner;

        public RoutesCommands(IRouteRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<Guid>> SaveAsync(SaveRouteRequest request, CancellationToken ct)
        {
            var route = Route.Create(request.Name, isLoop: false);

            await _repository.AddAsync(route, ct);

            return route.Id;
        }

        public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct)
        {
            var route = await _repository.GetByIdAsync(id, ct);

            if (route is null)
                return Error.NotFound("Route", id);

            await _repository.DeleteAsync(id, ct);

            return true;
        }

        public async Task<Result<RouteInfo>> PlanAsync(PlanRouteRequest request, CancellationToken ct)
        {
            var start = request.ToStartCoordinate();
            var end = request.ToEndCoordinate();
            var preferences = request.ToPreferences();

            // todo: call IRoutePlanner
            // var plannedRoute = await _planner.PlanAsync(start, end, preferences, ct);

            // placeholder
            return new RouteInfo(
                Guid.NewGuid(),
                "Planned Route",
                false,
                0,
                0,
                TimeSpan.Zero
            );
        }
    }
}
