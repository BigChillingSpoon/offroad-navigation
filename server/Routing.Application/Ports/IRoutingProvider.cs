
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;

namespace Routing.Application.Abstractions
{
    public interface IRoutingProvider
    {
        Task<List<ProviderRoute>> GetRoutesAsync(RouteIntent intent, CancellationToken cancellationToken);
        Task<List<ProviderRoute>> GetLoopsAsync(LoopIntent intent, CancellationToken cancellationToken);
    }
}
