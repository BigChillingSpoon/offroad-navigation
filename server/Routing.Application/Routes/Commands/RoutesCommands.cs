using Offroad.Core;
using Routing.Application.Contracts.Models;
using Routing.Application.Mappings;
using Routing.Application.Planning.Goals;
using Routing.Application.Planning.Planner;
using Routing.Domain.Enums;
using Routing.Domain.Models;
using Routing.Domain.Repositories;

namespace Routing.Application.Routes.Commands
{
    internal sealed class RoutesCommands
    {
        private readonly ITripRepository _repository;
        private readonly ITripPlanner _planner;

        public RoutesCommands(ITripRepository repository, ITripPlanner planner)
        {
            _repository = repository;
            _planner = planner;
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
            var goal = new RouteGoal();
            var settings = new PlannerSettings();

            var plan = await _planner.PlanAsync(intent, goal, profile, settings, ct);
            var route = Trip.Create("Test route", TripType.Route, plan);
            
            return RoutingResultMappings.ToTripResult(route);
        }
    }
}
