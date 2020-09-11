using System.Collections.Generic;

using Glacie.CommandLine.IO;
using Glacie.Data.Arc;
using Glacie.Data.Compression;

namespace Glacie.Cli.Arc.Commands
{
    internal sealed class AddCommand : ProcessInputFilesCommand
    {
        private int _addedCount;
        private int _skippedCount;

        public AddCommand(
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

        protected override string GetProcessInputFilesTitle() => "Adding files...";

        protected override void ProcessInputFile(ArcArchive archive, InputFileInfo fileInfo, IIncrementalProgress<long>? progress)
        {
            if (!archive.Exists(fileInfo.EntryName))
            {
                var entry = archive.CreateEntry(fileInfo.EntryName);
                {
                    using var entryStream = entry.OpenWrite();
                    CopyFileToStream(fileInfo.FileName, entryStream, progress);
                }
                entry.LastWriteTime = fileInfo.LastWriteTime;
                _addedCount++;

                // TODO: Console.Out.WriteLine("Added: " + inputFileInfo.EntryName);
            }
            else
            {
                _skippedCount++;
            }
        }

        protected override void PostProcessArchive(ArcArchive archive, IIncrementalProgress<long>? progress)
        {
            Console.Out.WriteLine("Added: {0} files added, {1} files skipped", _addedCount, _skippedCount);
        }
    }
}
