using Routing.Application.Planning.Intents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Candidates.Generators
{
    public interface ITripCandidateGeneratorFactory
    {
        ICandidateGenerator<TIntent> Resolve<TIntent>() where TIntent : ITripIntent;
    }

    public sealed class TripCandidateGeneratorFactory : ITripCandidateGeneratorFactory
    {
        private readonly IServiceProvider _provider;

        public TripCandidateGeneratorFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public ICandidateGenerator<TIntent> Resolve<TIntent>() where TIntent : ITripIntent
        {
            var generator = _provider.GetService(typeof(ICandidateGenerator<TIntent>));
            if (generator is null)
                throw new InvalidOperationException($"No candidate generator registered for {typeof(TIntent).Name}");

            return (ICandidateGenerator<TIntent>)generator;
        }
    }
}
