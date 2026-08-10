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
using Routing.Domain.Entities;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<LoopCandidateGenerator> _logger;

        public LoopCandidateGenerator(
            IRoutingProvider routingProvider,
            IRestrictedZoneBuilder restrictedZoneBuilder,
            INodeRepository nodeRepository,
            IEdgeRepository edgeRepository,
            ILoopSkeletonFinder skeletonFinder,
            IArenaFinder arenaFinder,
            ILogger<LoopCandidateGenerator> logger)
        {
            _routingProvider = routingProvider;
            _restrictedZoneBuilder = restrictedZoneBuilder;
            _nodeRepository = nodeRepository;
            _edgeRepository = edgeRepository;
            _skeletonFinder = skeletonFinder;
            _arenaFinder = arenaFinder;
            _logger = logger;
        }

        public async Task<IReadOnlyList<LoopTripCandidate>> GenerateCandidatesAsync(LoopIntent intent, CancellationToken ct)
        {
            // 1. GET ALL SUITABLE OFFROAD ENTRANCES
            var entrances = await _arenaFinder.FindEntrancesAsync(intent.Start, intent.PreferredLengthKm, intent.MaxDriveDistanceKm, ArenaSelectionSettings.MaxEntrances, ct);
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

            LoopPlanningDebug.WriteReport(intent, entrances.Count, allSkeletons, candidates, _logger);

            return candidates.Where(c => c is not null).Select(c => c!).ToList();
        }

        private async Task<LoopTripCandidate?> GenerateSingleCandidateAsync(LoopSkeleton skeleton, LoopIntent intent, int index, CancellationToken ct)
        {
            var points = BuildLoopPoints(skeleton);
            LoopPlanningDebug.DumpSkeleton(points, skeleton, index);
            try
            {
                var routes = await _routingProvider.GetRoutesAsync(points, intent.ToRoutingPreferences(), ct);
                var candidate = await MapToCandidateAsync(routes.First(), skeleton.Entrance);
                LoopPlanningDebug.DumpRoutedLoop(candidate, intent, skeleton.Entrance, index);
                return candidate;
            }
            catch (RoutingProviderException ex)
            {
                // One skeleton the routing provider can't route (e.g. our graph has a link the provider's
                // routing graph doesn't) must not sink the whole request - drop just this loop and keep
                // the others.
                _logger.LogWarning(ex,
                    "Loop skeleton {Index} from entrance {Lat},{Lon} ({PointCount} waypoints) failed routing: {Category}.",
                    index, skeleton.Entrance.Latitude, skeleton.Entrance.Longitude, points.Count, ex.ErrorCathegory);
                LoopPlanningDebug.DumpFailedSkeleton(points, skeleton, ex, index);
                return null;
            }
        }

        private static IReadOnlyList<Coordinate> BuildLoopPoints(LoopSkeleton skeleton)
        {
            var path = skeleton.Geometry;
            if (path.Count <= 2)
                return path;

            var total = GeoCalculator.CalculatePathDistance(path);
            var spacing = Math.Max(LoopGenerationSettings.RoutePointSpacingMeters, total / Math.Max(1, LoopGenerationSettings.MaxRoutePoints - 1));

            var points = new List<Coordinate> { path[0] };
            var accumulated = 0.0;
            for (var i = 1; i < path.Count - 1; i++)
            {
                accumulated += GeoCalculator.CalculateDistance(path[i - 1], path[i]);
                if (accumulated < spacing)
                    continue;
                points.Add(path[i]);
                accumulated = 0.0;
            }
            points.Add(path[^1]); // entrance again - closes the loop
            return points;
        }

        private async Task<IReadOnlyList<LoopSkeleton>> ProcessArenaAsync(Coordinate entrance, LoopIntent intent, CancellationToken ct)
        {
            // Fetch the nodes within the arena radius, then only the edges connecting two of those nodes
            // (guarantees a fully-resolvable graph). The radius matches the skeleton search's own
            // reachability bound (Lmax/2), so every node the search could legitimately use is fetched.
            var arenaRadiusMeters = intent.PreferredLengthKm * 1000 * LoopGenerationSettings.GoalMaxFraction / 2;
            var arenaNodes = await _nodeRepository.GetNodesNearAsync(
                entrance,
                arenaRadiusMeters,
                LoopGenerationSettings.ArenaMaxNodes,
                ct);

            if (!arenaNodes.Any())
                return Array.Empty<LoopSkeleton>();

            var arenaNodeIds = arenaNodes.Select(n => n.Id).ToList();
            var arenaEdges = await _edgeRepository.GetEdgesConnectingAsync(arenaNodeIds, ct);

            var options = new SkeletonSearchOptions(intent.AllowGates, intent.AllowPrivateRoads);
            var targetMeters = intent.PreferredLengthKm * 1000;

            // Primary start: the node nearest the requested entrance. Closure requires the loop to
            // return to the start via one of its incident edges, so on the (usually few) arenas where
            // the nearest node yields no loop, retry from the best-connected node near the entrance -
            // a higher degree gives many more exit/return frames to close through. The retry runs only
            // for otherwise-empty arenas (which are small and fast), keeping rich arenas cheap.
            var nearestStart = arenaNodes
                .OrderBy(n => GeoCalculator.CalculateDistance(n.Coordinate, entrance))
                .First();

            var skeletons = _skeletonFinder.FindSkeletons(nearestStart, arenaNodes, arenaEdges, targetMeters, options).ToList();
            if (skeletons.Count > 0)
                return skeletons;

            var degreeByNode = arenaEdges
                .SelectMany(e => new[] { e.SourceNodeId, e.TargetNodeId })
                .GroupBy(id => id)
                .ToDictionary(g => g.Key, g => g.Count());

            var wellConnectedStart = arenaNodes
                .Where(n => GeoCalculator.CalculateDistance(n.Coordinate, entrance) <= LoopGenerationSettings.StartNodeSearchRadiusMeters)
                .OrderByDescending(n => degreeByNode.GetValueOrDefault(n.Id))
                .ThenBy(n => GeoCalculator.CalculateDistance(n.Coordinate, entrance))
                .FirstOrDefault();

            if (wellConnectedStart is null || wellConnectedStart.Id == nearestStart.Id)
                return skeletons;

            return _skeletonFinder.FindSkeletons(wellConnectedStart, arenaNodes, arenaEdges, targetMeters, options).ToList();
        }

        private async Task<LoopTripCandidate> MapToCandidateAsync(ProviderRoute route, Coordinate entranceCoordinate)
        {
            var geometry = GetValidGeometry(route.Polyline);

            var maxEdgeIndex = geometry.Count - 1;

            var segments = SegmentBuilder.Build(
                geometry,
                route.RoadClassIntervals.EnsureFullCoverage(maxEdgeIndex, RoadClassType.UNKNOWN),
                route.SurfaceIntervals.EnsureFullCoverage(maxEdgeIndex, SurfaceType.UNKNOWN),
                route.TrackTypeIntervals.EnsureFullCoverage(maxEdgeIndex, TrackType.UNKNOWN));

            var barriers = BarrierBuilder.Build(route.BarrierIntervals, geometry);

            var restrictedZones = await _restrictedZoneBuilder.BuildAsync(route.RoadAccessIntervals, geometry);

            var maxGradient = GeoCalculator.CalculateMaxGradientPercentage(geometry);

            var candidate = LoopTripCandidate.Create(
                    segments, barriers, restrictedZones, route.Polyline,
                    route.Distance, route.Duration, route.Ascend, route.Descend, maxGradient,
                    hookCoordinate: default,
                    hookPolylineIndex: default,
                    estimatedTransitDistanceMeters: default,
                    entranceCoordinate: entranceCoordinate);

            return candidate;
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
