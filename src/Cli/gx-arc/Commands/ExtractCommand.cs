using System;
using System.Buffers;
using System.IO;

using Glacie.CommandLine.IO;
using Glacie.Data.Arc;

namespace Glacie.Cli.Arc.Commands
{
    internal sealed class ExtractCommand : Command
    {
        public string ArchivePath { get; }

        public string OutputPath { get; }

        public bool SetLastWriteTime { get; }

        public ExtractCommand(
            string archive,
            string output,
            bool setLastWriteTime)
        {
            ArchivePath = archive;
            OutputPath = output;
            SetLastWriteTime = setLastWriteTime;
        }

        public int Run()
        {
            if (!File.Exists(ArchivePath))
            {
                throw new CliErrorException("File does not exist: " + ArchivePath);
            }

            if (File.Exists(OutputPath))
            {
                throw new CliErrorException("Output path should be a directory: " + OutputPath);
            }

            ExtractArchive(ArchivePath);
            return 0;
        }

        private void ExtractArchive(string path)
        {
            using var archive = ArcArchive.Open(path);
            ExtractArchive(archive);
        }

        private void ExtractArchive(ArcArchive archive)
        {
            using var progress = StartProgress();
            progress.Title = "Extracting...";
            progress.SetValueUnit(Glacie.CommandLine.UI.ProgressValueUnit.Bytes);
            progress.ShowRate = true;
            progress.ShowElapsedTime = true;
            progress.ShowRemainingTime = true;
            progress.ShowValue = true;
            progress.ShowMaximumValue = true;

            long totalLength = 0;
            foreach (var entry in archive.SelectAll())
            {
                totalLength += entry.Length;
            }
            progress?.SetMaximumValue(totalLength);

            int filesWritten = 0;
            foreach (var entry in archive.SelectAll())
            {
                progress?.SetMessage(entry.Name);

                // Assuming what validated entry name is safe to use in path
                // (with combining).
                EntryNameUtilities.Validate(entry.Name);

                var outputPath = System.IO.Path.Combine(OutputPath, entry.Name);

                var directoryPath = System.IO.Path.GetDirectoryName(outputPath);
                Directory.CreateDirectory(directoryPath);

                using var inputStream = entry.Open();

                DateTimeOffset? lastWriteTime = SetLastWriteTime ? GetEntryLastWriteTime(entry) : null;
                WriteFile(outputPath, inputStream, lastWriteTime, progress);
                filesWritten++;
            }

            Console.Out.WriteLine("Extracted: {0} file(s)", filesWritten);
        }

        private void WriteFile(string path, Stream stream, DateTimeOffset? lastWriteTime, IIncrementalProgress<long>? progress)
        {
            {
                using var outputStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);

                var buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);

                while (true)
                {
                    var bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    outputStream.Write(buffer, 0, bytesRead);

                    progress?.AddValue(bytesRead);
                }

                ArrayPool<byte>.Shared.Return(buffer);
            }

            if (lastWriteTime != null)
            {
                try
                {
                    new FileInfo(path).LastWriteTimeUtc = lastWriteTime.Value.UtcDateTime;
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("error: Failed to set last write time for file: " + path);
                    Console.Error.WriteLine(e.ToString());
                }
            }
        }

        private static DateTimeOffset? GetEntryLastWriteTime(ArcArchiveEntry entry)
        {
            if (entry.TryGetLastWriteTime(out var result))
            {
                return result;
            }
            return null;
        }
    }
}
