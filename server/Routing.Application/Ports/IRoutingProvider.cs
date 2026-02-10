
using Routing.Application.Planning.Candidates.Models;

namespace Routing.Application.Abstractions
{
    public interface IRoutingProvider
    {
        Task<ProviderRoute?> GetRouteAsync(double fromLat, double fromLon, double toLat, double toLon, string profile, CancellationToken cancellationToken);
    }
}
