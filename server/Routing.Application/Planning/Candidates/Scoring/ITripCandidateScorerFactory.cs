using Routing.Application.Planning.Intents;

namespace Routing.Application.Planning.Candidates.Scoring
{
    public interface ITripCandidateScorerFactory
    {
        ITripCandidateScorer<TIntent> Resolve<TIntent>() where TIntent : ITripIntent;
    }
    public class TripCandidateScorerFactory : ITripCandidateScorerFactory
    {
        private readonly IServiceProvider _serviceProvider;
        public TripCandidateScorerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public ITripCandidateScorer<TIntent> Resolve<TIntent>() where TIntent : ITripIntent
        {
            return _serviceProvider.GetService(typeof(ITripCandidateScorer<TIntent>)) as ITripCandidateScorer<TIntent>
                ?? throw new InvalidOperationException($"No scorer registered for intent type {typeof(TIntent).Name}");
        }
    }
}
