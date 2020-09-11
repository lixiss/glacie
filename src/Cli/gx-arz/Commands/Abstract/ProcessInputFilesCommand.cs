using System;
using System.Buffers;
using System.Collections.Generic;

using Glacie.CommandLine.IO;
using Glacie.CommandLine.UI;
using Glacie.Data.Arz;
using Glacie.Data.Arz.Infrastructure;
using Glacie.Data.Compression;

using IO = System.IO;

namespace Glacie.Cli.Arz.Commands
{
    internal abstract class ProcessInputFilesCommand : DatabaseCommand
    {
        public string Database { get; }
        public string? OutputDatabase { get; }
        public IReadOnlyList<string> InputFiles { get; }
        public string RelativeToPath { get; }
        public ArzFileFormat Format { get; private set; }
        public CompressionLevel CompressionLevel { get; }
        public bool Checksum { get; }
        public bool SafeWrite { get; }
        public bool PreserveCase { get; }

        protected ProcessInputFilesCommand(
            string database,
            List<string> input,
            string relativeTo,
            ArzFileFormat format,
            CompressionLevel compressionLevel,
            bool checksum,
            bool safeWrite,
            bool preserveCase,
            string? output = null)
        {
            Database = database;
            InputFiles = input;
            RelativeToPath = relativeTo;
            Format = format;
            CompressionLevel = compressionLevel;
            Checksum = checksum;
            SafeWrite = safeWrite;
            PreserveCase = preserveCase;
            OutputDatabase = output;
        }

        protected abstract string GetProcessInputFilesTitle();
        protected abstract void ProcessInputFile(ArzDatabase database, InputFileInfo fileInfo, IIncrementalProgress<long>? progress);
        protected abstract void PostProcess(ArzDatabase database, IIncrementalProgress<long>? progress);
        protected abstract void OnInputDatabaseOpened(ArzDatabase database);

        protected int RunProcessInputFiles()
        {
            Check.True(InputFiles.Count > 0);

            using var database = OpenDatabase(Database);

            if (Format == ArzFileFormat.Automatic)
            {
                database.GetContext().TryInferFormat(out var format);
                if (format.Complete)
                {
                    Format = format;
                }
                else
                {
                    throw new CliErrorException("Can't infer file format. You must specify it manually.");
                }
            }


            OnInputDatabaseOpened(database);

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

                    var fileInfo = new IO.FileInfo(inputFile);
                    var fileLength = fileInfo.Length;

                    totalInputLength += fileLength;

                    var info = new InputFileInfo
                    {
                        FileName = inputFile,
                        Length = fileLength,
                        RecordName = CreateRecordName(inputFile),
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
                        if (!hashSet.Add(x.RecordName))
                        {
                            throw Error.InvalidOperation("Invalid casing. Final entry names must be unique.");
                        }
                    }
                }

                // Validate Record Names
                foreach (var x in inputFileInfos)
                {
                    RecordNameUtilities.Validate(x.RecordName);
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
                progress.SetMaximumValue(totalInputLength);

                foreach (var inputFileInfo in inputFileInfos)
                {
                    progress.Message = inputFileInfo.FileName;

                    ProcessInputFile(database, inputFileInfo, progress);
                }

                PostProcess(database, progress);
            }

            {
                using var progress = StartProgress("Writing...");
                progress.SetValueUnit("records", true);
                progress.ShowRate = true;
                progress.ShowElapsedTime = true;
                progress.ShowRemainingTime = true;
                // progress.ShowTotalTime = true;
                progress.ShowValue = true;
                progress.ShowMaximumValue = true;

                var writerOptions = CreateWriterOptions(
                    changesOnly: false,
                    compressionLevel: CompressionLevel,
                    format: Format,
                    inferRecordClass: false);

                long outputLength;
                var outputPath = OutputDatabase ?? Database;
                var shouldReplaceInputDatabase = string.IsNullOrEmpty(OutputDatabase);

                if (SafeWrite)
                {
                    var memoryStream = new IO.MemoryStream();
                    ArzWriter.Write(memoryStream, database, writerOptions, progress);
                    outputLength = memoryStream.Length;
                    database.Dispose();

                    FileUtilities.ReplaceFileTo(outputPath, memoryStream, "gx-arz", progress: null);
                }
                else if (shouldReplaceInputDatabase)
                {
                    var tempPath = FileUtilities.GetTemporaryFileNameForReplace(outputPath, "gx-arz");
                    try
                    {
                        ArzWriter.Write(tempPath, database, writerOptions, progress);
                        outputLength = new IO.FileInfo(tempPath).Length;
                        database.Dispose();
                    }
                    catch
                    {
                        try { FileUtilities.DeleteFile(tempPath); } catch { }
                        throw;
                    }
                    FileUtilities.ReplaceOrMoveFileTo(outputPath, tempPath);
                }
                else
                {
                    ArzWriter.Write(outputPath, database, writerOptions, progress);
                    outputLength = new IO.FileInfo(outputPath).Length;
                    database.Dispose();
                }
            }

            Console.Out.WriteLine("Done");

            return 0;
        }

        private ArzDatabase OpenDatabase(string path)
        {
            if (IO.File.Exists(path))
            {
                using var progress = StartProgress("Reading...");

                return ArzDatabase.Open(path, CreateReaderOptions(ArzReadingMode.Raw));
            }
            return ArzDatabase.Create();
        }

        private string CreateRecordName(string path)
        {
            var relativePath = IO.Path.GetRelativePath(RelativeToPath, path);
            if (Format.StandardPathSeparator)
            {
                relativePath = relativePath.Replace('\\', '/');
            }
            else
            {
                relativePath = relativePath.Replace('/', '\\');
            }
            if (!PreserveCase) relativePath = relativePath.ToLowerInvariant();
            return relativePath;
        }


        private IEnumerable<string> GetAllInputs()
        {
            foreach (var inputPath in InputFiles)
            {
                foreach (var x in GetAllInputsForPath(inputPath, "*.dbr"))
                {
                    yield return x;
                }
            }
        }

        private IEnumerable<string> GetAllInputsForPath(string path, string searchPattern)
        {
            if (IO.File.Exists(path))
            {
                yield return path;
            }
            else if (IO.Directory.Exists(path))
            {
                foreach (var x in IO.Directory.EnumerateFiles(path, searchPattern, IO.SearchOption.AllDirectories))
                {
                    yield return x;
                }
            }
            else
            {
                // TODO: Glob?
                throw new IO.FileNotFoundException("Input file not found.", path);
            }
        }

        protected struct InputFileInfo
        {
            public string FileName;
            public long Length;
            public string RecordName;
            public DateTimeOffset LastWriteTime;
        }
    }
}
