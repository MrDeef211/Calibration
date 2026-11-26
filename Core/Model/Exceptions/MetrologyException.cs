using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Model.Exceptions
{
    public class MetrologyException : Exception
    {
        public bool Test { get; init; }

        public MetrologyException(string message, bool test) : base(message)
        {
            Test = test;
        }
    }
}
