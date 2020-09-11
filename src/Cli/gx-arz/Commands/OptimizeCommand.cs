using System;
using System.Text;

using Glacie.Cli;
using Glacie.CommandLine.IO;
using Glacie.Data.Arz;
using Glacie.Data.Compression;
using Glacie.Cli.Arz.Processors;

using IO = System.IO;

namespace Glacie.Cli.Arz.Commands
{
    internal sealed class OptimizeCommand : DatabaseCommand
    {
        private string Database { get; }
        private string? OutputDatabase { get; }
        private bool OptimizeDbrRef { get; }
        private bool OptimizeTplRef { get; }
        private bool RebuildStrTable { get; }
        private bool OptimizeStrTable { get; }
        private bool OptimizeRecompress { get; }
        private bool Repack { get; }
        private CompressionLevel CompressionLevel { get; }
        private bool ComputeChecksum { get; }
        private bool SafeWrite { get; } = false;
        private bool ShouldReplaceInputDatabase => OutputDatabase == null;

        public OptimizeCommand(string database,
            bool oDbrRef,
            bool oTplRef,
            bool oRStrTable,
            bool oOStrTable,
            bool oRecompress,
            bool repack,
            CompressionLevel compressionLevel,
            bool checksum = true,
            bool safeWrite = false,
            string? output = null)
        {
            Database = database;
            Repack = repack;
            OptimizeDbrRef = oDbrRef || repack;
            OptimizeTplRef = oTplRef || repack;
            RebuildStrTable = oRStrTable || repack;
            OptimizeStrTable = oOStrTable || repack;
            OptimizeRecompress = oRecompress || repack;
            CompressionLevel = compressionLevel;
            OutputDatabase = output;
            ComputeChecksum = checksum;
            SafeWrite = safeWrite;
        }

        public int Run()
        {
            if (OutputDatabase == "")
            {
                throw new CliErrorException("Output database path should not be empty. Omit option completely, or specify path.");
            }

            if (OutputDatabase != null)
            {
                ValidateOutputDatabasePath(OutputDatabase);
            }

            // todo: determine reading mode
            using ArzDatabase database = ReadDatabase(Database,
                CreateReaderOptions(ArzReadingMode.Full));

            var inputLength = new IO.FileInfo(Database).Length;

            var rebuildStringTable = RebuildStrTable;
            if (OptimizeDbrRef)
            {
                using var progress = StartProgress("Optimizing: DBR file references...");
                progress.SetValueUnit("records", scale: true);
                progress.ShowRate = true;
                progress.ShowElapsedTime = true;
                progress.ShowRemainingTime = true;
                progress.ShowValue = true;
                progress.ShowMaximumValue = true;

                var result = new ArzDbrRefOptimizer(database).Run(progress);

                Console.Out.WriteLine("Optimized: DBR file references");
                Console.Out.WriteLine("  # of Remapped Strings: {0:N0}", result.NumberOfRemappedStrings);
                Console.Out.WriteLine("  Estimated Size Reduction: {0:N0} bytes", result.EstimatedSizeReduction);
                // Console.Out.WriteLine("  Completed In: {0:N0}ms", result.CompletedIn.TotalMilliseconds);

                if (result.NumberOfRemappedStrings > 0)
                {
                    rebuildStringTable |= true;
                }
            }

            if (OptimizeTplRef)
            {
                using var progress = StartProgress("Optimizing: TPL file references...");
                progress.SetValueUnit("records", scale: true);
                progress.ShowRate = true;
                progress.ShowElapsedTime = true;
                progress.ShowRemainingTime = true;
                progress.ShowValue = true;
                progress.ShowMaximumValue = true;

                var result = new ArzTplRefOptimizer(database).Run(progress);

                Console.Out.WriteLine("Optimized: TPL file references");
                Console.Out.WriteLine("  # of Remapped Strings: {0:N0}", result.NumberOfRemappedStrings);
                Console.Out.WriteLine("  Estimated Size Reduction: {0:N0} bytes", result.EstimatedSizeReduction);
                // Console.Out.WriteLine("  Completed In: {0:N0}ms", result.CompletedIn.TotalMilliseconds);

                if (result.NumberOfRemappedStrings > 0)
                {
                    rebuildStringTable |= true;
                }
            }

            long outputLength;
            {
                var outputPath = ShouldReplaceInputDatabase ? Database : OutputDatabase;
                Check.That(!string.IsNullOrEmpty(outputPath));

                using var progress = StartProgress("Writing...");
                progress.SetValueUnit("records", true);
                progress.ShowRate = true;
                progress.ShowElapsedTime = true;
                progress.ShowRemainingTime = true;
                // progress.ShowTotalTime = true;
                progress.ShowValue = true;
                progress.ShowMaximumValue = true;

                var writerOptions = CreateWriterOptions(
                    rebuildStringTable: rebuildStringTable,
                    optimizeStringTable: OptimizeStrTable,
                    forceCompression: OptimizeRecompress,
                    compressionLevel: CompressionLevel,
                    computeChecksum: ComputeChecksum);

                // TODO: add verbose mode
                if (false)
                {
                    Console.Out.WriteLine("Writing Database With Options:");
                    Console.Out.WriteLine("  ChangesOnly = {0}", writerOptions.ChangesOnly);
                    Console.Out.WriteLine("  Multithreaded = {0}", writerOptions.Multithreaded);
                    Console.Out.WriteLine("  MaxDegreeOfParallelism = {0}", writerOptions.MaxDegreeOfParallelism);
                    Console.Out.WriteLine("  UseLibDeflate = {0}", writerOptions.UseLibDeflate);
                    Console.Out.WriteLine("  Format = {0}", writerOptions.Format);
                    Console.Out.WriteLine("  InferRecordClass = {0}", writerOptions.InferRecordClass);
                    Console.Out.WriteLine("  RebuildStringTable = {0}", writerOptions.RebuildStringTable);
                    Console.Out.WriteLine("  OptimizeStringTable = {0}", writerOptions.OptimizeStringTable);
                    Console.Out.WriteLine("  ForceCompression = {0}", writerOptions.ForceCompression);
                    Console.Out.WriteLine("  CompressionLevel = {0}", writerOptions.CompressionLevel);
                    Console.Out.WriteLine("  ComputeChecksum = {0}", writerOptions.ComputeChecksum);
                    Console.Out.WriteLine();
                }

                if (SafeWrite)
                {
                    var memoryStream = new IO.MemoryStream(checked((int)inputLength));
                    ArzWriter.Write(memoryStream, database, writerOptions, progress);
                    outputLength = memoryStream.Length;
                    database.Dispose();

                    FileUtilities.ReplaceFileTo(outputPath, memoryStream, "gx-arz", null);
                }
                else if (ShouldReplaceInputDatabase)
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

                Console.Out.WriteLine("Written: {0}", outputPath);
            }

            var lengthRatio = (double)outputLength / inputLength;

            Console.Out.WriteLine("Optimized:");
            Console.Out.WriteLine("   Input Database Size: {0:N0} bytes", inputLength);
            Console.Out.WriteLine("  Output Database Size: {0:N0} bytes ({1:N1}%)", outputLength, lengthRatio * 100.0);

            return 0;
        }

        private void ValidateOutputDatabasePath(string path)
        {
            if (IO.Directory.Exists(path))
            {
                throw new CliErrorException("Output database path should be a file: " + path);
            }
        }
    }
}
