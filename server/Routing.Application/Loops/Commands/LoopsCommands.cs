using Offroad.Core;
using Routing.Application.Contracts.Models;
using Routing.Application.Mappings;
using Routing.Application.Planning.Goals;
using Routing.Application.Planning.Planner;
using Routing.Domain.Enums;
using Routing.Domain.Models;
using Routing.Domain.Repositories;

namespace Routing.Application.Loops.Commands
{
    internal sealed class LoopsCommands : ILoopsCommands
    {
        private readonly ITripRepository _repository;
        private readonly ITripPlanner _planner;

        public LoopsCommands(ITripRepository repository, ITripPlanner planner)
        {
            _repository = repository;
            _planner = planner;
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
            var goal = new LoopGoal();
            var settings = new PlannerSettings();
            //todo instead of returning from planner we shall return from loopfinder, that returns multiple options
            var plan = await _planner.PlanAsync(intent, goal, profile, settings, ct);
            var loop = Trip.Create("Test loop", TripType.Loop, plan);
            
            //
            return new List<TripResult>
            {
                RoutingResultMappings.ToTripResult(loop)
            };
        }
    }
}
