using FluentValidation;
using Offroad.Core;
using Routing.Application.Contracts.Models;
using Routing.Application.Contracts.Responses;
using Routing.Application.Mappings;
using Routing.Application.Planning.Exceptions;
using Routing.Application.Planning.Finders;
using Routing.Domain.Enums;
using Routing.Domain.Exceptions;
using Routing.Domain.Models;
using Routing.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Routing.Application.Routes.Commands
{
    internal sealed class RoutesCommands : IRoutesCommands
    {
        private readonly ITripRepository _repository;
        private readonly IRouteFinder _routeFinder;
        private readonly IValidator<PlanRouteRequest> _planValidator;
        private readonly ILogger<RoutesCommands> _logger;

        public RoutesCommands(ITripRepository repository, IRouteFinder routeFinder, IValidator<PlanRouteRequest> planValidator, ILogger<RoutesCommands> logger)
        {
            _repository = repository;
            _routeFinder = routeFinder;
            _planValidator = planValidator;
            _logger = logger;
        }

        public async Task<Result<Guid>> SaveAsync(SaveRouteRequest request, CancellationToken ct)
        {
            var routePlan = TripPlan.Create(request.TotalDistanceMeters, request.OffroadDistanceMeters,TimeSpan.FromSeconds( request.Duration.TotalSeconds), request.ElevationGainMeters, request.ElevationLossMeters);
            var route = Trip.Create(request.Name, TripType.Route, routePlan);

            await _repository.AddAsync(route, ct);

            return route.Id;
        }

        public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct)
        {
            var route = await _repository.GetByIdAsync(id, ct);

            if (route is null)
                return Error.NotFound("Route", id);

            await _repository.DeleteAsync(id, ct);

            return true;
        }

        public async Task<Result<TripResult>> PlanAsync(PlanRouteRequest request, CancellationToken ct)
        {
            var validationResult = await _planValidator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Error.Validation(errors);
            }

            var intent = request.ToRouteIntent();
            var profile = request.ToUserProfile();

            try
            {
                var routeResult = await _routeFinder.FindRouteAsync(intent, profile, ct);
                return routeResult.Bind<TripResult>(r =>
                     r.ToTripResult()
                );
            }
            catch (RoutingProviderException ex)
            {
                _logger.LogWarning(ex, "Routing provider failed during route planning");
                return PlanningErrorMappings.MapRoutingProviderError(ex);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain validation failed during route planning");
                return Error.Validation(ex.Message);
            }
        }
    }
}
