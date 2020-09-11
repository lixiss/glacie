using System;
using System.Globalization;

namespace Glacie.Logging
{
    public static class LoggerExtensions
    {
        #region Log: Message

        public static void Trace(this Logger log, string message)
        {
            if (log.Enabled(LogLevel.Trace))
            {
                log.Log(LogLevel.Trace, exception: null, message: message);
            }
        }

        public static void Debug(this Logger log, string message)
        {
            if (log.Enabled(LogLevel.Debug))
            {
                log.Log(LogLevel.Debug, exception: null, message: message);
            }
        }

        public static void Information(this Logger log, string message)
        {
            if (log.Enabled(LogLevel.Information))
            {
                log.Log(LogLevel.Information, exception: null, message: message);
            }
        }

        public static void Warning(this Logger log, string message)
        {
            if (log.Enabled(LogLevel.Warning))
            {
                log.Log(LogLevel.Warning, exception: null, message: message);
            }
        }

        public static void Error(this Logger log, string message)
        {
            if (log.Enabled(LogLevel.Error))
            {
                log.Log(LogLevel.Error, exception: null, message: message);
            }
        }

        public static void Critical(this Logger log, string message)
        {
            if (log.Enabled(LogLevel.Critical))
            {
                log.Log(LogLevel.Critical, exception: null, message: message);
            }
        }

        #endregion

        #region Log: String Format

        public static void Log(this Logger log, LogLevel level, string format, params object[] args)
        {
            if (log.Enabled(level))
            {
                log.Log(level, exception: null, message: Format(format, args));
            }
        }

        public static void Trace(this Logger log, string format, params object[] args)
        {
            if (log.Enabled(LogLevel.Trace))
            {
                log.Log(LogLevel.Trace, exception: null, message: Format(format, args));
            }
        }

        public static void Debug(this Logger log, string format, params object[] args)
        {
            if (log.Enabled(LogLevel.Debug))
            {
                log.Log(LogLevel.Debug, exception: null, message: Format(format, args));
            }
        }

        public static void Information(this Logger log, string format, params object[] args)
        {
            if (log.Enabled(LogLevel.Information))
            {
                log.Log(LogLevel.Information, exception: null, message: Format(format, args));
            }
        }

        public static void Warning(this Logger log, string format, params object[] args)
        {
            if (log.Enabled(LogLevel.Warning))
            {
                log.Log(LogLevel.Warning, exception: null, message: Format(format, args));
            }
        }

        public static void Error(this Logger log, string format, params object[] args)
        {
            if (log.Enabled(LogLevel.Error))
            {
                log.Log(LogLevel.Error, exception: null, message: Format(format, args));
            }
        }

        public static void Critical(this Logger log, string format, params object[] args)
        {
            if (log.Enabled(LogLevel.Critical))
            {
                log.Log(LogLevel.Critical, exception: null, message: Format(format, args));
            }
        }

        #endregion

        private static string Format(string format, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, format, args);
        }
    }
}
