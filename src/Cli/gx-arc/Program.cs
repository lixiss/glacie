using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Globalization;
using System.Linq;
using System.Threading;

using Glacie.Data.Arc;
using Glacie.Data.Compression;

using static Glacie.Cli.ArgumentUtilities;

namespace Glacie.Cli.Arc
{
    // TODO: [ ] List - extended

    // TODO: (VeryLow) (gx-arc) How to pass custom objects like ProgressConsole? into commands? We should register it and as service, or similar. (But command line is weird.)
    // TODO: (Medium) (gx-arc) Internal options: header-area, chunk-size / etc...
    // TODO: (Medium) (gx-arc) Glob support. Glob might be useful in other projects too (ArzDatabase/Context), so it is probably better to have it as library.

    internal static class Program
    {
        private static int Main(string[] args)
        {
            using var terminal = TerminalFactory.GetDefaultTerminal();

            var result = BuildCommandLine()
                .UseCli(terminal)
                .Build()
                .Invoke(args);

            return result;
        }

        private static CommandLineBuilder BuildCommandLine()
        {
            var rootCommand = new RootCommand("Glacie Archive Tool")
            {
                // Keep list sorted.
                CreateListCommand(),
                CreateInfoCommand(),
                CreateVerifyCommand(),

                CreateExtractCommand(),

                CreateOptimizeCommand(),
                CreateRebuildCommand(),

                CreateAddCommand(),
                CreateReplaceCommand(),
                CreateUpdateCommand(),
                CreateRemoveMissingCommand(),
                CreateRemoveCommand(),
            };
            rootCommand.AddGlobalOption(new Option<bool?>("--use-libdeflate",
                "Use libdeflate for zlib compression"));
            return new CommandLineBuilder(rootCommand);
        }

        private static Command CreateListCommand()
        {
            var command = new Command("list", "Lists contents of archive.")
            {
                new Argument<string>("archive", "Path to ARC file.")
                  // TODO: Use validator like ExistingOnly like for FileInfo?
            };
            command.AddAlias("ls");
            command.Handler = CommandHandler.Create((Commands.ListCommand cmd) => cmd.Run());
            return command;
        }

        private static Command CreateExtractCommand()
        {
            var command = new Command("extract", "Extract contents of archive.")
            {
                new Argument<string>("archive", "Path to ARC file."),
                new Option<string>("--output", () => ".", "Path to output directory."),
                new Option<bool>(new [] {"--set-last-write-time", "--set-timestamp" }, () => true, "Restore last write time file attribute from archive."),
            };
            command.Handler = CommandHandler.Create((Commands.ExtractCommand cmd) => cmd.Run());
            return command;
        }

        private static Command CreateInfoCommand()
        {
            var command = new Command("info", "Technical information about archive.")
            {
                new Argument<string>("archive", "Path to ARC file.")
            };
            command.Handler = CommandHandler.Create((Commands.InfoCommand cmd) => cmd.Run());
            return command;
        }

        private static Command CreateVerifyCommand()
        {
            var command = new Command("verify", "Test integrity of archive.")
            {
                new Argument<string>("archive", "Path to ARC file."),
            };
            command.AddAlias("test");
            command.Handler = CommandHandler.Create((Commands.VerifyCommand cmd) => cmd.Run());
            return command;
        }

        private static Command CreateOptimizeCommand()
        {
            var command = new Command("optimize", "Optimize archive.")
            {
                new Argument<string>("archive", "Path to ARC file."),
                // TODO: Use common options
                new Option<bool>("--repack", () => false,  "Recompress data chunks. This doesn't turn uncompressed entries into compressed."),
                new Option<CompressionLevel>("--compression-level",
                    ParseArcCompressionLevel, isDefault: true,
                    description: "Compression level. Valid values from 0 or 'no' (no compression), 1..12 from 'fastest' to 'maximum'."),
                new Option<bool>("--defragment", () => true,  "Defragment archive."),
                new Option<bool>("--safe-write", () => true,  "When enabled, perform all operations over archive in-memory, and write archive content to disk only when done. This requires more memory, but archive will not be corrupted if you break or cancel operation.\nWhen disabled - perform in-place archive updates."),
            };
            command.Handler = CommandHandler.Create((Commands.OptimizeCommand cmd) => cmd.Run());
            return command;
        }

