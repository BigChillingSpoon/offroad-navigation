using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Candidates.Models
{
    public sealed class ProviderRoute
    {
        public double Distance { get; init; }

        public TimeSpan Duration { get; init; }

        public double Ascend { get; init; }

        public double Descend { get; init; }

        public string Polyline { get; init; } = string.Empty; // polyline
    }
}
