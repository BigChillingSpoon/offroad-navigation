using Offroad.Core;
using Routing.Application.Planning.Exceptions;

namespace Routing.Application.Mappings
{
    public static class PlanningErrorMappings
    {
        public static Error MapRoutingProviderError(RoutingProviderException ex)
        {
            return ex.ErrorCathegory switch
            {
                RoutingProviderErrorCategory.Timeout =>
                    Error.Timeout("Routing provider request timed out."),

                RoutingProviderErrorCategory.HttpError =>
                    Error.ExternalServiceFailure("Routing provider returned an error."),

                RoutingProviderErrorCategory.InvalidResponse =>
                    Error.ExternalServiceFailure("Routing provider returned an invalid response."),

                RoutingProviderErrorCategory.Unavailable =>
                    Error.ExternalServiceFailure("Routing provider is unable to be reached."),

                RoutingProviderErrorCategory.OutOfBounds =>
                    Error.Validation("The requested coordinates are outside the currently loaded map area."),
                _ =>
                    Error.ExternalServiceFailure("An unexpected routing error occurred.")
            };
        }
    }
}
