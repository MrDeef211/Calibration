using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Model.Exceptions
{
    public class FileErrorException : Exception
    {
        public FileErrorException(string message) : base(message) { }
    }
}
