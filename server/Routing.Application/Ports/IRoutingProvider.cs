using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Domain.ValueObjects;

namespace Routing.Application.Ports
{
    public interface IRoutingProvider
    {
        //remove the whole intent and split it to multiple parameters, gh shouldn know about some shitty user's intents
        Task<List<ProviderRoute>> GetRoutesAsync(RouteIntent intent, CancellationToken cancellationToken);

        Task<ProviderRoute> GetRouteAsync(Coordinate start, Coordinate end, IReadOnlyList<Coordinate> waypoints , CancellationToken cancellationToken);
    }
}
