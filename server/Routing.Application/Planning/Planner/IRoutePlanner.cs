using Routing.Application.Planning.Goals;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Models;
using Routing.Application.Planning.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Planner
{
    public interface IRoutePlanner
    {
        //in future add some result to be returned
        public Task PlanAsync<TIntent>(TIntent intent, IPlanningGoal<TIntent> goal, UserRoutingProfile profile, PlannerConfig config, CancellationToken ct) where TIntent : IRoutingIntent;
    }
}
