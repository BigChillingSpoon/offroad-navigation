using Offroad.Core;
using Routing.Application.Contracts.Models;
using Routing.Application.Mappings;
using Routing.Domain.Enums;
using Routing.Domain.Repositories;

namespace Routing.Application.Routes.Queries
{
    internal sealed class RoutesQueries : IRouteQueries
    {
        private readonly ITripRepository _repository;

        public RoutesQueries(ITripRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<TripResult>> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var route = await _repository.GetByIdAsync(id, ct);

            if (route is null)
                return Error.NotFound("Route", id);

            return RoutingResultMappings.ToTripResult(route);
        }

        public async Task<Result<IReadOnlyList<TripResult>>> GetAllAsync(CancellationToken ct)
        {
            var routes = await _repository.GetAllAsync(ct);

            var result = routes
                .Where(r => r.Type == TripType.Route)
                .Select(r => RoutingResultMappings.ToTripResult(r))
                .ToList();

            return result;
        }
    }
}
