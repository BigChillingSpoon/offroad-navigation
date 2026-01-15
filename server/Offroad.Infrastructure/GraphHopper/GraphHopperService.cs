using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Routing.Application.Abstractions;
using System.Globalization;
using Microsoft.Extensions.Options;

namespace Routing.Infrastructure.GraphHopper
{
    public sealed class GraphHopperService : IGraphHopperService
    {
        private readonly HttpClient _httpClient;
        private readonly GraphHopperOptions _options;

        public GraphHopperService(HttpClient httpClient, IOptions<GraphHopperOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<string> GetRouteJsonAsync(double fromLat, double fromLon, double toLat, double toLon, string profile, CancellationToken cancellationToken)
        {
            var url =
                   $"/route" +
                   $"?point={fromLat.ToString(CultureInfo.InvariantCulture)},{fromLon.ToString(CultureInfo.InvariantCulture)}" +
                   $"&point={toLat.ToString(CultureInfo.InvariantCulture)},{toLon.ToString(CultureInfo.InvariantCulture)}" +
                   $"&profile={Uri.EscapeDataString(profile)}" +
                   $"&instructions={_options.Instructions.ToString().ToLowerInvariant()}" +
                   $"&calc_points={_options.CalcPoints.ToString().ToLowerInvariant()}" +
                   $"&points_encoded={_options.PointsEncoded.ToString().ToLowerInvariant()}" +
                   $"&elevation=true";
            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                url += $"&key={Uri.EscapeDataString(_options.ApiKey)}";
            }

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
    }
}
