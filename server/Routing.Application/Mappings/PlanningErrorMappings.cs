using Offroad.Core;
using Offroad.Core.Exceptions;
using Routing.Application.Planning.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Mappings
{
    public static class PlanningErrorMappings
    {
        public static Error MapError(Exception ex)
        {
            switch (ex)
            {
                //more exceptions to be added
                case RoutingProviderException routingProviderException:
                    return MapRoutingProviderError(routingProviderException);
                default:
                    return Error.Internal("An unexpected error occurred while planning the route.");
            }
        }
        private static Error MapRoutingProviderError(RoutingProviderException ex)
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
                _ =>
                    Error.ExternalServiceFailure("An unexpected routing error occurred.")
            };
        }
    }
}
