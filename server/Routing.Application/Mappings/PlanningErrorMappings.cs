using Offroad.Core;
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
                    Error.Timeout(ex.Message),

                RoutingProviderErrorCategory.HttpError =>
                    Error.ExternalServiceFailure(ex.Message),

                RoutingProviderErrorCategory.InvalidResponse =>
                    Error.ExternalServiceFailure(ex.Message),

                _ =>
                    Error.ExternalServiceFailure(ex.Message)
            };
        }
    }
}
