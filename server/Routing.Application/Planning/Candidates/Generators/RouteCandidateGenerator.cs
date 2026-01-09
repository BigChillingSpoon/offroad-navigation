using Routing.Application.Abstractions;
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Planner;
using Routing.Application.Planning.Profiles;
using Routing.Application.Planning.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Candidates.Generators
{
    public sealed class RouteCandidateGenerator : ICandidateGenerator<RouteIntent>
    {
        private readonly IGraphHopperService _graphHopper;
        public RouteCandidateGenerator(IGraphHopperService graphHopper)
        {
            _graphHopper = graphHopper;
        }

        public async Task<IReadOnlyList<TripCandidate>> GenerateCandidatesAsync(PlannerState state, RouteIntent intent, UserRoutingProfile profile, PlannerSettings settings, CancellationToken ct)
        {
            var profileName = profile.ToGraphhoperProfile();

            var json = await _graphHopper.GetRouteJsonAsync(
                 intent.Start.Latitude,
                 intent.Start.Longitude,
                 intent.End.Latitude,
                 intent.End.Longitude,
                 profileName,
                 ct);
            var response = JsonSerializer.Deserialize<GraphHopperRouteResponse>(json);

            var best = response?.Paths?.FirstOrDefault();
            if (best is null)
                return Array.Empty<TripCandidate>();

            var geometry = PolylineDecoder.Decode(best.Points);
            var chunk = new TripPlanChunk
            {
                Geometry = geometry,
                DistanceMeters = best.Distance,
                DurationSeconds = best.TimeMs / 1000d,
                OffroadDistanceMeters = 0,     // MVP placeholder - todo calculate actual offroad distance
                ElevationGainMeters = best.Ascend
            };

            var candidate = new TripCandidate
            {
                PlanChunks = new[] { chunk },
            };

            return new[] { candidate };
        }
    }
}
