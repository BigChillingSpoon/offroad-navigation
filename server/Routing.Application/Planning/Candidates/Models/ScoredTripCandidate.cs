using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Candidates.Models
{
    public sealed record ScoredTripCandidate
    {
        public required TripCandidate Candidate { get; init; }
        public required double Score { get; init; }
    }
}
