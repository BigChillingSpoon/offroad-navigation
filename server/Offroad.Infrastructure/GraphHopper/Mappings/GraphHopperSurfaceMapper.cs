using Routing.Domain.Enums;
using System;

namespace Routing.Infrastructure.GraphHopper.Mappings
{
    internal static class GraphHopperSurfaceMapper
    {
        public static SurfaceType Map(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return SurfaceType.UNKNOWN;

            var v = value.Trim().ToLowerInvariant();

            return v switch
            {
                // Explicitly missing from routing provider
                "missing" => SurfaceType.MISSING,

                // Paved 
                "paved" => SurfaceType.PAVED,
                "asphalt" => SurfaceType.ASPHALT,
                "concrete" => SurfaceType.CONCRETE,
                "paving_stones" => SurfaceType.PAVING_STONES,
                "cobblestone" => SurfaceType.COBBLESTONE,

                // Semi-paved / Unpaved 
                "unpaved" => SurfaceType.UNPAVED,
                "gravel" => SurfaceType.GRAVEL,
                "fine_gravel" => SurfaceType.FINE_GRAVEL,
                "compacted" => SurfaceType.COMPACTED,

                // Natural
                "dirt" => SurfaceType.DIRT,
                "ground" => SurfaceType.GROUND,
                "sand" => SurfaceType.SAND,
                "grass" => SurfaceType.GRASS,

                // Other & Special
                "wood" => SurfaceType.WOOD,
                "other" => SurfaceType.OTHER,

                _ => SurfaceType.UNKNOWN
            };
        }
    }
}