using Routing.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.State
{
    public sealed class PlannerState
    {
        public double TotalDistance { get; }
        public double OffroadDistance { get; }
        public Coordinate CurrentPosition { get; }
        public PlannerHistory History { get; init; }

        private PlannerState(Coordinate start)
        {
            TotalDistance = 0;
            OffroadDistance = 0;
            CurrentPosition = start;
            History = new();
        }

        public static PlannerState Initialize(Coordinate start)
        {
            return new PlannerState(start);
        }

    }
}
