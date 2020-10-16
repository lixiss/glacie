using System.Collections.Generic;
using System.IO;

using Glacie.CommandLine.IO;
using Glacie.Data.Arc;

namespace Glacie.Cli.Arc.Commands
{
    internal sealed class RemoveCommand : Command
    {
        public string ArchivePath { get; }
        public IReadOnlyList<string> EntryNames { get; }
        public bool SafeWrite { get; }
        public bool PreserveCase { get; }

        public RemoveCommand(
            string archive,
            List<string> entry,
            bool safeWrite,
            bool preserveCase)
        {
            ArchivePath = archive;
            EntryNames = entry;
            SafeWrite = safeWrite;
            PreserveCase = preserveCase;
        }

        public int Run()
        {
            if (!File.Exists(ArchivePath))
            {
                throw new CliErrorException("File does not exist: " + ArchivePath);
            }

            Check.That(EntryNames.Count > 0);

            var removedCount = 0;
            var skippedCount = 0;

            {
                using var progress = StartProgress();
                progress.Title = "Removing...";

                using var archive = ArcArchive.Open(ArchivePath, new ArcArchiveOptions
                {
                    Mode = ArcArchiveMode.Update,
                    SafeWrite = SafeWrite,
                });
                progress.AddMaximumValue(EntryNames.Count);

                // TODO: Glob support needed.
                foreach (var entryName in EntryNames)
                {
                    progress.Message = entryName;

                    // TODO: Use common helper.
                    var normalizedEntryName = entryName.Replace('\\', '/');
                    if (!PreserveCase)
                    {
                        normalizedEntryName = normalizedEntryName.ToLowerInvariant();
                    }

                    if (archive.TryGet(normalizedEntryName, out var entry))
                    {
                        var realEntryName = entry.Name;

                        entry.Remove();
                        removedCount++;

                        Console.Out.WriteLine("Removed: {0}", realEntryName);
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
            }

            Console.Out.WriteLine("Removed: {0} files removed, {1} files skipped", removedCount, skippedCount);

            return 0;
        }
    }
}
