using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Planner
{
    //konstanty pro planner, nemmene ale udavajici smer planeru jako takoveho
    public sealed class PlannerConfig
    {
        public int MaxIterations => 100;
        public int DefaultStepDistanceMeters => 100;
    }
}
