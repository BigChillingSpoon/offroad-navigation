using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Routing.Application.Abstractions;
using Routing.Application.Contracts; // Pro IGisService
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Planner;

namespace Routing.Application.Planning.Candidates.Generators
{
    public sealed class LoopCandidateGenerator : ICandidateGenerator<LoopIntent, LoopTripCandidate>
    {
        private readonly IGisService _gisService;
        private readonly IRoutingProvider _routingProvider;
        private readonly ICandidateFactory _candidateFactory;

        public LoopCandidateGenerator(IGisService gisService, IRoutingProvider routingProvider, ICandidateFactory candidateFactory)
        {
            _gisService = gisService;
            _routingProvider = routingProvider;
            _candidateFactory = candidateFactory;
        }

        public async Task<IReadOnlyList<LoopTripCandidate>> GenerateCandidatesAsync(LoopIntent intent, CancellationToken ct)
        {
            throw new NotImplementedException(); 
        }
    }
}