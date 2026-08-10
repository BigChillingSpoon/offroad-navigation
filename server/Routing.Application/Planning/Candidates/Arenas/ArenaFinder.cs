using Routing.Application.Abstractions.Persistence;
using Routing.Domain.Utilities;
using Routing.Domain.ValueObjects;

namespace Routing.Application.Planning.Candidates.Arenas
{
    /// <inheritdoc />
    public sealed class ArenaFinder : IArenaFinder
    {
        private readonly INodeRepository _nodeRepository;

        public ArenaFinder(INodeRepository nodeRepository)
        {
            _nodeRepository = nodeRepository;
        }

        public async Task<IReadOnlyList<Coordinate>> FindEntrancesAsync(
            Coordinate userStart,
            double preferredLengthKm,
            double maxDriveDistanceKm,
            int maxEntrances,
            CancellationToken ct)
        {
            var preferredLengthMeters = preferredLengthKm * 1000.0;
            var loopReachMeters = preferredLengthMeters * ArenaSelectionSettings.ReachFactor;
            var minSeparationMeters = preferredLengthMeters * ArenaSelectionSettings.MinEntranceSeparationFactor;
            var minOffroadLengthMeters = preferredLengthMeters * ArenaSelectionSettings.MinOffroadDensityFactor;

            var candidates = await _nodeRepository.GetCandidateArenaEntrancesAsync(
                userStart,
                maxDriveDistanceKm * 1000.0,
                loopReachMeters,
                minOffroadLengthMeters,
                ArenaSelectionSettings.RankingLengthBlend(preferredLengthKm),
                limit: maxEntrances * ArenaSelectionSettings.CandidateFetchMultiplier,
                ct);

            if (candidates.Count == 0)
                return Array.Empty<Coordinate>();

            var selected = new List<Coordinate>();

            // "User already inside offroad": if the user's own start cleared the density threshold, it
            // comes back among the candidates ~0 m away - prefer a no-drive loop straight from there
            // before considering any entry point we'd have to drive to.
            var userStartCandidate = candidates
                .Where(c => GeoCalculator.CalculateDistance(c.Coordinate, userStart) <= ArenaSelectionSettings.UserStartMatchMeters)
                .Select(c => (ArenaEntrance?)c)
                .FirstOrDefault();
            if (userStartCandidate is { } userArena)
                selected.Add(userArena.Coordinate);

            // Greedy spatial diversity: take the densest arenas first (by offroad-edge/intersection
            // density, which predicts loop-ability better than raw track length), skipping any that sit
            // within the minimum separation of an already-picked entrance so the returned arenas yield
            // genuinely non-overlapping loops rather than several variations of the same one. (This also
            // skips the user-start candidate on its second encounter, since it is 0 m from itself.)
            foreach (var candidate in candidates.OrderByDescending(c => c.ArenaScore))
            {
                if (selected.Count >= maxEntrances)
                    break;

                if (selected.Any(s => GeoCalculator.CalculateDistance(s, candidate.Coordinate) < minSeparationMeters))
                    continue;

                selected.Add(candidate.Coordinate);
            }

            return selected;
        }
    }
}
