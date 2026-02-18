using Routing.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Infrastructure.GraphHopper.Mappings
{
    internal static class GraphHopperSurfaceMapper
    {
        public static SurfaceType Map(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return SurfaceType.Unknown;

            var v = value.Trim().ToLowerInvariant();

            return v switch
            {
                // explicitně "missing" z GH details
                "missing" => SurfaceType.Missing,

                // paved
                "asphalt" => SurfaceType.Asphalt,
                "concrete" => SurfaceType.Concrete,
                "paving_stones" => SurfaceType.PavingStones,
                "cobblestone" => SurfaceType.Cobblestone,

                // semi-paved
                "gravel" => SurfaceType.Gravel,
                "fine_gravel" => SurfaceType.FineGravel,
                "compacted" => SurfaceType.Compacted,

                // natural
                "dirt" => SurfaceType.Dirt,
                "ground" => SurfaceType.Ground,
                "sand" => SurfaceType.Sand,
                "mud" => SurfaceType.Mud,
                "grass" => SurfaceType.Grass,

                // other
                "wood" => SurfaceType.Wood,
                "metal" => SurfaceType.Metal,
                "ice" => SurfaceType.Ice,
                "snow" => SurfaceType.Snow,

                _ => SurfaceType.Unknown
            };
        }
    }
}
