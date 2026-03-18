using Routing.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Domain.ValueObjects
{
    public sealed record RoadBarrier(
        BarrierType Type,
        int PointIndex,
        Coordinate Coordinate 
    );
}
