using Routing.Application.Planning.Candidates.Generators;
using Routing.Application.Planning.Goals;
using Routing.Application.Planning.Candidates.Scoring;
using Routing.Application.Planning.Mappings;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Candidates.Models;
using Routing.Domain.Models;
using Routing.Application.Planning.Profiles;
namespace Routing.Application.Planning.Pipelines
{
   public sealed class PlanningPipeline<TIntent, TCandidate> : IPlanningPipeline<TIntent, TCandidate>
        where TIntent : ITripIntent
        where TCandidate : TripCandidate
    {
        private readonly ICandidateGenerator<TIntent, TCandidate> _generator;
        private readonly ITripGoal<TIntent, TCandidate> _goal;
        private readonly ITripCandidateScorer<TIntent, TCandidate> _scorer;
        private readonly ITripMapper<TCandidate> _mapper;

        public PlanningPipeline(
            ICandidateGenerator<TIntent, TCandidate> generator,
            ITripGoal<TIntent, TCandidate> goal,
            ITripCandidateScorer<TIntent, TCandidate> scorer,
            ITripMapper<TCandidate> mapper)
        {
            _generator = generator;
            _goal = goal;
            _scorer = scorer;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<TripPlan>> PlanAsync(TIntent intent, UserRoutingProfile profile, CancellationToken ct)
        {
            // Generate
            var candidates = await _generator.GenerateCandidatesAsync(intent, ct);

            // Filter
            var validCandidates = candidates.Where(c => _goal.IsSatisfied(c, intent)).ToList();

            // Score
            var scored = _scorer.Score(validCandidates, intent, profile);

            // Sort & Map
            return scored
                .OrderByDescending(s => s.Score)
                .Select(s => _mapper.MapToPlan(s.Candidate))
                .ToList();
        }
    }
}