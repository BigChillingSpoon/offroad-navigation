using Routing.Application.Ports;
using Microsoft.Extensions.Options;
using Routing.Application.Planning.Exceptions;
using Routing.Application.Planning.Candidates.Models;
using System.Text.Json;
using Routing.Infrastructure.GraphHopper.Mappings;
using Routing.Infrastructure.GraphHopper.DTOs;
using Routing.Infrastructure.GraphHopper.Builders;
using Routing.Domain.Utilities;
using System.Net.Http.Json;
using Routing.Domain.ValueObjects; 

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
        
        public async Task<List<ProviderRoute>> GetRoutesAsync(IReadOnlyList<Coordinate> points, RoutingPreferences preferences, CancellationToken cancellationToken)
        {
            if (points is null || points.Count < 2)
                throw new ArgumentException("At least two points are required.", nameof(points));

            // GraphHopper's alternative_route algorithm only supports a plain start->end request -
            // it doesn't support via-points/waypoints. Only offer it when there are none.
            var hasWaypoints = points.Count > 2;

            var requestPayload = new GraphHopperRouteRequest
            {
                Points = points.Select(p => new[] { p.Longitude, p.Latitude }).ToArray(),
                Profile = GraphHopperProfileBuilder.ResolveProfileName(preferences),
                CustomModel = GraphHopperProfileBuilder.BuildCustomModel(preferences),
                Elevation = _graphHopperOptions.Elevation,
                Instructions = _graphHopperOptions.Instructions,
                CalcPoints = _graphHopperOptions.CalcPoints,
                PointsEncoded = _graphHopperOptions.PointsEncoded,
                Details = _graphHopperOptions.RequestedDetails,
                Algorithm = hasWaypoints ? null : _graphHopperOptions.Algorithm,
                AlternativeRouteMaxPaths = hasWaypoints ? null : _graphHopperOptions.AlternativeRouteMaxPaths,
                AlternativeRouteMaxShareFactor = hasWaypoints ? null : _graphHopperOptions.AlternativeRouteMaxShareFactor,
                AlternativeRouteMaxWeightFactor = hasWaypoints ? null : _graphHopperOptions.AlternativeRouteMaxWeightFactor,
                ChDisable = _graphHopperOptions.ChDisable
            };

            // points[1] is intent.End for a plain 2-point route, and the first waypoint (or start
            // itself, if there are none) for a loop. Not points[^1] - for a closed loop that's
            // always the start again (distance 0), which would collapse the timeout to its minimum.
            var dynamicTimeout = CalculateDynamicTimeout(points[0], points[1]);
            var response = await ExecuteRouteRequestAsync(requestPayload, dynamicTimeout, cancellationToken);

            if (response?.Paths is null || !response.Paths.Any())
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
        
        
    }
}