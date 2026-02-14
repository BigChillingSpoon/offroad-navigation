using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Routing.Application.Abstractions;
using System.Globalization;
using Microsoft.Extensions.Options;
using Offroad.Core;
using Routing.Application.Planning.Exceptions;
using Routing.Application.Planning.Candidates.Models;
using System.Text.Json;
using Routing.Infrastructure.GraphHopper.Mappings;
using Routing.Infrastructure.GraphHopper.DTOs;


namespace Routing.Infrastructure.GraphHopper
{
    public sealed class GraphHopperService : IRoutingProvider
    {
        private readonly HttpClient _httpClient;
        private readonly GraphHopperOptions _graphHopperOptions;
        private readonly JsonSerializerOptions _jsonOptions;

        public GraphHopperService(HttpClient httpClient, IOptions<GraphHopperOptions> graphHopperOptions, JsonSerializerOptions jsonOptions)
        {
            _httpClient = httpClient;
            _graphHopperOptions = graphHopperOptions.Value;
            _jsonOptions = jsonOptions;
        }

        public async Task<ProviderRoute?> GetRouteAsync(double fromLat, double fromLon, double toLat, double toLon, string profile, CancellationToken cancellationToken)
        {
            var json = await GetRouteJsonAsync(fromLat, fromLon, toLat, toLon, profile, cancellationToken);

            var response = JsonSerializer.Deserialize<GraphHopperRouteResponse>(json, _jsonOptions);

            if (response?.Paths is null)
                throw new RoutingProviderException(RoutingProviderErrorCategory.InvalidResponse, "Missing paths in routing response.");

            return response.Paths.Select(p => p.ToProviderRoute()).FirstOrDefault();//could be empty 
        }

        private async Task<string> GetRouteJsonAsync(double fromLat, double fromLon, double toLat, double toLon, string profile, CancellationToken cancellationToken)
        {
            var url = BuildUrl(fromLat, fromLon, toLat, toLon, profile);
            //todo create retry logic
            try
            {
                using var response = await _httpClient.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    GraphhopperExceptionMapper.ThrowExceptionBasedOnStatusCode(
                        response.StatusCode);
                }

                return await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // todo LOG ex
                throw new RoutingProviderException(RoutingProviderErrorCategory.Timeout, "GraphHopper request timed out", ex);
            }
        }

        private string BuildUrl(double fromLat, double fromLon, double toLat, double toLon, string profile)
        {
            var url =
               $"/route" +
               $"?point={fromLat.ToString(CultureInfo.InvariantCulture)},{fromLon.ToString(CultureInfo.InvariantCulture)}" +
               $"&point={toLat.ToString(CultureInfo.InvariantCulture)},{toLon.ToString(CultureInfo.InvariantCulture)}" +
               $"&profile={Uri.EscapeDataString(profile)}" +
               $"&instructions={_graphHopperOptions.Instructions.ToString().ToLowerInvariant()}" +
               $"&calc_points={_graphHopperOptions.CalcPoints.ToString().ToLowerInvariant()}" +
               $"&points_encoded={_graphHopperOptions.PointsEncoded.ToString().ToLowerInvariant()}" +
               $"&elevation=true" +
               "&details=road_class" +
               "&details=surface";

            if (!string.IsNullOrWhiteSpace(_graphHopperOptions.ApiKey))
            {
                url += $"&key={Uri.EscapeDataString(_graphHopperOptions.ApiKey)}";
            }

            return url;
        }
    }
}
