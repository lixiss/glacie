using System;

namespace Glacie.Data.Arz
{
    // TODO: (Medium) Unify GlacieException and ArzException. Also follow standard exception implementation.

    public sealed class ArzException : Exception
    {
        public ArzException(string errorCode, string message)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public ArzException(string errorCode, string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }

        public string ErrorCode { get; }
    }
}
