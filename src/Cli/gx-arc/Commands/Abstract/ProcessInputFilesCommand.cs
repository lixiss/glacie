using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

using Glacie.CommandLine.UI;
using Glacie.Data.Arc;
using Glacie.Data.Compression;

namespace Glacie.Cli.Arc.Commands
{
    internal abstract class ProcessInputFilesCommand : Command
    {
        public string ArchivePath { get; }
        public IReadOnlyList<string> InputFiles { get; }
        public string RelativeToPath { get; }
        public ArcFileFormat Format { get; }
        public CompressionLevel CompressionLevel { get; }
        public bool SafeWrite { get; }
        public bool PreserveCase { get; }
        public int? HeaderAreaSize { get; }
        public int? ChunkSize { get; }

        protected ProcessInputFilesCommand(
            string archive,
            List<string> input,
            string relativeTo,
            ArcFileFormat format,
            CompressionLevel compressionLevel,
            bool safeWrite,
            bool preserveCase,
            int? headerAreaSize = null,
            int? chunkSize = null)
        {
            ArchivePath = archive;
            InputFiles = input;
            RelativeToPath = relativeTo;
            Format = format;
            CompressionLevel = compressionLevel;
            SafeWrite = safeWrite;
            PreserveCase = preserveCase;
            HeaderAreaSize = headerAreaSize;
            ChunkSize = chunkSize;
        }

        protected abstract string GetProcessInputFilesTitle();
        protected abstract void ProcessInputFile(ArcArchive archive, InputFileInfo fileInfo, IIncrementalProgress<long>? progress);
        protected abstract void PostProcessArchive(ArcArchive archive, IIncrementalProgress<long>? progress);

        public int Run()
        {
            Check.True(InputFiles.Count > 0);

            var archiveOptions = CreateArchiveOptions();
            archiveOptions.Format = Format;
            archiveOptions.CompressionLevel = CompressionLevel;
            archiveOptions.SafeWrite = SafeWrite;
            archiveOptions.HeaderAreaLength = HeaderAreaSize;
            archiveOptions.ChunkLength = ChunkSize;

            var archiveExist = File.Exists(ArchivePath);
            archiveOptions.Mode = archiveExist ? ArcArchiveMode.Update : ArcArchiveMode.Create;

            long totalInputLength = 0;
            var inputFileInfos = new List<InputFileInfo>();
            {
                using var progress = StartProgress();
                progress.Title = "Discovering...";
                progress.SetValueUnit("files", scale: true);
                progress.ShowRate = true;
                progress.ShowValue = true;

                var inputs = GetAllInputs();
                foreach (var inputFile in inputs)
                {
                    progress.Message = inputFile;

                    var fileInfo = new FileInfo(inputFile);

                    totalInputLength += fileInfo.Length;

                    var info = new InputFileInfo
                    {
                        FileName = inputFile,
                        EntryName = CreateEntryName(inputFile),
                        LastWriteTime = new DateTimeOffset(fileInfo.LastWriteTimeUtc),
                    };
                    inputFileInfos.Add(info);

                    progress.AddValue(1);
                }

                if (!PreserveCase)
                {
                    var hashSet = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var x in inputFileInfos)
                    {
                        if (!hashSet.Add(x.EntryName))
                        {
                            throw Error.InvalidOperation("Invalid casing. Final entry names must be unique.");
                        }
                    }
                }

                // Validate Entry Names
                foreach (var x in inputFileInfos)
                {
                    EntryNameUtilities.Validate(x.EntryName);
                }
            }

            {
                using var progress = StartProgress();
                progress.SetValueUnit(ProgressValueUnit.Bytes);
                progress.ShowRate = true;
                progress.ShowValue = true;
                progress.ShowMaximumValue = true;
                progress.ShowElapsedTime = true;
                progress.ShowRemainingTime = true;
                progress.Title = GetProcessInputFilesTitle();

                using var archive = ArcArchive.Open(ArchivePath, archiveOptions);
                progress.SetMaximumValue(totalInputLength);

                foreach (var inputFileInfo in inputFileInfos)
                {
                    progress.Message = inputFileInfo.FileName;

                    ProcessInputFile(archive, inputFileInfo, progress);
                }

                PostProcessArchive(archive, progress);
            }

            return 0;
        }

        private string CreateEntryName(string path)
        {
            var relativePath = System.IO.Path.GetRelativePath(RelativeToPath, path);
            relativePath = relativePath.Replace('\\', '/');
            if (!PreserveCase) relativePath = relativePath.ToLowerInvariant();
            return relativePath;
        }


        private IEnumerable<string> GetAllInputs()
        {
            foreach (var inputPath in InputFiles)
            {
                foreach (var x in GetAllInputsForPath(inputPath))
                {
                    yield return x;
                }
            }
        }

        private IEnumerable<string> GetAllInputsForPath(string path)
        {
            if (File.Exists(path))
            {
                yield return path;
            }
            else if (Directory.Exists(path))
            {
                foreach (var x in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
                {
                    yield return x;
                }
            }
            else
            {
                // TODO: Glob?
                throw new FileNotFoundException("Input file not found.", path);
            }
        }

        protected void CopyFileToStream(string path, Stream outputStream, IIncrementalProgress<long>? progress)
        {
            using var inputStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);

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

        protected struct InputFileInfo
        {
            public string FileName;
            public string EntryName;
            public DateTimeOffset LastWriteTime;
        }
    }
}
