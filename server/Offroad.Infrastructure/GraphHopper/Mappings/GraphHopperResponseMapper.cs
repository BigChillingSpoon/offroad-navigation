using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Exceptions;
namespace Routing.Infrastructure.GraphHopper.Mappings
{
    public static class GraphHopperResponseMapper
    {
        public static ProviderRoute ToProviderRoute(this GraphHopperPath path)
        {
            if (path.Distance < 0)
                throw new RoutingProviderException(RoutingProviderErrorCategory.InvalidResponse,"Distance < 0");

            if (path.TimeMs < 0)
                throw new RoutingProviderException(RoutingProviderErrorCategory.InvalidResponse, "TimeMs < 0");

            if (string.IsNullOrEmpty(path.Points))
                throw new RoutingProviderException(RoutingProviderErrorCategory.InvalidResponse, "Missing geometry");

            return new ProviderRoute
            {
                Distance = path.Distance,
                Duration = TimeSpan.FromMicroseconds(path.TimeMs),
                Ascend = path.Ascend,
                Descend = path.Descend,
                Polyline = path.Points
            };
        }
    }
}
