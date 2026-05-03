using Routing.Application.Ports;
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
using Routing.Application.Ports.Persistence;
using Routing.Application.Planning.Skeletons.Services;
using Routing.Application.Planning.Skeletons.Models;
using Routing.Application.Ports.DTOs;
using Routing.Domain.Models;

namespace Routing.Application.Planning.Candidates.Generators
{
    public sealed class LoopCandidateGenerator : ICandidateGenerator<LoopIntent, LoopTripCandidate>
    {
        private readonly IRoutingProvider _routingProvider;
        private readonly IRestrictedZoneBuilder _restrictedZoneBuilder;
        private readonly IHookpointRepository _hookpointRepository;
        private readonly ILoopSkeletonFinder _skeletonFinder;

        public LoopCandidateGenerator(
            IRoutingProvider routingProvider,
            IRestrictedZoneBuilder restrictedZoneBuilder,
            IHookpointRepository hookpointRepository,
            ILoopSkeletonFinder skeletonFinder)
        {
            _routingProvider = routingProvider;
            _restrictedZoneBuilder = restrictedZoneBuilder;
            _hookpointRepository = hookpointRepository;
            _skeletonFinder = skeletonFinder;
        }

        public async Task<IReadOnlyList<LoopTripCandidate>> GenerateCandidatesAsync(LoopIntent intent, CancellationToken ct)
        {
            var allHookpoints = await _hookpointRepository.GetHookpointsNearAsync(intent.Start, intent.PreferredLengthKm * 1000 / 2, ct);

            if (!allHookpoints.Any())
                return Array.Empty<LoopTripCandidate>();

            // For now, we use a single starting point. This will be expanded to use multiple entry points from Arenas.
            var startHookpoints = new List<Hookpoint>
            {
                new(0, intent.Start, 0, 0) // Placeholder for a real start hookpoint
            };

            // For each starting point, find all possible skeletons and flatten the result into a single list.
            var allSkeletons = startHookpoints
                .SelectMany(start => _skeletonFinder.FindSkeletons(start, allHookpoints, intent.PreferredLengthKm * 1000))
                .Take(30) //constant for now, we need to estimate best ones in future here
                .ToList();

            if (!allSkeletons.Any())
                return Array.Empty<LoopTripCandidate>();

            var candidateTasks = allSkeletons.Select((skeleton, index) => GenerateSingleCandidateAsync(skeleton, index, ct));

            //at the moment we are returning all together that means we are waiting for all of them to finish, to be improved in future
            return await Task.WhenAll(candidateTasks);
        }

        private async Task<LoopTripCandidate> GenerateSingleCandidateAsync(SkeletonCandidate skeleton, int index, CancellationToken ct)
        {
            var waypoints = skeleton.Hookpoints.Select(h => h.Location).ToList();
            var providerRequest = new ProviderSkeletonRequest(waypoints);
            var providerRoute = await _routingProvider.GetRouteFromSkeletonAsync(providerRequest, ct);
            return await MapToCandidateAsync(providerRoute, index);
        }

        private async Task<LoopTripCandidate> MapToCandidateAsync(ProviderRoute route, int index)
        {
            var geometry = GetValidGeometry(route.Polyline);

            PlanningDebugExtensions.LogToGPX(geometry, $"C:\\tmp\\debug_loop_candidate_{index}.gpx");

            var maxEdgeIndex = geometry.Count - 1;

            var segments = SegmentBuilder.Build(
                geometry,
                route.RoadClassIntervals.EnsureFullCoverage(maxEdgeIndex, RoadClassType.UNKNOWN),
                route.SurfaceIntervals.EnsureFullCoverage(maxEdgeIndex, SurfaceType.UNKNOWN),
                route.TrackTypeIntervals.EnsureFullCoverage(maxEdgeIndex, TrackType.UNKNOWN));

            var barriers = BarrierBuilder.Build(route.BarrierIntervals, geometry);

            var restrictedZones = await _restrictedZoneBuilder.BuildAsync(route.RoadAccessIntervals, geometry);

            var maxGradient = GeoCalculator.CalculateMaxGradientPercentage(geometry);

            return LoopTripCandidate.Create(
                    segments, barriers, restrictedZones, route.Polyline,
                    route.Distance, route.Duration, route.Ascend, route.Descend, maxGradient,
                    hookCoordinate: default,
                    hookPolylineIndex: default,
                    estimatedTransitDistanceMeters: default);
        }

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
