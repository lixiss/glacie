using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Threading;

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
            var rootCommand = new RootCommand("Glacie Command Line Interface")
            {
            };
            return new CommandLineBuilder(rootCommand);
        }
    }
}
