using Routing.Application.Abstractions;
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Planner;
using Routing.Application.Planning.Profiles;
using Routing.Application.Planning.State;
using Routing.Domain.ValueObjects;
using Routing.Application.Planning.Exceptions;
using Routing.Application.Planning.Encoding;

namespace Routing.Application.Planning.Candidates.Generators
{
    public sealed class RouteCandidateGenerator : ICandidateGenerator<RouteIntent>
    {
        private readonly IRoutingProvider _routingProvider;
        public RouteCandidateGenerator(IRoutingProvider routingProvider)
        {
            _routingProvider = routingProvider;
        }

        public async Task<IReadOnlyList<TripCandidate>> GenerateCandidatesAsync(PlannerState state, RouteIntent intent, UserRoutingProfile profile, PlannerSettings settings, CancellationToken ct)
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

            var segment = Segment.Create(
                geometry.First(),
                geometry.Last(),
                route.Distance,
                route.Duration,
                0, // MVP placeholder - todo calculate actual offroad distance
                route.Ascend,
                geometry
                );

            return new[]
            {
                new TripCandidate
                {
                    Segments = new[] { segment }
                }
            };
        }

        //translate technical exception to domain specific one, so we can later on decide how to handle it based on category
        private IReadOnlyList<Coordinate> GetValidGeometry(EncodedPolyline polyline)
        {
            try
            {
                var decoded = PolylineDecoder.Decode(polyline.Points, polyline.PolylineEncodedMultiplier);
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
