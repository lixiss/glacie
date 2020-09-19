using System;
using System.Runtime.CompilerServices;

namespace Glacie.Logging
{
    public abstract class Logger
    {
        public static Logger Null => NullLogger.Instance;

        private LogLevel _level;

        protected Logger(LogLevel level)
        {
            _level = level;
        }

        public LogLevel Level => _level;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Enabled(LogLevel level)
        {
            return level >= _level;
        }

        public abstract void Log(LogLevel level, Exception? exception, string message);
    }
}
