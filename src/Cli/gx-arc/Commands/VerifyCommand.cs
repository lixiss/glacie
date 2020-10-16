using System.Buffers;
using System.IO;

using Glacie.ChecksumAlgorithms;
using Glacie.CommandLine.IO;
using Glacie.CommandLine.UI;
using Glacie.Data.Arc;

namespace Glacie.Cli.Arc.Commands
{
    internal sealed class VerifyCommand : Command
    {
        private const int BufferSize = 16 * 1024;
        private const string SuccessPrefix = "[ ok ]"; // "  \u221A"
        private const string FailurePrefix = "[fail]"; // "  X"

        public string ArchivePath { get; }

        public VerifyCommand(string archive)
        {
            ArchivePath = archive;
        }

        public int Run()
        {
            if (File.Exists(ArchivePath))
            {
                bool success;
                {
                    success = VerifyArchive(ArchivePath);
                }

                if (success)
                {
                    Console.Out.WriteLine(string.Format("{0} {1}", SuccessPrefix, ArchivePath));
                    return 0;
                }
                else
                {
                    Console.Error.WriteLine(string.Format("{0} {1}", FailurePrefix, ArchivePath));
                    return 1;
                }
            }
            else
            {
                Console.Error.WriteLine("File does not exist: " + ArchivePath);
                return 1;

                // TODO: Globbing support?

                //var files = GlobExpressions.Glob.Files(".", Path, GlobExpressions.GlobOptions.MatchFullPath);

                //var atLeastOneFile = false;
                //var hasErrors = false;
                //foreach (var file in files)
                //{
                //    atLeastOneFile = true;

                //    bool success;
                //    {
                //        using var consoleProgress = new ConsoleProgressBar();
                //        success = VerifyArchive(file, progress: consoleProgress);
                //    }

                //    if (success)
                //    {
                //        Console.Out.WriteLine(string.Format("{0} {1}", SuccessPrefix, file));
                //    }
                //    else
                //    {
                //        hasErrors = true;
                //        Console.Error.WriteLine(string.Format("{0} {1}", FailurePrefix, file));
                //    }
                //}
                //// TODO: Glob?

                //if (!atLeastOneFile)
                //{
                //    Console.Error.WriteLine("File does not exist: " + Path);
                //    // File does not exist: ./Creatures.arc1
                //    return 1;
                //}
                //else
                //{
                //    return hasErrors ? 1 : 0;
                //}
            }
        }

        private bool VerifyArchive(string path)
        {
            using var progress = StartProgress();
            progress.SetValueUnit(ProgressValueUnit.Bytes);
            progress.ShowRate = true;
            progress.ShowValue = true;
            progress.ShowMaximumValue = true;
            progress.ShowElapsedTime = true;
            progress.ShowRemainingTime = true;
            progress.SetTitle("Verifying...");

            using var archive = ArcArchive.Open(path);
            return VerifyArchive(archive, progress);
        }

        private bool VerifyArchive(ArcArchive archive, ProgressView? progress)
        {
            if (progress != null)
            {
                long totalLength = 0;
                foreach (var entry in archive.SelectAll())
                {
                    totalLength += entry.Length;
                }
                progress?.AddMaximumValue(totalLength);
            }

            var numberOfErrors = 0;
            foreach (var entry in archive.SelectAll())
            {
                progress?.SetMessage(entry.Name);

                var result = VerifyEntry(entry, progress);

                if (result)
                {
                    // Console.Out.WriteLine("[ OK ] " + entry.Name);
                }
                else
                {
                    Console.Error.WriteLine(string.Format("Invalid hash for entry: {0}", entry.Name));
                    numberOfErrors++;
                }
            }
            return numberOfErrors == 0;
        }

        private static bool VerifyEntry(ArcArchiveEntry entry, IIncrementalProgress<long>? progress)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            try
            {
                using var inputStream = entry.Open();
                var hash = new Adler32();
                while (true)
                {
                    var bytesInBuffer = inputStream.Read(buffer);
                    if (bytesInBuffer == 0) break;
                    hash.ComputeHash(buffer, 0, bytesInBuffer);

                    progress?.AddValue(bytesInBuffer);
                }
                return entry.Hash == hash.Hash;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
