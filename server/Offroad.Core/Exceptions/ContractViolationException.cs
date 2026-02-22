using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Offroad.Core.Exceptions
{
    public class ContractViolationException : Exception
    {
        public ContractViolationException(string message) : base(message)
        {
        }
    }
}
