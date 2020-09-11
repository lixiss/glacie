using System;

namespace Glacie.Data.Dbr
{
    public sealed class DbrParseErrorException : Exception
    {
        public DbrParseErrorException(string message)
            : base(message)
        {
        }
    }
}
