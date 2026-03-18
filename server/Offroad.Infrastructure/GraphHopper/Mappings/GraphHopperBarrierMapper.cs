using Routing.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Infrastructure.GraphHopper.Mappings
{
    internal class GraphHopperBarrierMapper
    {
        public static BarrierType Map(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return BarrierType.Unknown;

            return value.ToLowerInvariant() switch
            {
                "gate" => BarrierType.Gate,
                "lift_gate" => BarrierType.LiftGate,
                "chain" => BarrierType.Chain,
                "block" => BarrierType.Block,
                "bollard" => BarrierType.Bollard,
                "swing_gate" => BarrierType.SwingGate,
                _ => BarrierType.Unknown
            };
        }
    }
}
