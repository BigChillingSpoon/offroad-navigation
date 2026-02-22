using Routing.Application.Abstractions;
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Planner;
using Routing.Application.Planning.Profiles;
using Routing.Domain.ValueObjects;
using Routing.Application.Planning.Exceptions;
using Routing.Application.Planning.Encoding;
using Routing.Application.Planning.Candidates.Builders;
using Routing.Application.Planning.Extensions;

namespace Routing.Application.Planning.Candidates.Generators
{
    public sealed class RouteCandidateGenerator : ICandidateGenerator<RouteIntent>
    {
        private readonly IRoutingProvider _routingProvider;
        public RouteCandidateGenerator(IRoutingProvider routingProvider)
        {
            _routingProvider = routingProvider;
        }

        public async Task<IReadOnlyList<TripCandidate>> GenerateCandidatesAsync(RouteIntent intent, UserRoutingProfile profile, PlannerSettings settings, CancellationToken ct)
        {
            var profileName = profile.ToGraphhoperProfile();

            var route = await _routingProvider.GetRouteAsync(
                             intent.Start.Latitude,
                             intent.Start.Longitude,
                             intent.End.Latitude,
                             intent.End.Longitude,
                             profileName,
                             ct);

            if (route is null)//may be logged as warning - we do not handle this as ERROR
                return Array.Empty<TripCandidate>();

            var geometry = GetValidGeometry(route.Polyline);

            var maxEdgeIndex = geometry.Count - 1;
            var normalizedSurfaces = route.SurfaceIntervals.EnsureFullCoverage(maxEdgeIndex);
            var normalizedRoadClasses = route.RoadClassIntervals.EnsureFullCoverage(maxEdgeIndex);

            var segments = SegmentBuilder.Build(geometry, normalizedRoadClasses, normalizedSurfaces);

            return new[]
            {
                TripCandidate.Create(segments, route.Distance, route.Duration, 0, route.Ascend)
            };
        }

        //translate technical exception to domain specific one, so we can later on decide how to handle it based on category
        private IReadOnlyList<Coordinate> GetValidGeometry(EncodedPolyline polyline)
        {
            try
            {
                var decoded = PolylineDecoder.Decode(polyline.Points, polyline.PolylineEncodedMultiplier, polyline.Dimension);
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
