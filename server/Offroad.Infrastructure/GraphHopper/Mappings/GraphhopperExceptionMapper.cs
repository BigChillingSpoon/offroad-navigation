using System.Net;
using Routing.Application.Planning.Exceptions;

namespace Routing.Infrastructure.GraphHopper.Mappings
{
    public static class GraphhopperExceptionMapper
    {
        public static void ThrowExceptionBasedOnStatusCode(HttpStatusCode statusCode)
        {
            switch (statusCode)
            {
                case HttpStatusCode.BadRequest:
                case HttpStatusCode.UnprocessableEntity:
                    throw new RoutingProviderException(RoutingProviderErrorCategory.HttpError, "The routing request was invalid.");

                case HttpStatusCode.Unauthorized:
                    throw new RoutingProviderException(RoutingProviderErrorCategory.HttpError, "Unauthorized access to the GraphHopper.");

                case HttpStatusCode.TooManyRequests:
                    throw new RoutingProviderException(RoutingProviderErrorCategory.HttpError, "Rate limit exceeded for the GraphHopper.");

                default:
                    throw new RoutingProviderException(RoutingProviderErrorCategory.HttpError, "Routing service failed."); // general
            }
        }
    }
}
