using System;
using System.Collections.Generic;
using System.IO;

using Glacie.CommandLine.UI;
using Glacie.Data.Arz;
using Glacie.Data.Arz.Infrastructure;
using Glacie.Data.Compression;

namespace Glacie.Cli.Arz.Commands
{
    internal abstract class DatabaseCommand : Command
    {
        protected ArzReaderOptions CreateReaderOptions(ArzReadingMode mode)
        {
            return new ArzReaderOptions
            {
                Mode = mode,
                UseLibDeflate = UseLibDeflate,

                Format = ArzFileFormat.Automatic,

                Multithreaded = Parallelize ?? true,
                MaxDegreeOfParallelism = MaxDegreeOfParallelism ?? -1,

                CloseUnderlyingStream = false,
            };
        }

        protected ArzWriterOptions CreateWriterOptions(
            bool? changesOnly = null,
            ArzFileFormat? format = null,
            bool? inferRecordClass = null,
            bool? optimizeStringTable = null,
            bool? forceCompression = null,
            CompressionLevel? compressionLevel = null,
            bool? computeChecksum = null)
        {
            return new ArzWriterOptions
            {
                ChangesOnly = changesOnly ?? false,

                Multithreaded = Parallelize ?? true,
                MaxDegreeOfParallelism = MaxDegreeOfParallelism ?? -1,

                UseLibDeflate = UseLibDeflate,

                Format = format ?? ArzFileFormat.Automatic,

                InferRecordClass = inferRecordClass ?? true,

                OptimizeStringTable = optimizeStringTable ?? false,

                ForceCompression = forceCompression ?? false,

                CompressionLevel = compressionLevel ?? CompressionLevel.Maximum,

                ComputeChecksum = computeChecksum ?? true,
            };
        }

        protected ArzDatabase ReadDatabase(string path, ArzReaderOptions options, ProgressView? progress = null)
        {
            using var localProgress = StartChildProgress(progress);

            if (!File.Exists(path))
            {
                throw new CliErrorException("File does not exist: " + path);
            }

            return ArzDatabase.Open(path, options);
        }
    }
}
