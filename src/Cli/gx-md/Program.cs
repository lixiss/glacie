using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading;

using Glacie.Data;
using Glacie.Logging;

namespace Glacie.Cli.Metadata
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
            var rootCommand = new RootCommand("Glacie Metadata Tool")
            {
                CreateValidateCommand(),
            };
            rootCommand.AddGlobalOption(new Option<LogLevel>("--log-level", () => LogLevel.Information, "Logging level"));
            return new CommandLineBuilder(rootCommand);
        }

        private static Command CreateValidateCommand()
        {
            var command = new Command("create", "Create GXMD/GXMP metadata file")
            {
                new Option<string>("--metadata", "Metadata source (.arz, .tpl directory, templates .arc or .zip archive or .gxmd). May accept multiple sources, but any non-first source should be .gxmp file.")
                {
                    IsRequired = true,
                },
                new Option<string>("--output", "Output filename (.gxmd or .gxmp), or directory in multipart mode.")
                {
                    IsRequired = true,
                },
                new Option<string>("--output-format", () => "gxmd", "Specifies GXMD or GXMP output format")
                    .AddSuggestions("gxmd", "gxmp-boilerplate"),
                new Option<bool>("--multipart", () => false, "Enables multipart output"),
                new Option<string>("--mp-main", "Name of main filename (only in multipart mode)"),
                new Option<string>("--mp-include", "Name of subdirectory where include files will be written"),
                new Option<bool>("--emit-var-only", () => false, "Emit only properties which is defined in templates format, otherwise emit all properties"),
                new Option<EngineClass>("--engine-type", ArgumentUtilities.ParseEngineClass, isDefault: true, "Engine type.")
                    .AddSuggestions("tq", "tqit", "tqae", "gd"),
            };
            command.Handler = CommandHandler.Create((Commands.CreateCommand cmd) => cmd.Run());
            return command;
        }
    }
}
