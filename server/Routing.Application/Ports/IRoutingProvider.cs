using Routing.Application.Planning.Candidates.Models;
using Routing.Domain.ValueObjects;

namespace Routing.Application.Ports
{
    public interface IRoutingProvider
    {
        Task<List<ProviderRoute>> GetRoutesAsync(IReadOnlyList<Coordinate> points, RoutingPreferences preferences, CancellationToken cancellationToken);
    }
}
