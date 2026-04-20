using FluentValidation;
using Offroad.Core;
using Routing.Application.Contracts.Models;
using Routing.Application.Contracts.Responses;
using Routing.Application.Mappings;
using Routing.Application.Planning.Exceptions;
using Routing.Domain.Enums;
using Routing.Domain.Exceptions;
using Routing.Domain.Models;
using Routing.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Pipelines;

namespace Routing.Application.Loops.Commands
{
    internal sealed class LoopsCommands : ILoopsCommands
    {
        private readonly ITripRepository _repository;
        private readonly IPlanningPipeline<LoopIntent, LoopTripCandidate> _planningPipeline;
        private readonly IValidator<FindLoopsRequest> _findValidator;
        private readonly ILogger<LoopsCommands> _logger;

        public LoopsCommands(ITripRepository repository, IPlanningPipeline<LoopIntent, LoopTripCandidate> planningPipeline, IValidator<FindLoopsRequest> findValidator, ILogger<LoopsCommands> logger)
        {
            _repository = repository;
            _planningPipeline = planningPipeline;
            _findValidator = findValidator;
            _logger = logger;
        }

        public async Task<Result<Guid>> SaveAsync(SaveLoopRequest request, CancellationToken ct)
        {
            var loopPlan = TripPlan.Create(request.TotalDistanceMeters, request.OffroadDistanceMeters,TimeSpan.FromSeconds(request.Duration.TotalSeconds), request.ElevationGainMeters, request.ElevationLossMeters);
            var loop = Trip.Create(request.Name, TripType.Loop, loopPlan);

            await _repository.AddAsync(loop, ct);

            return loop.Id;
        }

        public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct)
        {
            var route = await _repository.GetByIdAsync(id, ct);

            if (route is null)
                return Error.NotFound("Loop", id);

            await _repository.DeleteAsync(id, ct);

            return true;
        }

        public async Task<Result<IReadOnlyList<TripResult>>> FindAsync(FindLoopsRequest request, CancellationToken ct)
        {
            var validationResult = await _findValidator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Error.Validation(errors);
            }

            var intent = request.ToLoopIntent();
            var profile = request.ToUserProfile();

            try
            {
                var plans = await _planningPipeline.PlanAsync(intent, profile, ct);

                if (!plans.Any())
                    return Error.Validation("No loops found.");

                var tripResults = plans
                    .Select(plan => Trip.Create("Test loop", TripType.Loop, plan))
                    .Select(trip => trip.ToTripResult(intent))
                    .ToList();

                return tripResults;
            }
            catch (RoutingProviderException ex)
            {
                _logger.LogWarning(ex, "Routing provider failed during loop finding");
                return PlanningErrorMappings.MapRoutingProviderError(ex);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain validation failed during loop finding");
                return Error.Validation(ex.Message);
            }
        }
    }
}
