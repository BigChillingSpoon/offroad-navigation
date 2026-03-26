using FluentValidation;
using Routing.Application.Contracts.Models;

namespace Routing.Application.Contracts.Validators
{
    public class FindLoopsRequestValidator : AbstractValidator<FindLoopsRequest>
    {
        public FindLoopsRequestValidator()
        {
            RuleFor(x => x.StartLatitude)
                .InclusiveBetween(-90.0, 90.0)
                .WithMessage("Start latitude must be between -90 and 90.");

            RuleFor(x => x.StartLongitude)
                .InclusiveBetween(-180.0, 180.0)
                .WithMessage("Start longitude must be between -180 and 180.");

            RuleFor(x => x.PreferredLengthKm)
                .GreaterThan(0)
                .WithMessage("Preferred loop length must be greater than 0.");

            RuleFor(x => x.MaxDriveDistanceKm)
                .GreaterThan(0)
                .WithMessage("Max drive distance must be greater than 0.");

            RuleFor(x => x.MaxDriveDistanceKm)
                .GreaterThanOrEqualTo(x => x.PreferredLengthKm)
                .WithMessage("Max drive distance must be greater than or equal to preferred length.");
        }
    }
}
