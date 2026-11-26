using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Model.Exceptions
{
    public class MathStabilityException : Exception
    {
        public MathStabilityException(string message) : base(message) { }
    }
}
