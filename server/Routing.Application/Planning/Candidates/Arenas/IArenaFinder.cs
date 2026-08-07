using Routing.Domain.ValueObjects;

namespace Routing.Application.Planning.Candidates.Arenas
{
    /// <summary>
    /// Selects the offroad "arenas" (entry points) a loop should be generated from: the entrances
    /// within the user's drive range best able to sustain a loop of the requested length. Today the
    /// decision is offroad density plus greedy spatial-diversity to avoid overlapping loops; further
    /// conditions (overlap scoring, terrain variety, ...) will be added here so the generator stays
    /// agnostic of how "most suitable" is decided.
    /// </summary>
    public interface IArenaFinder
    {
        /// <summary>
        /// Returns up to <paramref name="maxEntrances"/> entrance coordinates, best-first, from which
        /// loops of ~<paramref name="preferredLengthKm"/> can be generated, within
        /// <paramref name="maxDriveDistanceKm"/> of <paramref name="userStart"/>. Empty if none qualify.
        /// </summary>
        Task<IReadOnlyList<Coordinate>> FindEntrancesAsync(
            Coordinate userStart,
            double preferredLengthKm,
            double maxDriveDistanceKm,
            int maxEntrances,
            CancellationToken ct);
    }
}
