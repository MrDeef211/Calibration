using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Model.Exceptions
{
    public class DataErrorException : Exception
    {
        public DataErrorException(string message) : base(message) { }
    }
}
