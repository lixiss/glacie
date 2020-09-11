using System;

namespace Glacie.Logging
{
    // TODO: (Low) (Glacie.Logging) Design abstract logger. However it is better to use concrete logger, but interfaces
    // might be useful for increased interoperability.
    internal interface ILogger
    {
        bool IsEnabled(LogLevel logLevel);
        void Log(LogLevel logLevel, string message);
        void Log(LogLevel logLevel, Exception exception, string message);
    }
}
