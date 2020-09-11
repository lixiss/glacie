using System;
using System.Text;

using Glacie.Cli.Arz.Dbr;
using Glacie.CommandLine.IO;
using Glacie.Data.Arz;

using IO = System.IO;

namespace Glacie.Cli.Arz.Commands
{
    internal sealed class ExtractCommand : DatabaseCommand
    {
        private string Database { get; }
        private string Output { get; }
        private bool SetLastWriteTime { get; }

        private DbrRenderer _dbrRenderer = new DbrRenderer();

        public ExtractCommand(string database, string output, bool setLastWriteTime = false)
        {
            Database = database;
            Output = output;
            SetLastWriteTime = setLastWriteTime;
        }

        public int Run()
        {
            ValidateOutputPath(Output);

            using ArzDatabase database = ReadDatabase(Database,
                CreateReaderOptions(ArzReadingMode.Full));

            using var progress = StartProgress("Extracting...");
            progress.AddMaximumValue(database.Count);
            progress.SetValueUnit("files", scale: true);
            progress.ShowRate = true;
            progress.ShowElapsedTime = true;
            progress.ShowRemainingTime = true;
            progress.ShowValue = true;
            progress.ShowMaximumValue = true;

            var written = 0;
            foreach (var record in database.GetAll())
            {
                progress.Message = record.Name;

                // Assume what validated record name is safe to use in path.
                RecordNameUtilities.Validate(record.Name);

                var content = RenderDbrContent(record);

                // Normalize to used in path.
                var normalizedRecordName = RecordNameUtilities.NormalizeToFileSystemPath(record.Name);
                var outputPath = IO.Path.Combine(Output, normalizedRecordName);

                var directoryPath = IO.Path.GetDirectoryName(outputPath);
                IO.Directory.CreateDirectory(directoryPath);

                DateTimeOffset? lastWriteTime = SetLastWriteTime ? GetRecordLastWriteTime(record) : null;

                WriteDbrContent(outputPath, content, lastWriteTime);
                written++;

                progress.AddValue(1);
                // Console.Out.WriteLine(record.Name);
            }

            Console.Out.WriteLine("Extracted: {0} file(s)", written);

            return 0;
        }

        private string RenderDbrContent(ArzRecord record)
        {
            return _dbrRenderer.Render(record);
        }

        private void WriteDbrContent(string path, string content, DateTimeOffset? lastWriteTime)
        {
            IO.File.WriteAllText(path, content, DbrUtility.Encoding);

            if (lastWriteTime != null)
            {
                try
                {
                    new IO.FileInfo(path).LastWriteTimeUtc = lastWriteTime.Value.UtcDateTime;
                }
                catch
                {
                    Console.Error.WriteLine("error: Failed to set last write time for file: " + path);
                    throw;
                }
            }
        }

        private void ValidateOutputPath(string path)
        {
            if (IO.File.Exists(path))
            {
                throw new CliErrorException("Output path should be a directory: " + path);
            }
        }

        private static DateTimeOffset? GetRecordLastWriteTime(ArzRecord record)
        {
            if (record.TryGetLastWriteTime(out var result))
            {
                return result;
            }
            return null;
        }
    }
}
