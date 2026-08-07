using Routing.Application.Ports;
using Routing.Domain.Enums;
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Domain.ValueObjects;
using Routing.Application.Planning.Exceptions;
using Routing.Application.Planning.Encoding;
using Routing.Application.Planning.Candidates.Builders;
using Routing.Application.Planning.Candidates.Arenas;
using Routing.Application.Planning.Extensions;
using Routing.Application.Planning.DEBUG;
using Routing.Domain.Utilities;
using Routing.Application.Abstractions.Persistence;
using Routing.Application.Planning.Skeletons.Services;
using Routing.Application.Planning.Skeletons.Models;

namespace Routing.Application.Planning.Candidates.Generators
{
    public sealed class LoopCandidateGenerator : ICandidateGenerator<LoopIntent, LoopTripCandidate>
    {
        private readonly IRoutingProvider _routingProvider;
        private readonly IRestrictedZoneBuilder _restrictedZoneBuilder;
        private readonly INodeRepository _nodeRepository;
        private readonly IEdgeRepository _edgeRepository;
        private readonly ILoopSkeletonFinder _skeletonFinder;
        private readonly IArenaFinder _arenaFinder;

        public LoopCandidateGenerator(
            IRoutingProvider routingProvider,
            IRestrictedZoneBuilder restrictedZoneBuilder,
            INodeRepository nodeRepository,
            IEdgeRepository edgeRepository,
            ILoopSkeletonFinder skeletonFinder,
            IArenaFinder arenaFinder)
        {
            _routingProvider = routingProvider;
            _restrictedZoneBuilder = restrictedZoneBuilder;
            _nodeRepository = nodeRepository;
            _edgeRepository = edgeRepository;
            _skeletonFinder = skeletonFinder;
            _arenaFinder = arenaFinder;
        }

        public async Task<IReadOnlyList<LoopTripCandidate>> GenerateCandidatesAsync(LoopIntent intent, CancellationToken ct)
        {
            // 1. GET ALL SUITABLE OFFROAD ENTRANCES
            var entrances = await _arenaFinder.FindEntrancesAsync(intent.Start, intent.PreferredLengthKm, intent.MaxDriveDistanceKm, maxEntrances: 3, ct);
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
            var candidateTasks = allSkeletons.Select((skeleton, index) => GenerateSingleCandidateAsync(skeleton, intent, index, ct));
            var candidates = await Task.WhenAll(candidateTasks);

            return candidates.Where(c => c is not null).ToList()!;
        }

        private async Task<LoopTripCandidate?> GenerateSingleCandidateAsync(LoopSkeleton skeleton, LoopIntent intent, int index, CancellationToken ct)
        {
            var routes = await _routingProvider.GetRoutesAsync(BuildLoopPoints(skeleton), intent.ToRoutingPreferences(), ct);
            return await MapToCandidateAsync(routes.First(), skeleton.Entrance, index);
        }

        private static IReadOnlyList<Coordinate> BuildLoopPoints(LoopSkeleton skeleton)
        {
            var points = new List<Coordinate> { skeleton.Entrance };
            points.AddRange(skeleton.Waypoints);
            points.Add(skeleton.Entrance);
            return points;
        }

        private async Task<IReadOnlyList<LoopSkeleton>> ProcessArenaAsync(Coordinate entrance, LoopIntent intent, CancellationToken ct)
        {
            // 1. ARENA SELECTION & SINGLE DB FETCH: nodes within the arena radius, then only the
            // edges that connect two of those nodes (guarantees a fully-resolvable graph).
            var arenaNodes = await _nodeRepository.GetNodesNearAsync(
                entrance,
                intent.PreferredLengthKm * 1000 / 2,
                ct);

            if (!arenaNodes.Any())
                return Array.Empty<LoopSkeleton>();

            var arenaNodeIds = arenaNodes.Select(n => n.Id).ToList();
            var arenaEdges = await _edgeRepository.GetEdgesConnectingAsync(arenaNodeIds, ct);

            // The start of the search must be a real graph node - pick the one nearest the requested entrance.
            var startNode = arenaNodes
                .OrderBy(n => GeoCalculator.CalculateDistance(n.Coordinate, entrance))
                .First();

            var options = new SkeletonSearchOptions(intent.AllowGates, intent.AllowPrivateRoads, intent.AllowRestrictedZones);

            return _skeletonFinder.FindSkeletons(startNode, arenaNodes, arenaEdges, intent.PreferredLengthKm * 1000, options)
                .Take(5)
                .ToList();
        }

        private async Task<LoopTripCandidate> MapToCandidateAsync(ProviderRoute route, Coordinate entranceCoordinate, int index)
        {
            var geometry = GetValidGeometry(route.Polyline);

            PlanningDebugExtensions.LogToGPX(geometry, $"./debug_loop_candidate_{index}.gpx");

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
                    estimatedTransitDistanceMeters: default,
                    entranceCoordinate: entranceCoordinate);
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
