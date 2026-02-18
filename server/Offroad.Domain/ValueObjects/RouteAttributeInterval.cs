using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Domain.ValueObjects
{
    public abstract record RouteAttributeInterval
    {
        public required int FromIndex { get; set; }
        public required int ToIndex { get; set; }
    }
    
}
