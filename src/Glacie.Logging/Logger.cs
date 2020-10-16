using System;
using System.Globalization;
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

        public bool TraceEnabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Enabled(LogLevel.Trace);
        }

        public bool DebugEnabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Enabled(LogLevel.Debug);
        }

        public bool InformationEnabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Enabled(LogLevel.Information);
        }

        public bool WarningEnabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Enabled(LogLevel.Warning);
        }

        public bool ErrorEnabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Enabled(LogLevel.Error);
        }

        public bool CriticalEnabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Enabled(LogLevel.Critical);
        }

        #region Log

        public void Trace(string message)
        {
            if (TraceEnabled)
            {
                Log(LogLevel.Trace, exception: null, message: message);
            }
        }

        public void Debug(string message)
        {
            if (DebugEnabled)
            {
                Log(LogLevel.Debug, exception: null, message: message);
            }
        }

        public void Information(string message)
        {
            if (InformationEnabled)
            {
                Log(LogLevel.Information, exception: null, message: message);
            }
        }

        public void Warning(string message)
        {
            if (WarningEnabled)
            {
                Log(LogLevel.Warning, exception: null, message: message);
            }
        }

        public void Error(string message)
        {
            if (ErrorEnabled)
            {
                Log(LogLevel.Error, exception: null, message: message);
            }
        }

        public void Critical(string message)
        {
            if (CriticalEnabled)
            {
                Log(LogLevel.Critical, exception: null, message: message);
            }
        }

        #endregion

        #region Log: String Format

        public void Trace(string format, params object?[] args)
        {
            if (TraceEnabled)
            {
                Log(LogLevel.Trace, exception: null, message: Format(format, args));
            }
        }

        public void Debug(string format, params object?[] args)
        {
            if (DebugEnabled)
            {
                Log(LogLevel.Debug, exception: null, message: Format(format, args));
            }
        }

        public void Information(string format, params object?[] args)
        {
            if (InformationEnabled)
            {
                Log(LogLevel.Information, exception: null, message: Format(format, args));
            }
        }

        public void Warning(string format, params object?[] args)
        {
            if (WarningEnabled)
            {
                Log(LogLevel.Warning, exception: null, message: Format(format, args));
            }
        }

        public void Error(string format, params object?[] args)
        {
            if (ErrorEnabled)
            {
                Log(LogLevel.Error, exception: null, message: Format(format, args));
            }
        }

        public void Critical(string format, params object?[] args)
        {
            if (CriticalEnabled)
            {
                Log(LogLevel.Critical, exception: null, message: Format(format, args));
            }
        }

        #endregion

        public void Log(LogLevel level, string format, params object?[] args)
        {
            if (Enabled(level))
            {
                Log(level, exception: null, message: Format(format, args));
            }
        }

        public void Log(LogLevel level, Exception? exception, string format, params object?[] args)
        {
            if (Enabled(level))
            {
                Log(level, exception, message: Format(format, args));
            }
        }

        public abstract void Log(LogLevel level, Exception? exception, string message);

        private static string Format(string format, params object?[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, format, args);
        }
    }
}
