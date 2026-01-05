using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace Routing.Infrastructure.GraphHopper
{
    public class GraphHopperService : IGraphHopperService
    {
        private readonly HttpClient _httpClient;
        public GraphHopperService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://graphhopper.com/api/1")
            };
        }

        public async Task<string> GetRouteJsonAsync(double fromLat, double fromLon, double toLat, double toLon, string profile, CancellationToken cancellationToken)
        {
            // Minimal request – zatím bez instrukcí, bez geo bodů (rychlé a jednoduché)
            var url =
                $"/route" +
                $"?point={fromLat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{fromLon.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                $"&point={toLat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{toLon.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                $"&profile={Uri.EscapeDataString(profile)}" +
                $"&instructions=false" +
                $"&calc_points=false";

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
    }
}
