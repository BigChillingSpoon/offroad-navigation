using Routing.Application.Abstractions;
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Planner;
using Routing.Application.Planning.Profiles;
using Routing.Application.Planning.State;
using Routing.Domain.ValueObjects;
using System.Text.Json;
using Routing.Application.Planning.Exceptions;
using Routing.Application.Planning.Encoding;

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
            if (best is null)//may be logged as warning
                return Array.Empty<TripCandidate>();

            var geometry = ProvideGeometryFrom(best.Points);
           
            var segment = Segment.Create(
                geometry.FirstOrDefault() ?? throw new RoutingProviderException(RoutingProviderErrorCategory.InvalidResponse, "Routing engine returned invalid geometry. Geometry contains no elements."),
                geometry.LastOrDefault() ?? throw new RoutingProviderException(RoutingProviderErrorCategory.InvalidResponse, "Routing engine returned invalid geometry. Geometry contains no elements. "),
                best.Distance,
                best.TimeMs / 1000d,
                0, // MVP placeholder - todo calculate actual offroad distance
                best.Ascend,
                geometry
                );
           
            var candidate = new TripCandidate
            {
                Segments = new[] { segment },
            };

            return new[] { candidate };
        }

        private IReadOnlyList<Coordinate> ProvideGeometryFrom(string encodedPolyline)
        {
            try
            {
                return PolylineDecoder.Decode(encodedPolyline);
            }
            catch (InvalidPolylineException ex)
            {
                throw new RoutingProviderException(RoutingProviderErrorCategory.InvalidResponse, ex.Message);
            }
        }
    }
}
