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

        private readonly ITripCandidateScorer _candidateScorer;
        public TripPlanner(ITripCandidateGeneratorFactory candidateGeneratorFactory, ITripCandidateScorer candidateScorer)
        {
            _candidateGeneratorFactory = candidateGeneratorFactory;
            _candidateScorer = candidateScorer;
        }
        public async Task<List<TripPlan>> PlanAsync<TIntent>(TIntent intent, ITripGoal<TIntent> goal, UserRoutingProfile profile, PlannerSettings settings, CancellationToken ct) where TIntent : ITripIntent
        {
            var generator = _candidateGeneratorFactory.Resolve<TIntent>();
            var candidates = await generator.GenerateCandidatesAsync(intent, profile, settings, ct);

            // 1) HARD FILTER = GOAL
            var validCandidates = candidates
                .Where(c => goal.IsSatisfied(c, intent))
                .ToList();

            // 2) SCORE (preference ranking)
            var scored = _candidateScorer.Score(validCandidates, intent, profile, settings);

            // 3) order by score
            var ordered = scored
                .OrderByDescending(s => s.Score)
                .Select(s => s.Candidate.ToTripPlan())
                .ToList();

            return ordered;
        }
    }
}
