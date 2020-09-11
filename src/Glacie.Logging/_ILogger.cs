using System;

namespace Glacie.Logging
{
    // TODO: (Low) (Glacie.Logging) Design scoped logger.
    internal interface _ILogger
    {
        IDisposable BeginScope<TState>(TState state);

        bool IsEnabled(LogLevel logLevel);

        void Log<TState>(LogLevel logLevel,
            // event_id
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter // TODO: formatter should be object which allow non-allocating message writing
            );
    }
}
