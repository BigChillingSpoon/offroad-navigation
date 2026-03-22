using Routing.Application.Planning.Candidates.Generators;
using Routing.Application.Planning.Candidates.Scoring;
using Routing.Application.Planning.Goals;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Profiles;
using Routing.Domain.Models;
using Routing.Application.Mappings;
namespace Routing.Application.Planning.Planner
{
    public class TripPlanner : ITripPlanner
    {
        private readonly ITripCandidateGeneratorFactory _candidateGeneratorFactory;

        private readonly ITripCandidateScorerFactory _candidateScorerFactory;
        public TripPlanner(ITripCandidateGeneratorFactory candidateGeneratorFactory, ITripCandidateScorerFactory candidateScorerFactory)
        {
            _candidateGeneratorFactory = candidateGeneratorFactory;
            _candidateScorerFactory = candidateScorerFactory;
        }
        public async Task<IReadOnlyList<TripPlan>> PlanAsync<TIntent>(TIntent intent, ITripGoal<TIntent> goal, UserRoutingProfile profile, PlannerSettings settings, CancellationToken ct) where TIntent : ITripIntent
        {
            var generator = _candidateGeneratorFactory.Resolve<TIntent>();
            var candidates = await generator.GenerateCandidatesAsync(intent, settings, ct);

            // 1) HARD FILTER = GOAL
            var validCandidates = candidates
                .Where(c => goal.IsSatisfied(c, intent))
                .ToList();

            // 2) SCORE (preference ranking)
            var scorer = _candidateScorerFactory.Resolve<TIntent>();
            var scored = scorer.Score(validCandidates, intent, profile, settings);

            // 3) order by score
            var ordered = scored
                .OrderByDescending(s => s.Score)
                .Select(s => s.Candidate.ToTripPlan())
                .ToList();

            return ordered;
        }
    }
}
