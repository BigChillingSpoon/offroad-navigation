using Routing.Application.Planning.Intents;
using Routing.Application.Planning.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Goals
{
    public class LoopGoal : IPlanningGoal<LoopIntent>
    {
        public bool IsSatisfied(PlannerState state, LoopIntent intent)
        {
            return false;
        }

        public double GetGoalScore(PlannerState state, LoopIntent intent)
        {
            return 0d;
        }
    }
}
