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
using Routing.Application.Abstractions.Persistence;
using Routing.Application.Planning.Skeletons.Services;
using Routing.Application.Planning.Skeletons.Models;
using Routing.Domain.Models;
using Offroad.Routing.Application.Abstractions.Persistence;

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
            // 1. GET ALL SUITABLE OFFROAD ENTRANCES
            var entrances = await GetMostSuitableEntrancesAsync(intent.Start, intent.MaxDriveDistanceKm, maxEntrances: 3, ct);
            if (!entrances.Any())
                return Array.Empty<LoopTripCandidate>();

            // 2. PARALERLY GENERATE ALL ARENA SKELETONS
            var arenaTasks = entrances.Select(entrance => ProcessArenaAsync(entrance, intent, ct));
            var arenasResults = await Task.WhenAll(arenaTasks);

            // 3. GATHER ALL SKELETONS TOGETHER
            var allSkeletons = arenasResults.SelectMany(s => s).ToList();
            if (!allSkeletons.Any())
                return Array.Empty<LoopTripCandidate>();

            // 4. PARALLELY GENERATE CANDIDATES FOR ALL SKELETONS
            var candidateTasks = allSkeletons.Select((skeleton, index) => GenerateSingleCandidateAsync(skeleton, index, ct));
            var candidates = await Task.WhenAll(candidateTasks);

            return candidates.Where(c => c is not null).ToList()!;
        }

        //clean
        private async Task<LoopTripCandidate?> GenerateSingleCandidateAsync(LoopSkeleton skeleton, int index, CancellationToken ct)
        {
            var providerRoute = await _routingProvider.GetRouteAsync(skeleton.Entrance, skeleton.Entrance, skeleton.Waypoints, ct);
            return await MapToCandidateAsync(providerRoute, index);
        }

        //clean
        private async Task<IReadOnlyList<LoopSkeleton>> ProcessArenaAsync(Coordinate entrance, LoopIntent intent, CancellationToken ct)
        {
            var arenaHookpoints = await _hookpointRepository.GetHookpointsNearAsync(
                entrance,
                intent.PreferredLengthKm * 1000 / 2,
                ct);

            if (!arenaHookpoints.Any())
                return Array.Empty<LoopSkeleton>();

            //TODO modify after creation of final skeleton finder
            var startHookpoint = new Hookpoint(entrance, 0, 0, false);

            return _skeletonFinder.FindSkeletons(startHookpoint, arenaHookpoints, intent.PreferredLengthKm * 1000)
                .Take(5)
                .ToList();
        }

        //clean
        private async Task<IReadOnlyList<Coordinate>> GetMostSuitableEntrancesAsync(Coordinate userStart, double maxDriveDistanceKm, int maxEntrances, CancellationToken ct)
        {
            // TODO: Create smart arena finder
            return new List<Coordinate> { userStart };
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
