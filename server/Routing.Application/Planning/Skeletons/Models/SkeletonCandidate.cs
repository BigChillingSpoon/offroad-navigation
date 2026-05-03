using Routing.Domain.Models;
using System.Collections.Generic;

namespace Routing.Application.Planning.Skeletons.Models;

/// <summary>
/// Represents a single valid output from the skeleton generation algorithm.
/// It contains an ordered list of elite hookpoints that form the backbone of a potential loop.
/// </summary>
/// <param name="Hookpoints">The ordered list of hookpoints forming the loop skeleton.</param>
public record SkeletonCandidate(IReadOnlyList<Hookpoint> Hookpoints);
