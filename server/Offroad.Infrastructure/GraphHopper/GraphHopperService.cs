using Routing.Application.Abstractions;
using Microsoft.Extensions.Options;
using Routing.Application.Planning.Exceptions;
using Routing.Application.Planning.Candidates.Models;
using System.Text.Json;
using Routing.Infrastructure.GraphHopper.Mappings;
using Routing.Infrastructure.GraphHopper.DTOs;
using Routing.Infrastructure.GraphHopper.Builders;
using Routing.Application.Planning.Intents;
using System.Net.Http.Json;


namespace Routing.Infrastructure.GraphHopper
{
    public sealed class GraphHopperService : IRoutingProvider
    {
        private readonly HttpClient _httpClient;
        private readonly GraphHopperOptions _graphHopperOptions;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly GraphHopperResponseMapper _graphHopperResponseMapper;

        public GraphHopperService(HttpClient httpClient, IOptions<GraphHopperOptions> graphHopperOptions, JsonSerializerOptions jsonOptions, GraphHopperResponseMapper graphHopperResponseMapper)
        {
            _httpClient = httpClient;
            _graphHopperOptions = graphHopperOptions.Value;
            _jsonOptions = jsonOptions;
            _graphHopperResponseMapper = graphHopperResponseMapper;
        }

        public async Task<List<ProviderRoute>> GetRoutesAsync(RouteIntent intent, CancellationToken cancellationToken)
        {
            var requestPayload = new GraphHopperRouteRequest
            {
                Points = new[]
                {
                    new[] { intent.Start.Longitude, intent.Start.Latitude },
                    new[] { intent.End.Longitude, intent.End.Latitude }
                },
                Profile = GraphHopperProfileBuilder.ResolveProfileName(intent),
                CustomModel = GraphHopperProfileBuilder.BuildCustomModel(intent),
                Elevation = _graphHopperOptions.Elevation,
                Instructions = _graphHopperOptions.Instructions,
                CalcPoints = _graphHopperOptions.CalcPoints,
                PointsEncoded = _graphHopperOptions.PointsEncoded,
                Details = _graphHopperOptions.RequestedDetails,
                Algorithm = _graphHopperOptions.Algorithm,
                AlternativeRouteMaxPaths = _graphHopperOptions.AlternativeRouteMaxPaths,
                AlternativeRouteMaxShareFactor = _graphHopperOptions.AlternativeRouteMaxShareFactor,
                AlternativeRouteMaxWeightFactor = _graphHopperOptions.AlternativeRouteMaxWeightFactor,
                ChDisable = _graphHopperOptions.ChDisable
            };

            var response = await ExecuteRouteRequestAsync(requestPayload, cancellationToken);

            if (response?.Paths is null)
                throw new RoutingProviderException(RoutingProviderErrorCategory.InvalidResponse, "Missing paths in routing response.");
            return response.Paths.Select(p => _graphHopperResponseMapper.ToProviderRoute(p)).ToList();//could be empty 
        }

        private async Task<GraphHopperRouteResponse?> ExecuteRouteRequestAsync(GraphHopperRouteRequest requestPayload, CancellationToken cancellationToken)
        {
            var url = BuildUrl();
            //todo create retry logic
            try
            {
                using var response = await _httpClient.PostAsJsonAsync(url, requestPayload, _jsonOptions, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    GraphhopperExceptionMapper.ThrowExceptionBasedOnStatusCode(response.StatusCode, responseBody);
                }

                return await response.Content.ReadFromJsonAsync<GraphHopperRouteResponse>(_jsonOptions, cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new RoutingProviderException(RoutingProviderErrorCategory.Timeout, "GraphHopper request timed out.", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new RoutingProviderException(RoutingProviderErrorCategory.Unavailable, "GraphHopper is unreachable.", ex);
            }
        }

        private string BuildUrl()
        {
            var url = "/route";

            if (!string.IsNullOrWhiteSpace(_graphHopperOptions.ApiKey))
            {
                url += $"?key={Uri.EscapeDataString(_graphHopperOptions.ApiKey)}";
            }

            return url;
        }
    }
}