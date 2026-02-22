using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Exceptions
{
    public enum RoutingProviderErrorCategory
    {
        Timeout,
        HttpError,
        InvalidResponse,
        Unavailable
    }
}
