using FluentValidation;
using Routing.Application.Contracts.Models;

namespace Routing.Application.Contracts.Validators
{
    public class PlanRouteRequestValidator : AbstractValidator<PlanRouteRequest>
    {
        public PlanRouteRequestValidator()
        {
            RuleFor(x => x.StartLatitude)
                .InclusiveBetween(-90.0, 90.0)
                .WithMessage("Start latitude must be between -90 and 90.");

            RuleFor(x => x.StartLongitude)
                .InclusiveBetween(-180.0, 180.0)
                .WithMessage("Start longitude must be between -180 and 180.");

            RuleFor(x => x.EndLatitude)
                .InclusiveBetween(-90.0, 90.0)
                .WithMessage("End latitude must be between -90 and 90.");

            RuleFor(x => x.EndLongitude)
                .InclusiveBetween(-180.0, 180.0)
                .WithMessage("End longitude must be between -180 and 180.");

            RuleFor(x => x.RouteBalance)
                .IsInEnum()
                .WithMessage("Invalid route balance value.");

            RuleFor(x => x)
                .Must(x => x.StartLatitude != x.EndLatitude || x.StartLongitude != x.EndLongitude)
                .WithMessage("Start and end coordinates must be different.");
        }
    }
}
