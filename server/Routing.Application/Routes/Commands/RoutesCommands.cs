using Offroad.Core;
using Routing.Application.Contracts.Models;
using Routing.Application.Mappings;
using Routing.Application.Planning.Finders;
using Routing.Domain.Enums;
using Routing.Domain.Models;
using Routing.Domain.Repositories;

namespace Routing.Application.Routes.Commands
{
    internal sealed class RoutesCommands : IRoutesCommands
    {
        private readonly ITripRepository _repository;
        private readonly IRouteFinder _routeFinder;

        public RoutesCommands(ITripRepository repository, IRouteFinder routeFinder)
        {
            _repository = repository;
            _routeFinder = routeFinder;
        }

        public async Task<Result<Guid>> SaveAsyncCommand(SaveRouteRequest request, CancellationToken ct)
        {
            var routePlan = TripPlan.Create(request.TotalDistanceMeters, request.OffroadDistanceMeters, request.Duration.TotalSeconds);
            var route = Trip.Create(request.Name, TripType.Loop, routePlan);

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

        public async Task<Result<TripResult>> PlanAsyncCommand(PlanRouteRequest request, CancellationToken ct)
        {
            var intent = request.ToRouteIntent();
            var profile = request.ToUserProfile();

            var route = await _routeFinder.FindRouteAsync(intent, profile, ct);

            return route.ToTripResult();
        }
    }
}
