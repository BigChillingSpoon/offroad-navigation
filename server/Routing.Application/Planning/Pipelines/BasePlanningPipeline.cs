using Routing.Application.Planning.Candidates.Generators;
using Routing.Application.Planning.Candidates.Models;
using Routing.Application.Planning.Candidates.Scoring;
using Routing.Application.Planning.Goals;
using Routing.Application.Planning.Intents;
using Routing.Application.Planning.Planner;
using Routing.Application.Planning.Profiles;
using Routing.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Pipelines
{
    internal abstract class BasePlanningPipeline<TIntent, TCandidate> : IPlanningPipeline<TIntent>
    where TIntent : ITripIntent
    where TCandidate : TripCandidate
    {
        private readonly ICandidateGenerator<TIntent, TCandidate> _generator;
        private readonly ITripGoal<TIntent, TCandidate> _goal;
        private readonly ITripCandidateScorer<TIntent, TCandidate> _scorer;

        protected BasePlanningPipeline(
            ICandidateGenerator<TIntent, TCandidate> generator,
            ITripGoal<TIntent, TCandidate> goal,
            ITripCandidateScorer<TIntent, TCandidate> scorer)
        {
            _generator = generator;
            _goal = goal;
            _scorer = scorer;
        }

        public async Task<IReadOnlyList<TripPlan>> PlanAsync(TIntent intent, UserRoutingProfile profile, CancellationToken ct)
        {
            var candidates = await _generator.GenerateCandidatesAsync(intent, ct);

            // Hard filtering
            var validCandidates = candidates
                .Where(c => _goal.IsSatisfied(c, intent))
                .ToList();

            // Scoring
            var scored = _scorer.Score(validCandidates, intent, profile);

            // Ordering
            var ordered = scored
                .OrderByDescending(s => s.Score)
                .Select(s => MapToPlan(s.Candidate))
                .ToList();

            return ordered;
        }
        protected abstract TripPlan MapToPlan(TCandidate candidate);
    }
}
