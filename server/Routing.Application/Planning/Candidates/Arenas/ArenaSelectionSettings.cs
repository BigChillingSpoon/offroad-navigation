namespace Routing.Application.Planning.Candidates.Arenas
{
    /// <summary>
    /// Tuning knobs for offroad "arena" (entrance) selection in <see cref="ArenaFinder"/>. The factors
    /// are expressed relative to the requested loop length so they scale automatically with everything
    /// from short loops up to 20-30km ones. Grouped here so they can be tuned in one place.
    /// </summary>
    public static class ArenaSelectionSettings
    {
        /// <summary>
        /// Fraction of the loop length used as its reach radius: a closed loop of perimeter L only
        /// ranges ~L/4 out from its start, so we look for offroad density within that radius. Also
        /// used as the minimum spacing between two chosen entrances (greedy spatial diversity).
        /// </summary>
        public const double ReachFactor = 0.25;

        /// <summary>
        /// Multiple of the loop length an arena must offer as reachable offroad before it qualifies -
        /// the skeleton search needs clearly more terrain than the loop itself to find a well-shaped
        /// closed loop rather than a single there-and-back track.
        /// </summary>
        public const double MinOffroadDensityFactor = 1.5;

        /// <summary>
        /// Tolerance (meters) for recognising the user's own start among the returned candidates -
        /// the repository always returns it, at ~0 m from the requested start.
        /// </summary>
        public const double UserStartMatchMeters = 5.0;

        /// <summary>
        /// How many more candidates to fetch than we finally keep, giving greedy spatial-diversity
        /// room to skip overlapping arenas and still fill every entrance slot.
        /// </summary>
        public const int CandidateFetchMultiplier = 10;
    }
}
