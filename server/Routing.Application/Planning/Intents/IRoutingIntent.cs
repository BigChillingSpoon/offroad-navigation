using Routing.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Intents
{
    public interface IRoutingIntent
    {
        Coordinate Start { get; }
    }
}
