
namespace Routing.Application.Abstractions
{
    public interface IGraphHopperService
    {
        Task<string> GetRouteJsonAsync(double fromLat, double fromLon, double toLat, double toLon, string profile, CancellationToken cancellationToken);
    }
}
