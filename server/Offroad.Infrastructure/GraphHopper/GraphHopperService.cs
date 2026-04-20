using Routing.Application.Abstractions;
using Microsoft.Extensions.Options;
using Routing.Application.Planning.Exceptions;
using Routing.Application.Planning.Candidates.Models;
using System.Text.Json;
using Routing.Infrastructure.GraphHopper.Mappings;
using Routing.Infrastructure.GraphHopper.DTOs;
using Routing.Infrastructure.GraphHopper.Builders;
using Routing.Application.Planning.Intents;
using Routing.Domain.Utilities;
using System.Net.Http.Json;
using Routing.Domain.ValueObjects; // P�id�no pro Coordinate

namespace Routing.Infrastructure.GraphHopper
{
    public sealed class GraphHopperService : IRoutingProvider
    {
        public static readonly HttpRequestOptionsKey<TimeSpan> DynamicTimeoutKey = new("GraphHopperDynamicTimeout");

        private const double BaseTimeoutSeconds = 4.0;
        private const double SecondsPerTenKm = 1.0;
        private const double MaxTimeoutSeconds = 60.0;

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

            var dynamicTimeout = CalculateDynamicTimeout(intent.Start, intent.End);
            var response = await ExecuteRouteRequestAsync(requestPayload, dynamicTimeout, cancellationToken);

            if (response?.Paths is null)
                throw new RoutingProviderException(RoutingProviderErrorCategory.InvalidResponse, "Missing paths in routing response.");

            return response.Paths.Select(p => _graphHopperResponseMapper.ToProviderRoute(p)).ToList();
        }

        private async Task<GraphHopperRouteResponse?> ExecuteRouteRequestAsync(GraphHopperRouteRequest requestPayload, TimeSpan dynamicTimeout, CancellationToken cancellationToken)
        {
            var url = BuildUrl();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = JsonContent.Create(requestPayload, options: _jsonOptions)
                };
                request.Options.Set(DynamicTimeoutKey, dynamicTimeout);

                using var response = await _httpClient.SendAsync(request, cancellationToken);

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

        
        private static TimeSpan CalculateDynamicTimeout(Coordinate start, Coordinate end)
        {
            var straightLineMeters = GeoCalculator.CalculateDistance(start, end);
            var straightLineKm = straightLineMeters / 1000.0;

            var timeoutSeconds = BaseTimeoutSeconds + (straightLineKm / 10.0) * SecondsPerTenKm;
            timeoutSeconds = Math.Min(timeoutSeconds, MaxTimeoutSeconds);

            return TimeSpan.FromSeconds(timeoutSeconds);
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
        
        public async Task<List<ProviderRoute>> GetLoopsAsync(LoopIntent intent, CancellationToken cancellationToken)
        {
            var requestPayload = new GraphHopperRouteRequest
            {
                Points = new[]
                {
                    new[] { intent.Start.Longitude, intent.Start.Latitude } //for now we are sending only start point, in future we have to calculate start point in the radius not user's start point
                },
                Profile = GraphHopperProfileBuilder.ResolveProfileName(intent),
                CustomModel = GraphHopperProfileBuilder.BuildCustomModel(intent),
                Elevation = _graphHopperOptions.Elevation,
                Instructions = _graphHopperOptions.Instructions,
                CalcPoints = _graphHopperOptions.CalcPoints,
                PointsEncoded = _graphHopperOptions.PointsEncoded,
                Details = _graphHopperOptions.RequestedDetails,
                Algorithm = "round_trip",
                ChDisable = true,
                RoundTripDistance = (int)intent.PreferredLengthKm * 1000, 
                RoundTripSeed = 2 
            };

            var dynamicTimeout = CalculateDynamicTimeoutForLoop(intent.PreferredLengthKm);
            var response = await ExecuteLoopRequestAsync(requestPayload, dynamicTimeout, cancellationToken);

            if (response?.Paths is null)
                throw new RoutingProviderException(RoutingProviderErrorCategory.InvalidResponse, "Missing paths in routing response.");

            return response.Paths.Select(p => _graphHopperResponseMapper.ToProviderRoute(p)).ToList();
        }

        private async Task<GraphHopperRouteResponse?> ExecuteLoopRequestAsync(GraphHopperRouteRequest requestPayload, TimeSpan dynamicTimeout, CancellationToken cancellationToken)
        {
            var url = BuildUrl();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = JsonContent.Create(requestPayload, options: _jsonOptions)
                };
                request.Options.Set(DynamicTimeoutKey, dynamicTimeout);
                var rawJson = await request.Content.ReadAsStringAsync();
                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    GraphhopperExceptionMapper.ThrowExceptionBasedOnStatusCode(response.StatusCode, responseBody);
                }

                return await response.Content.ReadFromJsonAsync<GraphHopperRouteResponse>(_jsonOptions, cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new RoutingProviderException(RoutingProviderErrorCategory.Timeout, "GraphHopper loop request timed out.", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new RoutingProviderException(RoutingProviderErrorCategory.Unavailable, "GraphHopper is unreachable.", ex);
            }
        }

        private static TimeSpan CalculateDynamicTimeoutForLoop(double distanceKm)
        {
            var timeoutSeconds = BaseTimeoutSeconds + (distanceKm / 10.0) * SecondsPerTenKm;
            timeoutSeconds = Math.Min(timeoutSeconds, MaxTimeoutSeconds);

            return TimeSpan.FromSeconds(timeoutSeconds);
        }
    }
}