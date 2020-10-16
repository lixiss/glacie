using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading;

using Glacie.Data.Arz;
using Glacie.Data.Compression;
using Glacie.Lab.Analysis;
using Glacie.Lab.Core;
using Glacie.Lab.Metadata;
using Glacie.Logging;

using static Glacie.Cli.ArgumentUtilities;

namespace Glacie.Cli.Lab
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


            var cmd = Glacie.Lab.Command.Create<PathHashingCommand>(terminal);
            // var cmd = Glacie.Lab.Command.Create<ScanForDbrReferencesCommand>(terminal);
            // var cmd = Glacie.Lab.Command.Create<CreateMetadataCommand>(terminal);
            // var cmd = Glacie.Lab.Command.Create<ResourceCountByTypeCommand>(terminal);
            cmd.Run();


            return 0;


            var result = BuildCommandLine()
                .UseCli(terminal)
                .Build()
                .Invoke(args);

            return result;
        }

        private static CommandLineBuilder BuildCommandLine()
        {
            var rootCommand = new RootCommand("Glacie Lab")
            {
            };
            return new CommandLineBuilder(rootCommand);
        }
    }
}
