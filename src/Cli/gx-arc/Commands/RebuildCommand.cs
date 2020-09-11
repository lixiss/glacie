using System.Buffers;
using System.IO;

using Glacie.CommandLine.UI;
using Glacie.Data.Arc;
using Glacie.Data.Compression;

namespace Glacie.Cli.Arc.Commands
{
    internal sealed class RebuildCommand : Command
    {
        public string ArchivePath { get; }
        public ArcFileFormat Format { get; }
        public CompressionLevel CompressionLevel { get; }
        public bool PreserveStore { get; }
        public bool SafeWrite { get; }
        public int? HeaderAreaSize { get; }
        public int? ChunkSize { get; }

        public RebuildCommand(
            string archive,
            ArcFileFormat format,
            CompressionLevel compressionLevel,
            bool preserveStore,
            bool safeWrite,
            int? headerAreaSize = null,
            int? chunkSize = null)
        {
            ArchivePath = archive;
            Format = format;
            CompressionLevel = compressionLevel;
            PreserveStore = preserveStore;
            SafeWrite = safeWrite;
            HeaderAreaSize = headerAreaSize;
            ChunkSize = chunkSize;
        }

        public int Run()
        {
            if (!File.Exists(ArchivePath))
            {
                throw new CliErrorException("File does not exist: " + ArchivePath);
            }

            RebuildArchive(ArchivePath);
            return 0;
        }

        private void RebuildArchive(string path)
        {
            var archiveOptions = CreateArchiveOptions(ArcArchiveMode.Create);
            archiveOptions.CompressionLevel = CompressionLevel;
            archiveOptions.SafeWrite = false;
            archiveOptions.HeaderAreaLength = HeaderAreaSize;
            archiveOptions.ChunkLength = ChunkSize;
            
            using var progress = StartProgress();
            progress.SetValueUnit(Glacie.CommandLine.UI.ProgressValueUnit.Bytes);
            progress.ShowRate = true;
            progress.ShowValue = true;
            progress.ShowMaximumValue = true;
            progress.ShowElapsedTime = true;
            progress.ShowRemainingTime = true;

            using var inputArchive = ArcArchive.Open(ArchivePath, ArcArchiveMode.Read);

            if (Format == default)
            {
                archiveOptions.Format = inputArchive.GetFormat();
            }
            else
            {
                archiveOptions.Format = Format;
            }

            long totalLength = 0;
            foreach (var entry in inputArchive.GetEntries())
            {
                totalLength += entry.Length;
            }

            progress.SetMaximumValue(totalLength);

            if (true || SafeWrite)
            {
                var outputStream = new MemoryStream();
                using var outputArchive = ArcArchive.Open(outputStream, archiveOptions);

                CopyEntries(inputArchive, outputArchive, progress);

                outputArchive.Dispose();
                inputArchive.Dispose();

                // TODO: spawn new progress
                FileUtilities.ReplaceFileTo(ArchivePath, outputStream, "gx-arc", progress);
            }
            else
            {
                // TODO: implement writing into different file
                throw Error.NotImplemented();
            }
        }

        private void CopyEntries(ArcArchive inputArchive, ArcArchive outputArchive, ProgressView? progress)
        {
            foreach (var inputEntry in inputArchive.GetEntries())
            {
                progress?.SetMessage(inputEntry.Name);

                CompressionLevel? compressionLevel = PreserveStore && (int)inputEntry.EntryType == 1
                        ? CompressionLevel.NoCompression
                        : (CompressionLevel?)null;

                var outputEntry = outputArchive.CreateEntry(inputEntry.Name);

                {
                    using var inputStream = inputEntry.Open();
                    using var outputStream = outputEntry.OpenWrite(compressionLevel);
                    CopyStream(inputStream, outputStream, progress);
                }

                outputEntry.Timestamp = inputEntry.Timestamp;
            }
        }

        private void CopyStream(Stream inputStream, Stream outputStream, IIncrementalProgress<long>? progress)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
            while (true)
            {
                var bytesRead = inputStream.Read(buffer);
                if (bytesRead == 0) break;
                outputStream.Write(buffer, 0, bytesRead);

                progress?.AddValue(bytesRead);
            }
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
