using Glacie.CommandLine;
using Glacie.Logging;

namespace Glacie.Cli.Logging
{
    public static class LoggerFactory
    {
        public static Logger CreateLogger(LogLevel level, IConsole console)
        {
            return new ConsoleLogger(console, level);
        }

        public static Logger CreateDefaultLogger(LogLevel level = LogLevel.Trace)
        {
            return new ConsoleLogger(
                TerminalFactory.GetDefaultTerminal(),
                level);
        }
    }
}
