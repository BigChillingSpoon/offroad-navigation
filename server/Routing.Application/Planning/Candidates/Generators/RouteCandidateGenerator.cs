using Routing.Application.Abstractions;
using Routing.Domain.Enums;
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Domain.ValueObjects;
using Routing.Application.Planning.Exceptions;
using Routing.Application.Planning.Encoding;
using Routing.Application.Planning.Candidates.Builders;
using Routing.Application.Planning.Extensions;
using Routing.Application.Planning.DEBUG;
using Routing.Domain.Utilities;

namespace Routing.Application.Planning.Candidates.Generators
{
    public sealed class RouteCandidateGenerator : ICandidateGenerator<RouteIntent, TripCandidate>
    {
        private readonly IRoutingProvider _routingProvider;
        private readonly IRestrictedZoneBuilder _restrictedZoneBuilder;

        public RouteCandidateGenerator(IRoutingProvider routingProvider, IRestrictedZoneBuilder restrictedZoneBuilder)
        {
            _routingProvider = routingProvider;
            _restrictedZoneBuilder = restrictedZoneBuilder;
        }

        public async Task<IReadOnlyList<TripCandidate>> GenerateCandidatesAsync(RouteIntent intent, CancellationToken ct)
        {
            var routes = await _routingProvider.GetRoutesAsync(intent, ct);

            if (routes is null || !routes.Any())
                return Array.Empty<TripCandidate>();

            int index = 0;
            var candidateTasks = routes.Select(route => MapToCandidateAsync(route, index++));

            TripCandidate[] candidates = await Task.WhenAll(candidateTasks);

            return candidates.ToList();
        }

        private async Task<TripCandidate> MapToCandidateAsync(ProviderRoute route, int index)
        {
            var geometry = GetValidGeometry(route.Polyline);

            //index is only for debug, to be removed 
            PlanningDebugExtensions.LogToGPX(geometry, $"C:\\tmp\\debug_route_candidate_{index}.gpx");

            var maxEdgeIndex = geometry.Count - 1;

            var segments = SegmentBuilder.Build(
                geometry,
                route.RoadClassIntervals.EnsureFullCoverage(maxEdgeIndex, RoadClassType.UNKNOWN),
                route.SurfaceIntervals.EnsureFullCoverage(maxEdgeIndex, SurfaceType.UNKNOWN),
                route.TrackTypeIntervals.EnsureFullCoverage(maxEdgeIndex, TrackType.UNKNOWN));

            var barriers = BarrierBuilder.Build(route.BarrierIntervals, geometry);

            var restrictedZones = await _restrictedZoneBuilder.BuildAsync(route.RoadAccessIntervals, geometry);

            var maxGradient = GeoCalculator.CalculateMaxGradientPercentage(geometry);

            return TripCandidate.Create(
                segments,
                barriers,
                restrictedZones,
                route.Polyline,
                route.Distance,
                route.Duration,
                route.Ascend,
                route.Descend,
                maxGradient);
        }

        //translate technical exception to domain specific one, so we can later on decide how to handle it based on category
        private IReadOnlyList<Coordinate> GetValidGeometry(EncodedPolyline polyline)
        {
            try
            {
                var decoded = PolylineDecoder.Decode(polyline);
                if (decoded.Count < 2)
                    throw new RoutingProviderException(RoutingProviderErrorCategory.InvalidResponse, "Routing engine returned invalid geometry: Decoded polyline contains less than 2 points.");

                return decoded;
            }
            catch (InvalidPolylineException ex)
            {
                throw new RoutingProviderException(RoutingProviderErrorCategory.InvalidResponse, ex.Message);
            }
        }
    }
}