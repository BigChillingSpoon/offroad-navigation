using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Goals
{
    public class LoopGoal : ITripGoal<LoopIntent>
    {
        public bool IsSatisfied(TripCandidate candidate, LoopIntent intent)
        {
            return true;
        }

        public double GetGoalScore(TripCandidate candidate, LoopIntent intent)
        {
            return 0d;
        }
    }
}
