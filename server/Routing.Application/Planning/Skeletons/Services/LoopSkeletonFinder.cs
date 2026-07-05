using Routing.Application.Planning.Skeletons.Helpers;
using Routing.Application.Planning.Skeletons.Models;
using Routing.Application.Planning.Skeletons.Services;
using Routing.Domain.Models;
using Routing.Domain.Utilities;
using Routing.Domain.ValueObjects;

public class LoopSkeletonFinder : ILoopSkeletonFinder
{
    private const int MinJumpMeters = 400;
    private const int MaxJumpMeters = 3000;//this limits the search 
    private const int MaxNodes = 8;

    // Maximální povolená "ostrost" zatáčky v lese (120 stupňů znamená, že nepojedeš ostře zpět)
    private const double MaxTurnAngleDegrees = 120.0;

    public IReadOnlyList<LoopSkeleton> FindSkeletons(
        Hookpoint startHookpoint,
        IReadOnlyList<Hookpoint> allHookpointsInArea,
        double targetLoopDistanceMeters)
    {
        var rawSkeletons = new List<List<SkeletonStep>>();

        // 1. Založíme cestu s prvním (nultým) krokem.
        // Start nemá předchozí vzdálenost ani úhel, tak dáme nuly.
        var initialStep = new SkeletonStep(startHookpoint, 0, 0);
        var currentRawSkeleton = new List<SkeletonStep> { initialStep };

        // 2. DFS Hledání
        FindSkeletonsRecursive(
            currentPath: currentRawSkeleton,
            remainingHookpoints: allHookpointsInArea,
            targetDistance: targetLoopDistanceMeters,
            accumulatedDistance: 0,
            completedSkeletons: rawSkeletons);

        // 3. Ořez a RAM Skórování (Prostorová hustota!)
        var bestSkeletons = rawSkeletons
            .Select(path => new
            {
                Path = path,
                Score = CalculateRamScore(path, allHookpointsInArea)
            })
            .OrderByDescending(x => x.Score)
            .Select(ToLoopSkeleton)
            .ToList();

        return bestSkeletons;
    }

    private void FindSkeletonsRecursive(
        List<SkeletonStep> currentPath,
        IReadOnlyList<Hookpoint> remainingHookpoints,
        double targetDistance,
        double accumulatedDistance,
        List<List<SkeletonStep>> completedSkeletons)
    {
        var currentStep = currentPath[^1]; // Moderní C# (vezme poslední krok)
        var startNode = currentPath[0].DestinationNode;

        // --- KONTROLA UZAVŘENÍ OKRUHU ---
        if (currentPath.Count >= 3)
        {
            var distanceHome = GeoCalculator.CalculateDistance(currentStep.DestinationNode.Location, startNode.Location);
            var totalEstimatedDistance = accumulatedDistance + distanceHome;

            if (totalEstimatedDistance >= targetDistance * 0.85 && totalEstimatedDistance <= targetDistance * 1.15)
            {
                // Extra kontrola úhlu pro návrat domů (aby cíl nebyl V-vracák)
                var bearingHome = GeoCalculator.CalculateBearing(currentStep.DestinationNode.Location, startNode.Location);
                var closingTurnAngle = GeoCalculator.GetAngleDifference(currentStep.HeadingAngleFromPreviousStep, bearingHome);

                if (closingTurnAngle <= MaxTurnAngleDegrees)
                {
                    completedSkeletons.Add(new List<SkeletonStep>(currentPath));
                    return;
                }
            }

            if (totalEstimatedDistance > targetDistance * 1.15) return; // Hard-cut
        }

        if (currentPath.Count >= MaxNodes) return;

        // --- SKOKY NA SOUSEDY ---
        foreach (var neighbor in remainingHookpoints)
        {
            // Zabráníme smyčkám
            if (currentPath.Any(step => step.DestinationNode.Equals(neighbor))) continue;

            // 1. Fyzika: Vzdálenost
            var jumpDistance = GeoCalculator.CalculateDistance(currentStep.DestinationNode.Location, neighbor.Location);
            if (jumpDistance < MinJumpMeters || jumpDistance > MaxJumpMeters) continue;

            // 2. Geometrie: Úhel a zatáčení
            var bearingToNeighbor = GeoCalculator.CalculateBearing(currentStep.DestinationNode.Location, neighbor.Location);

            // Pokud to není první skok ze startu, zkontrolujeme úhel zlomu
            if (currentPath.Count > 1)
            {
                var turnAngle = GeoCalculator.GetAngleDifference(currentStep.HeadingAngleFromPreviousStep, bearingToNeighbor);

                // ZABIJÁK V-VRACÁKŮ:
                if (turnAngle > MaxTurnAngleDegrees) continue;
            }

            // ACTION: Jdeme do větve
            var nextStep = new SkeletonStep(neighbor, jumpDistance, bearingToNeighbor);
            currentPath.Add(nextStep);

            FindSkeletonsRecursive(
                currentPath: currentPath,
                remainingHookpoints: remainingHookpoints,
                targetDistance: targetDistance,
                accumulatedDistance: accumulatedDistance + jumpDistance,
                completedSkeletons: completedSkeletons);

            // UNDO: Backtracking
            currentPath.RemoveAt(currentPath.Count - 1);
        }
    }