        private static Command CreateRebuildCommand()
        {
            var command = new Command("rebuild", "Rebuild archive.")
            {
                new Argument<string>("archive", "Path to ARC file."),
                new Option<ArcFileFormat>("--format",
                    ParseArcFileFormat, isDefault: true,
                    "Archive file format. Non-automatic value required when you create new archive. Valid values are 1 or 3 or use game type tags.")
                    .AddSuggestions("auto", "1", "tq", "tqit", "tqae", "3", "gd"),
                new Option<CompressionLevel>("--compression-level",
                    ParseArcCompressionLevel, isDefault: true,
                    description: "Compression level. Valid values from 0 or 'no' (no compression), 1..12 from 'fastest' to 'maximum'."),
                new Option<bool>("--preserve-store", () => true, "Preserve uncompressed entries."),
                new Option<bool>("--safe-write", () => true, "When enabled, avoid to perform destructive operations."),
                new Option<int>("--header-area-size", "Size of header area. Default is 2048."),
                new Option<int>("--chunk-size", "Chunk length. Default is 262144."),
            };
            command.Handler = CommandHandler.Create((Commands.RebuildCommand cmd) => cmd.Run());
            return command;
        }


        private static Command CreateAddCommand()
        {
            var command = new Command("add", "Add a file or directory. If a file is already in the archive it will not be added.")
            {
                new Argument<string>("archive", "Path to ARC file."),
                new Argument<List<string>>("input", "Input files or directories.")
                {
                    Arity = ArgumentArity.OneOrMore,
                },
                new Option<string>("--relative-to", () => ".", "Specifies base directory (entry names will be generated relative to this path)."),
                new Option<ArcFileFormat>("--format",
                    ParseArcFileFormat, isDefault: true,
                    "Archive file format. Non-automatic value required when you create new archive. Valid values are 1 or 3 or use game type tags.")
                    .AddSuggestions("auto", "1", "tq", "tqit", "tqae", "3", "gd"),
                new Option<CompressionLevel>("--compression-level",
                    ParseArcCompressionLevel, isDefault: true,
                    description: "Compression level. Valid values from 0 or 'no' (no compression), 1..12 from 'fastest' to 'maximum'."),
                new Option<bool>("--safe-write", () => true, "When enabled, avoid to perform destructive operations."),
                new Option<bool>("--preserve-case", () => false, "Entry names by default is case-insensitive. This option enables creating archives with preserved case."),
                new Option<int>("--header-area-size", "Size of header area. Default is 2048."),
                new Option<int>("--chunk-size", "Chunk length. Default is 262144."),
            };
            command.Handler = CommandHandler.Create((Commands.AddCommand cmd) => cmd.Run());
            return command;
        }

        private static Command CreateReplaceCommand()
        {
            var command = new Command("replace", "Replace a file or directory. If a file is already in the archive it will be overwritten.")
            {
                new Argument<string>("archive", "Path to ARC file."),
                new Argument<List<string>>("input", "Input files or directories.")
                {
                    Arity = ArgumentArity.OneOrMore,
                },
                new Option<string>("--relative-to", () => ".", "Specifies base directory (entry names will be generated relative to this path)."),
                new Option<ArcFileFormat>("--format",
                    ParseArcFileFormat, isDefault: true,
                    "Archive file format. Non-automatic value required when you create new archive. Valid values are 1 or 3 or use game type tags.")
                    .AddSuggestions("auto", "1", "tq", "tqit", "tqae", "3", "gd"),
                new Option<CompressionLevel>("--compression-level",
                    ParseArcCompressionLevel, isDefault: true,
                    description: "Compression level. Valid values from 0 or 'no' (no compression), 1..12 from 'fastest' to 'maximum'."),
                new Option<bool>("--safe-write", () => true, "When enabled, avoid to perform destructive operations."),
                new Option<bool>("--preserve-case", () => false, "Entry names by default is case-insensitive. This option enables creating archives with preserved case."),
                new Option<int>("--header-area-size", "Size of header area. Default is 2048."),
                new Option<int>("--chunk-size", "Chunk length. Default is 262144."),
            };
            command.Handler = CommandHandler.Create((Commands.ReplaceCommand cmd) => cmd.Run());
            return command;
        }


