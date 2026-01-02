using Offroad.Core;
using Routing.Application.Contracts.Models;
using Routing.Domain.Repositories;
using Routing.Domain.Enums;
using Routing.Application.Mappings;
namespace Routing.Application.Loops.Queries
{
    internal sealed class LoopsQueries
    {
        private readonly ITripRepository _repository;

        public LoopsQueries(ITripRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<TripResult>> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var route = await _repository.GetByIdAsync(id, ct);

            if (route is null)
                return Error.NotFound("Loop", id);

            return RoutingResultMappings.ToTripResult(route);
        }

        public async Task<Result<IReadOnlyList<TripResult>>> GetAllAsync(CancellationToken ct)
        {
            var trips = await _repository.GetAllAsync(ct);

            var result = trips
                .Where(t => t.Type == TripType.Loop)
                .Select(t => RoutingResultMappings.ToTripResult(t))
                .ToList();

            return result;
        }
    }
}
