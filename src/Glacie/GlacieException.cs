using System;

namespace Glacie
{
    // TODO: (Gx) GlacieException - unify GlacieException/ArzException.
    public sealed class GlacieException : Exception
    {
        public GlacieException(string errorCode, string message)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public GlacieException(string errorCode, string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }

        public string ErrorCode { get; }
    }
}
