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
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Routing.Application.Abstractions.Persistence;
using Routing.Application.Planning.Skeletons.Services;
using Routing.Application.Planning.Skeletons.Models;

namespace Routing.Application.Planning.Candidates.Generators
{
    public sealed class LoopCandidateGenerator : ICandidateGenerator<LoopIntent, LoopTripCandidate>
    {
        // How close to the requested entrance the (well-connected) start node must be. Keeps the loop
        // anchored near the chosen arena while allowing us to pick a better-connected junction than the
        // single nearest node.
        private const double StartNodeSearchRadiusMeters = 300.0;

        // Cap on the number of (nearest) nodes fetched per arena. A round loop stays near its start, so
        // for big loops the Lmax/2 fetch radius would otherwise pull thousands of far nodes that only
        // bloat and slow the skeleton search. Bounds the graph the finder works on, keeping big-loop
        // searches fast; small arenas contain fewer nodes than this anyway.
        private const int ArenaMaxNodes = 700;

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
            var attemptTasks = allSkeletons.Select((skeleton, index) => GenerateSingleCandidateAsync(skeleton, intent, index, ct));
            var attempts = await Task.WhenAll(attemptTasks);

            // Consolidated per-request analysis dump (skeleton length vs GH-routed length vs goal band
            // for every candidate, plus summary counts) - so it's clear why N created but only M returned.
            WriteLoopReport(intent, entrances.Count, attempts.Select(a => a.Debug).ToList());

            return attempts.Where(a => a.Candidate is not null).Select(a => a.Candidate!).ToList();
        }

        private async Task<CandidateAttempt> GenerateSingleCandidateAsync(LoopSkeleton skeleton, LoopIntent intent, int index, CancellationToken ct)
        {
            var points = BuildLoopPoints(skeleton);
            try
            {
                var routes = await _routingProvider.GetRoutesAsync(points, intent.ToRoutingPreferences(), ct);
                var candidate = await MapToCandidateAsync(routes.First(), intent, skeleton.Entrance, index);
                var debug = new LoopCandidateDebug(
                    index, skeleton.Entrance.Latitude, skeleton.Entrance.Longitude,
                    skeleton.TraveledDistanceKm,
                    candidate.TotalDistanceMeters / 1000.0,
                    (int)Math.Round(candidate.OffroadRatio * 100),
                    candidate.ElevationGainMeters,
                    Verdict(candidate.TotalDistanceMeters, intent));
                return new CandidateAttempt(candidate, debug);
            }
            catch (RoutingProviderException ex)
            {
                // One skeleton that GraphHopper can't stitch (e.g. our graph has an offroad link GH's
                // routing graph doesn't) must not sink the whole request - drop just this loop and keep
                // the others. Log the exact points and dump a GPX so the failing route can be replayed.
                _logger.LogWarning(ex,
                    "Loop skeleton {Index} from entrance {Lat},{Lon} ({PointCount} waypoints) failed routing: {Category}. Points: {Points}",
                    index, skeleton.Entrance.Latitude, skeleton.Entrance.Longitude, points.Count, ex.ErrorCathegory,
                    string.Join(" | ", points.Select(p => $"{p.Latitude:F6},{p.Longitude:F6}")));
                points.LogToGPX(
                    $"./debug_failed_skeleton_{index}_{skeleton.TraveledDistanceKm:F1}km_ROUTING-FAILED.gpx",
                    name: $"skeleton #{index} FAILED routing ({skeleton.TraveledDistanceKm:F2}km) - {ex.ErrorCathegory}",
                    description: $"entrance {skeleton.Entrance.Latitude:F5},{skeleton.Entrance.Longitude:F5}; {points.Count} waypoints; {ex.Message}");
                var debug = new LoopCandidateDebug(
                    index, skeleton.Entrance.Latitude, skeleton.Entrance.Longitude,
                    skeleton.TraveledDistanceKm, RoutedKm: null, OffroadPct: null, AscendM: null, Verdict: "ROUTING-FAILED");
                return new CandidateAttempt(null, debug);
            }
        }

        // Predicts LoopGoal's distance-band decision (the dominant reason candidates get discarded).
        private static string Verdict(double routedMeters, LoopIntent intent)
        {
            var target = intent.PreferredLengthKm * 1000;
            var min = target * LoopDistanceTolerance.MinFraction;
            var max = target * LoopDistanceTolerance.MaxFraction;
            return routedMeters >= min && routedMeters <= max ? "INBAND" : "OUTOFBAND";
        }

        private sealed record LoopCandidateDebug(
            int Index, double EntranceLat, double EntranceLon, double SkeletonKm,
            double? RoutedKm, int? OffroadPct, double? AscendM, string Verdict);

        private sealed record CandidateAttempt(LoopTripCandidate? Candidate, LoopCandidateDebug Debug);

