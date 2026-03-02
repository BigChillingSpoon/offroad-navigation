
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;

namespace Routing.Application.Abstractions
{
    public interface IRoutingProvider
    {
        Task<ProviderRoute?> GetRouteAsync(RouteIntent intent, CancellationToken cancellationToken);
    }
}
