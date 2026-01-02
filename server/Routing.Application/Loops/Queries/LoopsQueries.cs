using Offroad.Core;
using Routing.Application.Contracts.Models;
using Routing.Domain.Repositories;

namespace Routing.Application.Loops.Queries
{
    internal sealed class LoopsQueries
    {
        private readonly ITripRepository _repository;

        public LoopsQueries(ITripRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<TripInfo>> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var route = await _repository.GetByIdAsync(id, ct);

            if (route is null)
                return Error.NotFound("Loop", id);

            return new TripInfo(
                route.Id,
                route.Name,
                route.IsLoop,
                0,
                0,
                TimeSpan.Zero
            );
        }

        public async Task<Result<IReadOnlyList<TripInfo>>> GetAllAsync(CancellationToken ct)
        {
            var routes = await _repository.GetAllAsync(ct);

            var result = routes
                .Where(r => r.IsLoop)
                .Select(r => new TripInfo(
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
