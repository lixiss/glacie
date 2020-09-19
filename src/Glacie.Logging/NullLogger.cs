using System;

namespace Glacie.Logging
{
    internal sealed class NullLogger : Logger
    {
        private static readonly NullLogger s_instance = new NullLogger();

        public static Logger Instance => s_instance;

        private NullLogger() : base((LogLevel)int.MaxValue)
        { }

        public override void Log(LogLevel level, Exception? exception, string message)
        { }
    }
}
