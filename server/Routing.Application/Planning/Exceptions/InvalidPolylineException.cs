using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Exceptions
{
    internal class InvalidPolylineException : Exception
    {
        public InvalidPolylineException(string message) : base(message)
        {
        }

        public InvalidPolylineException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
