using Offroad.Core;
using Routing.Application.Contracts.Models;
using Routing.Application.Mappings;
using Routing.Domain.Models;
using Routing.Domain.Repositories;

namespace Routing.Application.Loops.Commands
{
    internal sealed class LoopsCommands
    {
        private readonly IRouteRepository _repository;
        // todo: private readonly ILoopFinder _loopFinder;

        public LoopsCommands(IRouteRepository repository)
        {
            _repository = repository;
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
            var start = request.ToStartCoordinate();
            var preferences = request.ToPreferences();

            // todo: volání ILoopFinder
            // var loops = await _loopFinder.FindAsync(start, preferences, ct);

            // Placeholder - vrátí mock data
            return new List<RouteInfo>
            {
                new RouteInfo(Guid.NewGuid(), "Loop Option 1", true, 0, 0, TimeSpan.Zero),
                new RouteInfo(Guid.NewGuid(), "Loop Option 2", true, 0, 0, TimeSpan.Zero)
            };
        }
    }
}
