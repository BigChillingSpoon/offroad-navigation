using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Exceptions
{
    public class RoutingProviderException : Exception
    {
        public RoutingProviderErrorCategory ErrorCathegory { get; private init; }
        public RoutingProviderException(RoutingProviderErrorCategory cathegory, string message, Exception inner) : base(message, inner) 
        {
            ErrorCathegory = cathegory;
        }
        
        public RoutingProviderException(RoutingProviderErrorCategory cathegory, string message) : base(message)
        {
            ErrorCathegory = cathegory;
        }
    }
}
