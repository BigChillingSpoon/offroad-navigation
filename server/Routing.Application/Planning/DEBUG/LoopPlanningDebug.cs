using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Encoding;
using Routing.Application.Planning.Exceptions;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Skeletons.Models;
using Routing.Domain.ValueObjects;

namespace Routing.Application.Planning.DEBUG
{
    /// <summary>
    /// DEBUG-only diagnostics for loop planning: dumps each skeleton, routed loop and routing failure to
    /// a self-describing GPX (for QGIS), and writes a per-request JSON report comparing skeleton length to
    /// routed length so it is clear why N skeletons yielded only M returned loops. Every method is
    /// <see cref="ConditionalAttribute"/>("DEBUG") and stripped from Release; the generator just calls them.
    /// </summary>
    internal static class LoopPlanningDebug
    {
        [Conditional("DEBUG")]
        public static void DumpSkeleton(IReadOnlyList<Coordinate> points, LoopSkeleton skeleton, int index) =>
            points.LogToGPX(
                $"./debug_skeleton_{index}_{skeleton.TraveledDistanceKm:F1}km.gpx",
                name: $"skeleton #{index} - {skeleton.TraveledDistanceKm:F2}km ({points.Count} waypoints)",
                description: $"entrance {skeleton.Entrance.Latitude:F5},{skeleton.Entrance.Longitude:F5}");

        [Conditional("DEBUG")]
        public static void DumpFailedSkeleton(IReadOnlyList<Coordinate> points, LoopSkeleton skeleton, RoutingProviderException ex, int index) =>
            points.LogToGPX(
                $"./debug_failed_skeleton_{index}_{skeleton.TraveledDistanceKm:F1}km_ROUTING-FAILED.gpx",
                name: $"skeleton #{index} FAILED routing ({skeleton.TraveledDistanceKm:F2}km) - {ex.ErrorCathegory}",
                description: $"entrance {skeleton.Entrance.Latitude:F5},{skeleton.Entrance.Longitude:F5}; {points.Count} waypoints; {ex.Message}");

        [Conditional("DEBUG")]
        public static void DumpRoutedLoop(LoopTripCandidate candidate, LoopIntent intent, Coordinate entrance, int index)
        {
            IReadOnlyList<Coordinate> geometry;
            try { geometry = PolylineDecoder.Decode(candidate.Polyline); }
            catch (InvalidPolylineException) { return; }

            var (minMeters, maxMeters) = GoalBand(intent.PreferredLengthKm);
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

        /// <summary>
        /// One JSON analysis report per request: request params, goal band and, per skeleton (paired with
        /// its candidate by index), skeleton length vs routed length vs verdict, plus summary counts. A null
        /// candidate means routing failed for that skeleton.
        /// </summary>
        [Conditional("DEBUG")]
        public static void WriteReport(
            LoopIntent intent,
            int entrancesSelected,
            IReadOnlyList<LoopSkeleton> skeletons,
            IReadOnlyList<LoopTripCandidate?> candidates,
            ILogger logger)
        {
            var (minMeters, maxMeters) = GoalBand(intent.PreferredLengthKm);

            var rows = skeletons.Select((skeleton, index) =>
            {
                var candidate = index < candidates.Count ? candidates[index] : null;
                var verdict = candidate is null
                    ? "ROUTING-FAILED"
                    : candidate.TotalDistanceMeters >= minMeters && candidate.TotalDistanceMeters <= maxMeters ? "INBAND" : "OUTOFBAND";
                return new
                {
                    Index = index,
                    entrance = new[] { skeleton.Entrance.Latitude, skeleton.Entrance.Longitude },
                    SkeletonKm = (double)skeleton.TraveledDistanceKm,
                    RoutedKm = candidate is null ? (double?)null : candidate.TotalDistanceMeters / 1000.0,
                    OffroadPct = candidate is null ? (int?)null : (int)Math.Round(candidate.OffroadRatio * 100),
                    AscendM = candidate is null ? (double?)null : candidate.ElevationGainMeters,
                    Verdict = verdict
                };
            }).ToList();

            var report = new
            {
                timestamp = DateTime.Now.ToString("o"),
                request = new { intent.Start.Latitude, intent.Start.Longitude, intent.PreferredLengthKm, intent.MaxDriveDistanceKm, intent.AllowPrivateRoads, intent.AllowGates },
                goalBandMeters = new { min = minMeters, max = maxMeters },
                entrancesSelected,
                summary = new
                {
                    skeletonsCreated = rows.Count,
                    routingFailed = rows.Count(r => r.Verdict == "ROUTING-FAILED"),
                    returnedInBand = rows.Count(r => r.Verdict == "INBAND"),
                    discardedOutOfBand = rows.Count(r => r.Verdict == "OUTOFBAND")
                },
                candidates = rows
            };

            var file = $"./debug_loop_report_{DateTime.Now:yyyyMMdd_HHmmss_fff}.json";
            File.WriteAllText(file, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            logger.LogInformation(
                "Loop report {File}: target {TargetKm}km, {Entrances} entrances, {Created} skeletons -> {InBand} returned (in-band), {Out} discarded (out-of-band), {Failed} routing-failed.",
                file, intent.PreferredLengthKm, entrancesSelected, report.summary.skeletonsCreated,
                report.summary.returnedInBand, report.summary.discardedOutOfBand, report.summary.routingFailed);
        }

        private static (double minMeters, double maxMeters) GoalBand(double preferredLengthKm)
        {
            var target = preferredLengthKm * 1000;
            return (target * LoopGenerationSettings.GoalMinFraction, target * LoopGenerationSettings.GoalMaxFraction);
        }
    }
}
