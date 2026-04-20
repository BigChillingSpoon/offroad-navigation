using Routing.Application.Planning.Intents;
using System;

namespace Routing.Application.Planning.Pipelines
{
    public interface IPlanningPipelineFactory
    {
        IPlanningPipeline<TIntent> Create<TIntent>() where TIntent : ITripIntent;
    }

    public sealed class PlanningPipelineFactory : IPlanningPipelineFactory
    {
        private readonly IServiceProvider _provider;

        public PlanningPipelineFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public IPlanningPipeline<TIntent> Create<TIntent>() where TIntent : ITripIntent
        {
            var pipeline = _provider.GetService(typeof(IPlanningPipeline<TIntent>)) as IPlanningPipeline<TIntent>;

            if (pipeline == null)
            {
                throw new InvalidOperationException(
                    $"Configuration Error: No planning pipeline registered for intent '{typeof(TIntent).Name}'. " +
                    $"Make sure to register it in DI (e.g., services.AddScoped<IPlanningPipeline<{typeof(TIntent).Name}>, ...>)");
            }

            return pipeline;
        }
    }
}