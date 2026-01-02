using Offroad.Core;
using Routing.Application.Contracts.Models;
using Routing.Application.Mappings;
using Routing.Application.Planning.Goals;
using Routing.Application.Planning.Planner;
using Routing.Application.Routes.Queries;
using Routing.Domain.Models;
using Routing.Domain.Repositories;

namespace Routing.Application.Loops.Commands
{
    internal sealed class LoopsCommands
    {
        private readonly IRouteRepository _repository;
        private readonly IRoutePlanner _planner;

        public LoopsCommands(IRouteRepository repository, IRoutePlanner planner)
        {
            _repository = repository;
            _planner = planner;
        }

        public async Task<Result<Guid>> SaveAsync(SaveLoopRequest request, CancellationToken ct)
        {
            var route = Route.Create(request.Name, isLoop: true);

            await _repository.AddAsync(route, ct);

            return route.Id;
        }

        public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct)
        {
            var route = await _repository.GetByIdAsync(id, ct);

            if (route is null)
                return Error.NotFound("Loop", id);

            await _repository.DeleteAsync(id, ct);

            return true;
        }

        public async Task<Result<IReadOnlyList<RouteInfo>>> FindAsync(FindLoopsRequest request, CancellationToken ct)
        {
            var intent = request.ToLoopIntent();
            var profile = request.ToUserProfile();
            var goal = new LoopGoal();
            var config = new PlannerConfig();
            await _planner.PlanAsync(intent, goal, profile, config, ct);
            // Placeholder - vrátí mock data
            return new List<RouteInfo>
            {
                new RouteInfo(Guid.NewGuid(), "Loop Option 1", true, 0, 0, TimeSpan.Zero),
                new RouteInfo(Guid.NewGuid(), "Loop Option 2", true, 0, 0, TimeSpan.Zero)
            };
        }
    }
}
