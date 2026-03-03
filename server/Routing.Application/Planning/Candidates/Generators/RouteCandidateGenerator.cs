using Routing.Application.Abstractions;
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Planner;
using Routing.Domain.ValueObjects;
using Routing.Application.Planning.Exceptions;
using Routing.Application.Planning.Encoding;
using Routing.Application.Planning.Candidates.Builders;
using Routing.Application.Planning.Extensions;
using Routing.Application.Planning.DEBUG;

namespace Routing.Application.Planning.Candidates.Generators
{
    public sealed class RouteCandidateGenerator : ICandidateGenerator<RouteIntent>
    {
        private readonly IRoutingProvider _routingProvider;

        public RouteCandidateGenerator(IRoutingProvider routingProvider)
        {
            _routingProvider = routingProvider;
        }

        public async Task<IReadOnlyList<TripCandidate>> GenerateCandidatesAsync(RouteIntent intent, PlannerSettings settings, CancellationToken ct)
        {
            var routes = await _routingProvider.GetRoutesAsync(intent, ct);

            if (routes is null || !routes.Any())
                return Array.Empty<TripCandidate>();

            return routes
                .Select(MapToCandidate)
                .ToList();
        }

        private TripCandidate MapToCandidate(ProviderRoute route, int index)
        {
            var geometry = GetValidGeometry(route.Polyline);

            //index is only for debug, to be removed 
            PlanningDebugExtensions.LogToGPX(geometry, $"C:\\tmp\\debug_route_candidate_{index}.gpx");

            var maxEdgeIndex = geometry.Count - 1;

            var segments = SegmentBuilder.Build(
                geometry,
                route.RoadClassIntervals.EnsureFullCoverage(maxEdgeIndex),
                route.SurfaceIntervals.EnsureFullCoverage(maxEdgeIndex));

            return TripCandidate.Create(
                segments,
                route.Distance,
                route.Duration,
                route.Ascend,
                route.Descend);
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
