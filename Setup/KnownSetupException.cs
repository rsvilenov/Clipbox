using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Setup
{
    public class KnownSetupException : ApplicationException
    {
        public KnownSetupException()
        {
        }

        public KnownSetupException(string message)
        : base(message)
        {
        }

        public KnownSetupException(string message, Exception innerException)
        : base(message, innerException)
        {
        }
    }
}
