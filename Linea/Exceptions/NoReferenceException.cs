using System;

namespace Linea.Exceptions
{
    public class NoReferenceException : InvalidOperationException
    {
        public NoReferenceException()
        : base("No reference to check against")
        {
        }
    }
}
