using Offroad.Core;
using Routing.Application.Contracts.Models;
using Routing.Application.Mappings;
using Routing.Application.Planning.Goals;
using Routing.Application.Planning.Planner;
using Routing.Domain.Models;
using Routing.Domain.Repositories;

namespace Routing.Application.Routes.Commands
{
    internal sealed class RoutesCommands
    {
        private readonly IRouteRepository _repository;
        private readonly IRoutePlanner _planner;

        public RoutesCommands(IRouteRepository repository, IRoutePlanner planner)
        {
            _repository = repository;
            _planner = planner;
        }

        public async Task<Result<Guid>> SaveAsyncCommand(SaveRouteRequest request, CancellationToken ct)
        {
            var route = Route.Create(request.Name, isLoop: false);

            await _repository.AddAsync(route, ct);

            return route.Id;
        }

        public async Task<Result<bool>> DeleteAsyncCommand(Guid id, CancellationToken ct)
        {
            var route = await _repository.GetByIdAsync(id, ct);

            if (route is null)
                return Error.NotFound("Route", id);

            await _repository.DeleteAsync(id, ct);

            return true;
        }

        public async Task<Result<RouteInfo>> PlanAsyncCommand(PlanRouteRequest request, CancellationToken ct)
        {
            var intent = request.ToRouteIntent();
            var profile = request.ToUserProfile();
            var goal = new RouteGoal();
            var config = new PlannerConfig();

            // todo: get actual result
            await _planner.PlanAsync(intent, goal, profile, config, ct);

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
