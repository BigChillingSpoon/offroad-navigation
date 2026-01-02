using Routing.Application.Planning.Intents;
using Routing.Application.Planning.State;
using Routing.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Goals
{
    public class RouteGoal : IPlanningGoal<RouteIntent>
    {
        public bool IsSatisfied(PlannerState state, RouteIntent intent)
        {
            return false;
        }

        public double GetGoalScore(PlannerState state, RouteIntent intent)
        {
            return 0d;
        }
    }
}
