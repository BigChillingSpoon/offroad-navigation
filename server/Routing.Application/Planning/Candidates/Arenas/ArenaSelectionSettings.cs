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
        /// ranges ~L/4 out from its start, so we look for offroad density within that radius.
        /// </summary>
        public const double ReachFactor = 0.25;

        /// <summary>
        /// Minimum spacing between two chosen entrances, as a fraction of loop length. Two loops each
        /// reach ~ReachFactor*L out from their entrance, so their areas are disjoint only when the
        /// entrances are at least 2*ReachFactor*L apart - hence 0.5. Using the reach itself (0.25)
        /// let entrances one reach apart through, producing visibly overlapping loops.
        /// </summary>
        public const double MinEntranceSeparationFactor = 0.5;

        /// <summary>
        /// How many spatially-distinct arenas to return (the "meadow"). Each becomes its own skeleton
        /// search + routing call, so this is also a cost governor; raise it for more loops. Some arenas
        /// yield no loop (sparse topology) or get rejected downstream (routed distance out of band), so
        /// the final loop count is usually lower than this.
        /// </summary>
        public const int MaxEntrances = 8;

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

        /// <summary>
        /// Crossover loop length (km): the single scale at which arena ranking weighs intersection
        /// density and offroad length equally. Below it, density dominates (short loops want a dense
        /// mesh of small edges); above it, length dominates and long edges are preferred (big loops
        /// want a big arena of few, long edges - which also keeps the skeleton search smaller/faster).
        /// </summary>
        public const double RankingCrossoverLoopKm = 5.0;

        /// <summary>
        /// Smooth blend factor t = L / (L + L0) in (0,1) from loop length - continuous with no
        /// thresholds, so nearby lengths (9.9 vs 10.1 km) rank almost identically. t -> 0 as loops
        /// shrink (pure density), t = 0.5 at L = L0 (pure length), t -> 1 as loops grow (length
        /// favouring long edges). The repository ranks by <c>offroad_len^(2t) * edge_count^(1-2t)</c>.
        /// </summary>
        public static double RankingLengthBlend(double preferredLengthKm) =>
            preferredLengthKm / (preferredLengthKm + RankingCrossoverLoopKm);
    }
}
