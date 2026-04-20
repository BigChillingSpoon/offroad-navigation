using Routing.Application.Planning.Candidates.Models;
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
        ICandidateGenerator<TIntent,TCandidate> Resolve<TIntent,TCandidate>() 
            where TIntent : ITripIntent
            where TCandidate : TripCandidate;
    }

    public sealed class TripCandidateGeneratorFactory : ITripCandidateGeneratorFactory
    {
        private readonly IServiceProvider _provider;

        public TripCandidateGeneratorFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public ICandidateGenerator<TIntent, TCandidate> Resolve<TIntent, TCandidate>() 
            where TIntent : ITripIntent
            where TCandidate : TripCandidate
        {
            var generator = _provider.GetService(typeof(ICandidateGenerator<TIntent, TCandidate>));
            if (generator is null)
                throw new InvalidOperationException($"No candidate generator registered for {typeof(TIntent).Name}");

            return (ICandidateGenerator<TIntent, TCandidate>)generator;
        }
    }
}
