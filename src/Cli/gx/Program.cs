using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading;

using Glacie.Logging;

namespace Glacie.Cli
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            // TODO: (Low) This is workaround for ArzWriter.Pipeline.cs or other threading code,
            // See comments. Without enough threads in thread pool, Terminal will not able to
            // fire timer evernts in right time, because ArzWriter/Reader is effectively spam
            // and block threads.
            ThreadPool.GetMinThreads(out var minWorkerThreads, out var minCompletionPortThreads);
            ThreadPool.SetMinThreads(minWorkerThreads + 4, minCompletionPortThreads);

            using var terminal = TerminalFactory.GetDefaultTerminal();

            var result = BuildCommandLine()
                .UseCli(terminal)
                .Build()
                .Invoke(args);

            return result;
        }

        private static CommandLineBuilder BuildCommandLine()
        {
            var rootCommand = new RootCommand("Glacie CLI")
            {
                CreateValidateCommand(),
            };
            rootCommand.AddGlobalOption(new Option<LogLevel>("--log-level", () => LogLevel.Information, "Logging level"));
            return new CommandLineBuilder(rootCommand);
        }

        private static Command CreateValidateCommand()
        {
            var command = new Command("validate", "...")
            {
                new Argument<string>("project", () => "", "Specifies the path of the project file to run (folder name or full path). If not specified, it defaults to the current directory.")
                {
                    Arity = ArgumentArity.ZeroOrOne
                },
                // new Option<string>(new [] { "-p", "--project" }, () => "", "Specifies the path of the project file to run (folder name or full path). If not specified, it defaults to the current directory."),

                new Option<bool>("--resolve-references", () => false, "Resolve resource references."),

                new Option<string>("--output-html-report", "Generates HTML report to specified file."),

                // new Option<string>("some-option", "")
                // build command should have -c/--configuration=debug/release option
                // however validate command should have option to pass config file explicitly.
                // -cc/--context-configuration=...
            };
            command.Handler = CommandHandler.Create((Commands.ValidateCommand cmd) => cmd.Run());
            return command;
        }
    }
}
