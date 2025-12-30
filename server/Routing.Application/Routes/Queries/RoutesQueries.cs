using Offroad.Core;
using Routing.Application.Contracts.Models;
using Routing.Domain.Repositories;

namespace Routing.Application.Routes.Queries
{
    internal sealed class RoutesQueries
    {
        private readonly IRouteRepository _repository;

        public RoutesQueries(IRouteRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<RouteInfo>> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var route = await _repository.GetByIdAsync(id, ct);

            if (route is null)
                return Error.NotFound("Route", id);

            return new RouteInfo(
                route.Id,
                route.Name,
                route.IsLoop,
                0,
                0,
                TimeSpan.Zero
            );
        }

        public async Task<Result<IReadOnlyList<RouteInfo>>> GetAllAsync(CancellationToken ct)
        {
            var routes = await _repository.GetAllAsync(ct);

            var result = routes
                .Where(r => !r.IsLoop)
                .Select(r => new RouteInfo(
                    r.Id,
                    r.Name,
                    r.IsLoop,
                    0,
                    0,
                    TimeSpan.Zero
                ))
                .ToList();

            return result;
        }
    }
}
