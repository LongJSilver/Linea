using System;

namespace Linea.Exceptions
{
    public class UnknownArgumentException : ArgumentException
    {
        public readonly string ArgumentName;

        public UnknownArgumentException(string argumentName) : base($"Unknown argument : <{(argumentName ?? throw new ArgumentNullException(nameof(argumentName)))}>")
        {
            this.ArgumentName = argumentName;
        }
    }
}
