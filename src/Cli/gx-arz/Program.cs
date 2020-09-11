using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading;

using Glacie.Data.Arz;
using Glacie.Data.Compression;
using Glacie.Logging;

using static Glacie.Cli.ArgumentUtilities;

namespace Glacie.Cli.Arz
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
            var rootCommand = new RootCommand("Glacie Database Tool")
            {
                CreateListCommand(),
                CreateInfoCommand(),
                CreateVerifyCommand(),
                CreateExtractCommand(),

                CreateOptimizeCommand(),

                CreateBuildCommand(Commands.BuildCommand.Mode.Build),
                CreateBuildCommand(Commands.BuildCommand.Mode.Add),
                CreateBuildCommand(Commands.BuildCommand.Mode.Update),
                CreateBuildCommand(Commands.BuildCommand.Mode.Replace),
                CreateBuildCommand(Commands.BuildCommand.Mode.RemoveMissing),

                // TODO: Remove command

                CreateDumpStringTableCommand(),

            };
            rootCommand.AddGlobalOption(new Option<bool?>("--use-libdeflate",
                "Use libdeflate for zlib compression"));
            rootCommand.AddGlobalOption(new Option<LogLevel>("--log-level", () => LogLevel.Information, "Logging level"));
            return new CommandLineBuilder(rootCommand);
        }

        private static Command CreateListCommand()
        {
            var command = new Command("list", "List contents of database")
            {
                new Argument<string>("database", "Path to database (.arz) file")
            };
            command.AddAlias("ls");
            command.Handler = CommandHandler.Create((Commands.ListCommand cmd) => cmd.Run());
            return command;
        }

        private static Command CreateDumpStringTableCommand()
        {
            var command = new Command("dump-string-table", "List contents of database's string table")
            {
                new Argument<string>("database", "Path to database (.arz) file")
            };
            command.AddAlias("dump-strtable");
            command.Handler = CommandHandler.Create((Commands.DumpStringTableCommand cmd) => cmd.Run());
            return command;
        }

        private static Command CreateInfoCommand()
        {
            var command = new Command("info", "Show information about database")
            {
                new Argument<string>("database", "Path to database (.arz) file")
            };
            command.Handler = CommandHandler.Create((Commands.InfoCommand cmd) => cmd.Run());
            return command;
        }

        private static Command CreateVerifyCommand()
        {
            var command = new Command("verify", "Verify database integrity")
            {
                new Argument<string>("database", "Path to database (.arz) file")
            };
            command.Handler = CommandHandler.Create((Commands.VerifyCommand cmd) => cmd.Run());
            return command;
        }

        private static Command CreateExtractCommand()
        {
            var command = new Command("extract", "Extract contents of database")
            {
                new Argument<string>("database", "Path to database (.arz) file"),
                new Option<string>("--output", () => ".", "Path to output directory"),
                new Option<bool>(new []{ "--set-last-write-time", "--set-timestamp" },
                    () => true,
                    "Restore last write time file attribute from database"),
            };
            command.Handler = CommandHandler.Create((Commands.ExtractCommand cmd) => cmd.Run());
            return command;
        }

        private static Command CreateOptimizeCommand()
        {
            var command = new Command("optimize", "Optimize database")
            {
                new Argument<string>("database", "Path to database (.arz) file"),
                new Option<string>("--output", "Path to output database file. If not specified, input database will be replaced"),
                new Option<bool>("-Odbrref", () => false, "Optimize .dbr file references (normalize dbr file name strings which may result into smaller string table)"),
                new Option<bool>("-Otplref", () => false, "Optimize .tpl file references (normalize tpl file name strings)"),
                new Option<bool>("-Orstrtable", () => false, "Rebuild string table, so if there is exist unused strings, they will not be included in output file"),
                new Option<bool>("-Oostrtable", () => false, "Optimize string table, so it contents will be stable, with possibility of better overall compression"),
                new Option<bool>("-Orecompress", () => false, "Force recompress all records"),
                new Option<bool>("--repack", () => false, "Enable all optimizations"),
                new Option<CompressionLevel>("--compression-level",
                    ParseArzCompressionLevel, isDefault: true,
                    description: "Compression level. Valid values from 1..12 from 'fastest' to 'maximum'."),
                new Option<bool>("--checksum", () => true, "Calculate checksums"),
                new Option<bool>(new[] {"-mp", "--parallelize" }, () => true, "Use parallel decompression/compression"),
                new Option<int>(new[] { "-mdop", "--max-degree-of-parallelism" }, () => -1, "Max degree of parallelism. By default it is equal to number of logical processors."),
                new Option<bool?>("--safe-write", () => false, "When enabled, perform all operations over database in-memory, and write database content to disk only when done. This requires more memory, but database will not be corrupted if you break or cancel operation.\nWhen disabled - perform in-place database updates. By default it is enabled if you doesn't specify output path, and disabled if you specify it."),
            };
            command.Handler = CommandHandler.Create((Commands.OptimizeCommand cmd) => cmd.Run());
            return command;
        }

        // Build
        // Update command

        private static Command CreateBuildCommand(Commands.BuildCommand.Mode mode)
        {
            string name;
            string description;
            switch (mode)
            {
                case Commands.BuildCommand.Mode.Build:
                    name = "build";
                    description = "Build database from dbr files. This command is equivalent to update & remove-misssing comand.";
                    break;

                case Commands.BuildCommand.Mode.Add:
                    name = "add";
                    description = "Add records to database. If a file is already in the database it will not be added.";
                    break;

                case Commands.BuildCommand.Mode.Replace:
                    name = "replace";
                    description = "Replace records in database. If a file is already in the database it will be overwritten.";
                    break;

                case Commands.BuildCommand.Mode.Update:
                    name = "update";
                    description = "Update records in database. Files will only be added if they are newer than those already in the database.";
                    break;

                case Commands.BuildCommand.Mode.RemoveMissing:
                    name = "remove-missing";
                    description = "Remove the records that are not in the specified inputs.";
                    break;

                default: throw Error.Argument(nameof(mode));
            }

            var command = new Command(name, description)
            {
                new Argument<string>("database", "Path to database (.arz) file"),
                new Argument<List<string>>("input", "Input .dbr files or directories.")
                {
                    Arity = ArgumentArity.OneOrMore,
                },
                // TODO: I want to specify two definition sources: one database, and second templates. Then they should be merged somehow.
                new Option<string>(new[]{ "--definitions", "--templates" }, "Path to record definitions (templates). This value might be: directory with .tpl files, path to .arc or .zip file with template files or path to .arz database which will be used to as source of ephemeral record definitions. When this option is not specified, then <database> argument is used as record definition source."),
                new Option<string>("--output", "Path to output database file. If not specified, input database will be replaced"),
                new Option<string>("--relative-to", () => ".", "Specifies base directory (record names will be generated relative to this path)."),
                new Option<ArzFileFormat>("--format",
                    ParseArzFileFormat, isDefault: true,
                    "Archive file format. Non-automatic value required when you create new archive. Valid values are game type tags.")
                    .AddSuggestions("auto", "tq", "tqit", "tqae", "gd"),
                new Option<CompressionLevel>("--compression-level",
                    ParseArzCompressionLevel, isDefault: true,
                    description: "Compression level. Valid values from 1..12 from 'fastest' to 'maximum'."),
                new Option<bool>("--checksum", () => true, "Calculate checksums"),
                // TODO: mp/parallelize is global option
                // TODO: mdop/max-degree-of-parallelism is global option
                new Option<bool>(new[] { "-mp", "--parallelize" }, () => true, "Use parallel decompression/compression"),
                new Option<int>(new[] { "-mdop", "--max-degree-of-parallelism" }, () => -1, "Max degree of parallelism. By default it is equal to number of logical processors."),
                new Option<bool>("--safe-write", () => false, "When enabled, perform all operations over database in-memory, and write database content to disk only when done. This requires more memory, but database will not be corrupted if you break or cancel operation.\nWhen disabled - perform in-place database updates. By default it is enabled if you doesn't specify output path, and disabled if you specify it."),
                new Option<bool>("--preserve-case", () => false, "Record names by default is case-insensitive and stored in lower-case. This option enables creating records with preserved case."),
            };
            command.Handler = CommandHandler.Create((Commands.BuildCommand cmd) => cmd.Run(mode));
            return command;
        }
    }
}
