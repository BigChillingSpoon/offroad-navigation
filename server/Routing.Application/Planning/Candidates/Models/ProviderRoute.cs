using Routing.Domain.Enums;
﻿using System;
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

        public EncodedPolyline Polyline { get; init; } = new();
    }
    public sealed record EncodedPolyline
    {
        public string Points { get; init; } = string.Empty; // encoded polyline
        public double PolylineEncodedMultiplier { get; init; }
        public PolylineDimension Dimension {  get; init; }
    }
}