        private static Command CreateUpdateCommand()
        {
            var command = new Command("update", "Update a file or directory. Files will only be added if they are newer than those already in the archive.")
            {
                new Argument<string>("archive", "Path to ARC file."),
                new Argument<List<string>>("input", "Input files or directories.")
                {
                    Arity = ArgumentArity.OneOrMore,
                },
                new Option<string>("--relative-to", () => ".", "Specifies base directory (entry names will be generated relative to this path)."),
                new Option<ArcFileFormat>("--format",
                    ParseArcFileFormat, isDefault: true,
                    "Archive file format. Non-automatic value required when you create new archive. Valid values are 1 or 3 or use game type tags.")
                    .AddSuggestions("auto", "1", "tq", "tqit", "tqae", "3", "gd"),
                new Option<CompressionLevel>("--compression-level",
                    ParseArcCompressionLevel, isDefault: true,
                    description: "Compression level. Valid values from 0 or 'no' (no compression), 1..12 from 'fastest' to 'maximum'."),
                new Option<bool>("--safe-write", () => true, "When enabled, avoid to perform destructive operations."),
                new Option<bool>("--preserve-case", () => false, "Entry names by default is case-insensitive. This option enables creating archives with preserved case."),
                new Option<int>("--header-area-size", "Size of header area. Default is 2048."),
                new Option<int>("--chunk-size", "Chunk length. Default is 262144."),
            };
            command.Handler = CommandHandler.Create((Commands.UpdateCommand cmd) => cmd.Run());
            return command;
        }

        // TODO: It accept only existing archive, so remove unrelated options, and enfroce this in command.
        private static Command CreateRemoveMissingCommand()
        {
            var command = new Command("remove-missing", "Remove the files that are not in the specified inputs.")
            {
                new Argument<string>("archive", "Path to ARC file."),
                new Argument<List<string>>("input", "Input files or directories.")
                {
                    Arity = ArgumentArity.OneOrMore,
                },
                new Option<string>("--relative-to", () => ".", "Specifies base directory (entry names will be generated relative to this path)."),
                new Option<ArcFileFormat>("--format",
                    ParseArcFileFormat, isDefault: true,
                    "Archive file format. Non-automatic value required when you create new archive. Valid values are 1 or 3 or use game type tags.")
                    .AddSuggestions("auto", "1", "tq", "tqit", "tqae", "3", "gd"),
                new Option<CompressionLevel>("--compression-level",
                    ParseArcCompressionLevel, isDefault: true,
                    description: "Compression level. Valid values from 0 or 'no' (no compression), 1..12 from 'fastest' to 'maximum'."),
                new Option<bool>("--safe-write", () => true, "When enabled, avoid to perform destructive operations."),
                new Option<bool>("--preserve-case", () => false, "Entry names by default is case-insensitive and stored in lower-case. This option enables creating archives with preserved case."),
                new Option<int>("--header-area-size", "Size of header area. Default is 2048."),
                new Option<int>("--chunk-size", "Chunk length. Default is 262144."),
            };
            command.Handler = CommandHandler.Create((Commands.RemoveMissingCommand cmd) => cmd.Run());
            return command;
        }

        private static Command CreateRemoveCommand()
        {
            var command = new Command("remove", "Remove a file from the archive.")
            {
                new Argument<string>("archive", "Path to ARC file."),
                new Argument<List<string>>("entry", "Entry names to remove.")
                {
                    Arity = ArgumentArity.OneOrMore,
                },
                new Option<bool>("--safe-write", () => true, "When enabled, avoid to perform destructive operations."),
                new Option<bool>("--preserve-case", () => false, "Entry names by default is case-insensitive. This option enables creating archives with preserved case."),
            };
            command.Handler = CommandHandler.Create((Commands.RemoveCommand cmd) => cmd.Run());
            return command;
        }

    }
}
