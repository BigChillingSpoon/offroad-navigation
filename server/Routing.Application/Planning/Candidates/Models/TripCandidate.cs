using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Candidates.Models
{
    public sealed record TripCandidate
    {
        public required IReadOnlyList<TripPlanChunk> PlanChunks { get; init; }
        public IReadOnlyDictionary<string, object>? Metadata { get; init; }
    }
}
