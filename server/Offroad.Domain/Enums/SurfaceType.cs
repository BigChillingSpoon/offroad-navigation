using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Domain.Enums
{
    public enum SurfaceType
    {
        Unknown = 0,
        Missing,

        Asphalt,
        Concrete,
        PavingStones,
        Cobblestone,

        Gravel,
        FineGravel,
        Compacted,

        Dirt,
        Ground,
        Sand,
        Mud,
        Grass,

        Wood,
        Metal,
        Ice,
        Snow
    }
}
