using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Infrastructure.GraphHopper.DTOs
{
    /// <summary>
    /// Interval is [FromIndex, ToIndex) - ToIndex is exclusive.
    /// </summary>

    public record GraphHopperAttributeInterval
    (
        int FromIndex,
        int ToIndex,
        string Value
    );
}
