using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Intents;

namespace Routing.Application.Planning.Candidates.Scoring
{
    public interface ITripCandidateScorerFactory
    {
        ITripCandidateScorer<TIntent, TCandidate> Resolve<TIntent, TCandidate>()
            where TIntent : ITripIntent
            where TCandidate : TripCandidate;
    }
    public class TripCandidateScorerFactory : ITripCandidateScorerFactory
    {
        private readonly IServiceProvider _serviceProvider;
        public TripCandidateScorerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public ITripCandidateScorer<TIntent, TCandidate> Resolve<TIntent, TCandidate>() 
            where TIntent : ITripIntent
            where TCandidate : TripCandidate
        {
            return _serviceProvider.GetService(typeof(ITripCandidateScorer<TIntent, TCandidate>)) as ITripCandidateScorer<TIntent, TCandidate>
                ?? throw new InvalidOperationException($"No scorer registered for intent type {typeof(TIntent).Name}");
        }
    }
}
