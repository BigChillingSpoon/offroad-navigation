using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Pipelines;
using Routing.Application.Planning.Profiles;
using Routing.Domain.Models;

namespace Routing.Application.Planning.Planner
{
    public sealed class TripPlanner : ITripPlanner
    {
        private readonly IPlanningPipelineFactory _pipelineFactory;

        public TripPlanner(IPlanningPipelineFactory pipelineFactory)
        {
            _pipelineFactory = pipelineFactory;
        }

        public Task<IReadOnlyList<TripPlan>> PlanAsync<TIntent>(TIntent intent, UserRoutingProfile profile, PlannerSettings settings, CancellationToken ct)
            where TIntent : ITripIntent
        {
            var pipeline = _pipelineFactory.Create<TIntent>();
            return pipeline.PlanAsync(intent, profile, settings, ct);
        }
    }
}