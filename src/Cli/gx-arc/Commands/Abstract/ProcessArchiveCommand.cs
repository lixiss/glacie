using System.IO;

using Glacie.CommandLine.IO;
using Glacie.CommandLine.UI;
using Glacie.Data.Arc;

namespace Glacie.Cli.Arc.Commands
{
    internal abstract class ProcessArchiveCommand : Command
    {
        public string ArchivePath { get; }

        public bool SafeWrite { get; }

        protected ProcessArchiveCommand(
            string archive,
            bool safeWrite)
        {
            ArchivePath = archive;
            SafeWrite = safeWrite;
        }

        public int Run()
        {
            if (!File.Exists(ArchivePath))
            {
                throw new CliErrorException("File does not exist: " + ArchivePath);
            }

            ProcessArchive(ArchivePath);

            // Console.Out.WriteLine(string.Format("{0} {1}", "[done]", Path));
            return 0;
        }

        private void ProcessArchive(string path)
        {
            long inputLength;
            long outputLength;
            bool wasModified;

            if (SafeWrite)
            {
                MemoryStream archiveStream;
                {
                    using var progress = StartProgress();
                    progress.Title = "Reading...";
                    //progressBar.Message = path;
                    archiveStream = FileUtilities.ReadFile(path, progress);
                }
                inputLength = archiveStream.Length;

                {
                    using var progress = StartProgress();
                    using var archive = ArcArchive.Open(archiveStream, CreateArchiveOptions(ArcArchiveMode.Update));
                    ProcessArchive(archive, progress);
                    wasModified = archive.Modified;
                }
                outputLength = archiveStream.Length;

                if (wasModified)
                {
                    using var progress = StartProgress();
                    progress.Title = "Writing...";
                    //progressBar.Message = path;
                    FileUtilities.ReplaceFileTo(path, archiveStream, "gx-arc", progress);
                }
            }
            else
            {
                using var progress = StartProgress();

                inputLength = new FileInfo(path).Length;

                {
                    using var archive = ArcArchive.Open(path, CreateArchiveOptions(ArcArchiveMode.Update));
                    ProcessArchive(archive, progress);
                    wasModified = archive.Modified;
                }

                outputLength = new FileInfo(path).Length;
            }

            var lengthRatio = (double)outputLength / inputLength;

            if (wasModified)
            {
                Console.Out.WriteLine("Optimized:");
                Console.Out.WriteLine("   Input Archive Size: {0:N0} bytes", inputLength);
                Console.Out.WriteLine("  Output Archive Size: {0:N0} bytes ({1:N1}%)", outputLength, lengthRatio * 100.0);
            }
            else
            {
                Console.Out.WriteLine("Optimized: Nothing to optimize.");
            }
        }

        protected abstract void ProcessArchive(ArcArchive archive, ProgressView? progress);
    }
}
