using System;

using Glacie.CommandLine;
using Glacie.CommandLine.IO;
using Glacie.Logging;

namespace Glacie.Cli.Logging
{
    public sealed class ConsoleLogger : Logger
    {
        private readonly IConsole _console;

        public ConsoleLogger(IConsole console, LogLevel level)
            : base(level)
        {
            _console = console;
        }

        public override void Log(LogLevel level, Exception? exception, string message)
        {
            IStandardStreamWriter writer;
            string levelString;

            switch (level)
            {
                case LogLevel.Trace:
                    writer = _console.Out;
                    levelString = "trace";
                    break;

                case LogLevel.Debug:
                    writer = _console.Out;
                    levelString = "debug";
                    break;

                case LogLevel.Information:
                    writer = _console.Out;
                    levelString = "info";
                    break;

                case LogLevel.Warning:
                    writer = _console.Out;
                    levelString = "warning";
                    break;

                case LogLevel.Error:
                    writer = _console.Out;
                    levelString = "error";
                    break;

                case LogLevel.Critical:
                    writer = _console.Error;
                    levelString = "critical";
                    break;

                default:
                    writer = _console.Error;
                    levelString = "FATAL";
                    break;
            }

            writer.Write(levelString);
            writer.Write(": ");
            writer.WriteLine(message);
            if (exception != null)
            {
                writer.WriteLine(exception.ToString());
            }
        }
    }
}
