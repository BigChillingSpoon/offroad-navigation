using Routing.Application.Abstractions;
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Planner;

namespace Routing.Application.Planning.Candidates.Generators
{
    public sealed class RouteCandidateGenerator : ICandidateGenerator<RouteIntent, TripCandidate>
    {
        private readonly IRoutingProvider _routingProvider;
        private readonly ICandidateFactory _candidateFactory;

        public RouteCandidateGenerator(IRoutingProvider routingProvider, ICandidateFactory candidateFactory)
        {
            _routingProvider = routingProvider;
            _candidateFactory = candidateFactory;
        }

        public async Task<IReadOnlyList<TripCandidate>> GenerateCandidatesAsync(RouteIntent intent, PlannerSettings settings, CancellationToken ct)
        {
            var routes = await _routingProvider.GetRoutesAsync(intent, ct);

            if (routes is null || !routes.Any())
                return Array.Empty<TripCandidate>();

            var candidateTasks = routes.Select(route => _candidateFactory.CreateRouteCandidateAsync(route));

            var candidates = await Task.WhenAll(candidateTasks);

            return candidates.ToList();
        }
    }
}