using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Profiles;
using Routing.Domain.Models;

namespace Routing.Application.Planning.Finders
{
    public interface IRouteFinder
    {
        Task<Trip> FindRouteAsync(RouteIntent intent, UserRoutingProfile profile, CancellationToken ct);
    }
}
