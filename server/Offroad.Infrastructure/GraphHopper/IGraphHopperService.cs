using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Infrastructure.GraphHopper
{
    public interface IGraphHopperService
    {
        Task<string> GetRouteJsonAsync(double fromLat, double fromLon, double toLat, double toLon, string profile, CancellationToken cancellationToken);
    }
}
