using System;
using System.Collections.Generic;

using Glacie.CommandLine.IO;
using Glacie.Data.Arc;
using Glacie.Data.Compression;

namespace Glacie.Cli.Arc.Commands
{
    internal sealed class RemoveMissingCommand : ProcessInputFilesCommand
    {
        private readonly HashSet<string> _entryNamesToKeep = new HashSet<string>(StringComparer.Ordinal);

        public RemoveMissingCommand(
            string archive,
            List<string> input,
            string relativeTo,
            ArcFileFormat format,
            CompressionLevel compressionLevel,
            bool safeWrite,
            bool preserveCase,
            int? headerAreaSize = null,
            int? chunkSize = null)
            : base(archive, input, relativeTo, format, compressionLevel,
                  safeWrite, preserveCase, headerAreaSize, chunkSize)
        { }

        protected override string GetProcessInputFilesTitle() => "Removing missing files...";

        protected override void ProcessInputFile(ArcArchive archive, InputFileInfo fileInfo, IIncrementalProgress<long>? progress)
        {
            if (archive.Exists(fileInfo.EntryName))
            {
                _entryNamesToKeep.Add(fileInfo.EntryName);
            }
        }

        protected override void PostProcessArchive(ArcArchive archive, IIncrementalProgress<long>? progress)
        {
            var removedCount = 0;

            progress?.AddMaximumValue(archive.Count);

            foreach (var entry in archive.SelectAll())
            {
                if (!_entryNamesToKeep.Contains(entry.Name))
                {
                    var entryName = entry.Name;

                    entry.Remove();

                    Console.Out.WriteLine("Removing: {0}", entryName);
                    removedCount++;
                }

                progress?.AddValue(1);
            }

            Console.Out.WriteLine("Removed: {0} files removed", removedCount);
        }
    }
}