    private double CalculateRamScore(List<SkeletonStep> path, IReadOnlyList<Hookpoint> allPoints)
    {
        double score = 0;
        var startNode = path[0].DestinationNode;

        // Projdeme všechny kroky (od indexu 1, protože index 0 je start s nulovými hodnotami)
        for (int i = 1; i < path.Count; i++)
        {
            var nodeFrom = path[i - 1].DestinationNode;
            var nodeTo = path[i].DestinationNode;

            score += nodeTo.Passages * 0.5;
            score += nodeTo.GradesMask * 1.2;

            // BUBBLE HEURISTIC (Prostorová hustota kolem úsečky)
            score += ScoreCorridorDensity(nodeFrom.Location, nodeTo.Location, path, allPoints);
        }

        // Musíme ohodnotit i poslední uzavírací úsečku (Z posledního bodu zpět na start!)
        var lastNode = path[^1].DestinationNode;
        score += ScoreCorridorDensity(lastNode.Location, startNode.Location, path, allPoints);

        return score;
    }

    private int ScoreCorridorDensity(Coordinate from, Coordinate to, List<SkeletonStep> currentPath, IReadOnlyList<Hookpoint> allPoints)
    {
        int dotsInEllipse = 0;
        foreach (var candidate in allPoints)
        {
            if (currentPath.Any(step => step.DestinationNode.Equals(candidate))) continue;

            //we have to calculate minor axis based on length of the main axis (distance from "from" to "to") to create a more elongated ellipse for longer segments
            if (GeoCalculator.IsPointInEllipse(candidate.Location, from, to, minorAxisRadiusMeters: 250.0))
            {
                dotsInEllipse++;
            }
        }
        return dotsInEllipse * 50;
    }

    //todo move to some mapper shit
    private LoopSkeleton ToLoopSkeleton(dynamic scoredResult)
    {
        List<SkeletonStep> steps = scoredResult.Path;
        var start = steps[0].DestinationNode.Location;

        // Cestovní body jsou všechno kromě startu
        var waypoints = steps.Skip(1).Select(s => s.DestinationNode.Location).ToList();

        // Sečteme vzdálenosti všech kroků + vzdálenost domů
        var distanceHome = GeoCalculator.CalculateDistance(steps[^1].DestinationNode.Location, start);
        var totalMeters = steps.Sum(s => s.EuclideanDistanceFromPreviousStep) + distanceHome;

        return new LoopSkeleton(
            Entrance: start,
            Waypoints: waypoints,
            EucledianDistanceKm: (float)(totalMeters / 1000.0)
        );
    }
}