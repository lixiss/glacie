using System;

namespace Glacie.Cli
{
    // Exception which will be translated to user-level error and will be
    // reported without stacktrace.
    public sealed class CliErrorException : Exception
    {
        public CliErrorException(string? message)
            : base(message)
        { }

        public CliErrorException(string? message, Exception? innerException)
            : base(message ?? innerException?.Message, innerException)
        { }
    }
}
