using Offroad.Core;
using Routing.Application.Contracts.Models;
using Routing.Application.Contracts.Responses;
using Routing.Application.Mappings;
using Routing.Application.Planning.Finders;
using Routing.Domain.Enums;
using Routing.Domain.Models;
using Routing.Domain.Repositories;

namespace Routing.Application.Loops.Commands
{
    internal sealed class LoopsCommands : ILoopsCommands
    {
        private readonly ITripRepository _repository;
        private readonly ILoopFinder _loopFinder;

        public LoopsCommands(ITripRepository repository, ILoopFinder loopFinder)
        {
            _repository = repository;
            _loopFinder = loopFinder;
        }

        public async Task<Result<Guid>> SaveAsyncCommand(SaveLoopRequest request, CancellationToken ct)
        {
            var loopPlan = TripPlan.Create(request.TotalDistanceMeters, request.OffroadDistanceMeters, request.Duration.TotalSeconds);
            var loop = Trip.Create(request.Name, TripType.Loop, loopPlan);

            await _repository.AddAsync(loop, ct);

            return loop.Id;
        }

        public async Task<Result<bool>> DeleteAsyncCommand(Guid id, CancellationToken ct)
        {
            var route = await _repository.GetByIdAsync(id, ct);

            if (route is null)
                return Error.NotFound("Loop", id);

            await _repository.DeleteAsync(id, ct);

            return true;
        }

        public async Task<Result<IReadOnlyList<TripResult>>> FindAsyncCommand(FindLoopsRequest request, CancellationToken ct)
        {
            var intent = request.ToLoopIntent();
            var profile = request.ToUserProfile();

            var loops = await _loopFinder.FindLoopsAsync(intent, profile, ct);

            // Map domain loops to application-level results
            var tripResults = loops.Select(RoutingResultMappings.ToTripResult).ToList();

            return Result<IReadOnlyList<TripResult>>.Success(tripResults);
        }
    }
}
