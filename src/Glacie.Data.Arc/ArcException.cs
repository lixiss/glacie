using System;

namespace Glacie.Data.Arc
{
    // TODO: (Medium) Unify GlacieException, ArzException and ArcException. Also follow standard exception implementation.

    public sealed class ArcException : Exception
    {
        public ArcException(string errorCode, string message)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public ArcException(string errorCode, string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }

        public string ErrorCode { get; }
    }
}