        // Writes one JSON analysis report per request: request params, goal band, and every candidate's
        // skeleton length vs GraphHopper-routed length vs verdict, plus summary counts (created / in-band
        // / out-of-band / routing-failed). DEBUG-only, stripped from Release.
        [System.Diagnostics.Conditional("DEBUG")]
        private void WriteLoopReport(LoopIntent intent, int entrancesSelected, IReadOnlyList<LoopCandidateDebug> debugs)
        {
            var target = intent.PreferredLengthKm * 1000;
            var report = new
            {
                timestamp = DateTime.Now.ToString("o"),
                request = new
                {
                    intent.Start.Latitude,
                    intent.Start.Longitude,
                    intent.PreferredLengthKm,
                    intent.MaxDriveDistanceKm,
                    intent.AllowPrivateRoads,
                    intent.AllowGates
                },
                goalBandMeters = new { min = target * LoopDistanceTolerance.MinFraction, max = target * LoopDistanceTolerance.MaxFraction },
                entrancesSelected,
                summary = new
                {
                    skeletonsCreated = debugs.Count,
                    routedOk = debugs.Count(d => d.RoutedKm.HasValue),
                    routingFailed = debugs.Count(d => d.Verdict == "ROUTING-FAILED"),
                    returnedInBand = debugs.Count(d => d.Verdict == "INBAND"),
                    discardedOutOfBand = debugs.Count(d => d.Verdict == "OUTOFBAND")
                },
                candidates = debugs.OrderBy(d => d.Index).Select(d => new
                {
                    d.Index,
                    entrance = new[] { d.EntranceLat, d.EntranceLon },
                    d.SkeletonKm,
                    d.RoutedKm,
                    d.OffroadPct,
                    d.AscendM,
                    d.Verdict
                })
            };

            var file = $"./debug_loop_report_{DateTime.Now:yyyyMMdd_HHmmss_fff}.json";
            File.WriteAllText(file, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            _logger.LogInformation(
                "Loop report {File}: target {TargetKm}km, {Entrances} entrances, {Created} skeletons -> {InBand} returned (in-band), {Out} discarded (out-of-band), {Failed} routing-failed.",
                file, intent.PreferredLengthKm, entrancesSelected, report.summary.skeletonsCreated,
                report.summary.returnedInBand, report.summary.discardedOutOfBand, report.summary.routingFailed);
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
            // edges that connect two of those nodes (guarantees a fully-resolvable graph). The radius
            // matches the skeleton search's own reachability bound (Holy Circle H4 = Lmax/2), so every
            // node the search could legitimately use is fetched - a plain L/2 left out the outer band
            // and starved short-loop arenas of graph.
            var arenaRadiusMeters = intent.PreferredLengthKm * 1000 * LoopDistanceTolerance.MaxFraction / 2;
            var arenaNodes = await _nodeRepository.GetNodesNearAsync(
                entrance,
                arenaRadiusMeters,
                ArenaMaxNodes,
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
            int Degree(long id) => degreeByNode.TryGetValue(id, out var d) ? d : 0;

            var wellConnectedStart = arenaNodes
                .Where(n => GeoCalculator.CalculateDistance(n.Coordinate, entrance) <= StartNodeSearchRadiusMeters)
                .OrderByDescending(n => Degree(n.Id))
                .ThenBy(n => GeoCalculator.CalculateDistance(n.Coordinate, entrance))
                .FirstOrDefault();

            if (wellConnectedStart is null || wellConnectedStart.Id == nearestStart.Id)
                return skeletons;

            return _skeletonFinder.FindSkeletons(wellConnectedStart, arenaNodes, arenaEdges, targetMeters, options).ToList();
        }

        private async Task<LoopTripCandidate> MapToCandidateAsync(ProviderRoute route, LoopIntent intent, Coordinate entranceCoordinate, int index)
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

            WriteCandidateDebugGpx(geometry, candidate, intent, entranceCoordinate, index);

            return candidate;
        }

        // Dumps the routed loop to a GPX whose name/filename say which loop it is and whether it will
        // survive the goal - most candidates get discarded there for landing outside the distance band,
        // and a bare "debug_loop_candidate_N.gpx" made them impossible to tell apart in QGIS.
        [System.Diagnostics.Conditional("DEBUG")]
        private static void WriteCandidateDebugGpx(IReadOnlyList<Coordinate> geometry, LoopTripCandidate candidate, LoopIntent intent, Coordinate entrance, int index)
        {
            var targetMeters = intent.PreferredLengthKm * 1000;
            var minMeters = targetMeters * LoopDistanceTolerance.MinFraction;
            var maxMeters = targetMeters * LoopDistanceTolerance.MaxFraction;
            var inBand = candidate.TotalDistanceMeters >= minMeters && candidate.TotalDistanceMeters <= maxMeters;
            var verdict = inBand ? "INBAND" : "OUTOFBAND"; // predicts the LoopGoal distance check
            var offroadPct = (int)Math.Round(candidate.OffroadRatio * 100);
            var km = candidate.TotalDistanceMeters / 1000.0;

            var name = $"loop #{index} - {km:F2}km (target {intent.PreferredLengthKm:F1}km) - offroad {offroadPct}% - {verdict}";
            var description =
                $"entrance {entrance.Latitude:F5},{entrance.Longitude:F5}; " +
                $"routed {candidate.TotalDistanceMeters:F0}m; goal band {minMeters:F0}-{maxMeters:F0}m; " +
                $"ascend {candidate.ElevationGainMeters:F0}m; verdict {verdict} " +
                (inBand ? "(kept)" : "(discarded by goal: distance out of band)");

            geometry.LogToGPX($"./debug_loop_candidate_{index}_{km:F1}km_offroad{offroadPct}_{verdict}.gpx", name, description);
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
