using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Application.Ports.DTOs;

namespace Routing.Application.Ports
{
    public interface IRoutingProvider
    {
        Task<List<ProviderRoute>> GetRoutesAsync(RouteIntent intent, CancellationToken cancellationToken);

        /// <summary>
        /// Generates a single, continuous route from an ordered list of coordinates (a skeleton).
        /// </summary>
        /// <param name="request">A DTO containing the waypoints and profile for the route.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A single ProviderRoute representing the complete loop.</returns>
        Task<ProviderRoute> GetRouteFromSkeletonAsync(ProviderSkeletonRequest request, CancellationToken cancellationToken);
    }
}
