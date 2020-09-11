using System.Collections.Generic;

using Glacie.CommandLine.IO;
using Glacie.Data.Arc;
using Glacie.Data.Compression;

namespace Glacie.Cli.Arc.Commands
{
    internal sealed class ReplaceCommand : ProcessInputFilesCommand
    {
        private int _addedCount;
        private int _updatedCount;

        public ReplaceCommand(
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
        {
        }

        protected override string GetProcessInputFilesTitle() => "Replacing files...";

        protected override void ProcessInputFile(ArcArchive archive, InputFileInfo fileInfo, IIncrementalProgress<long>? progress)
        {
            if (archive.TryGetEntry(fileInfo.EntryName, out var entry))
            {
                {
                    using var entryStream = entry.OpenWrite();
                    CopyFileToStream(fileInfo.FileName, entryStream, progress);
                }
                entry.LastWriteTime = fileInfo.LastWriteTime;

                // TODO: Console.Out.WriteLine("Added: " + inputFileInfo.EntryName);
                _updatedCount++;
            }
            else
            {
                entry = archive.CreateEntry(fileInfo.EntryName);
                {
                    using var entryStream = entry.OpenWrite();
                    CopyFileToStream(fileInfo.FileName, entryStream, progress);
                }
                entry.LastWriteTime = fileInfo.LastWriteTime;

                // TODO: Console.Out.WriteLine("Added: " + inputFileInfo.EntryName);
                _addedCount++;
            }
        }

        protected override void PostProcessArchive(ArcArchive archive, IIncrementalProgress<long>? progress)
        {
            Console.Out.WriteLine("Replaced: {0} files added, {1} files updated", _addedCount, _updatedCount);
        }
    }
}
