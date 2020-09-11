using Glacie.CommandLine.UI;
using Glacie.Data.Arc;
using Glacie.Data.Compression;

namespace Glacie.Cli.Arc.Commands
{
    internal sealed class OptimizeCommand : ProcessArchiveCommand
    {
        private readonly bool _defragment;
        private readonly bool _repack;
        private readonly CompressionLevel _compressionLevel;

        public OptimizeCommand(
            string archive,
            bool repack,
            CompressionLevel compressionLevel,
            bool defragment,
            bool safeWrite)
            : base(archive, safeWrite)
        {
            _repack = repack;
            _compressionLevel = compressionLevel;
            _defragment = defragment;
        }

        protected override void ProcessArchive(ArcArchive archive, ProgressView? progress)
        {
            if (_defragment)
            {
                if (progress != null)
                {
                    progress.SetValueUnit("it", scale: true);
                    progress.ShowRate = false;
                    progress.ShowValue = false;
                    progress.ShowMaximumValue = false;
                    progress.ShowElapsedTime = true;
                    progress.ShowRemainingTime = true;
                    progress.Restart();
                }

                progress?.SetTitle("Defragmenting...");
                archive.Defragment(progress);
            }

            // Repack/Compacting
            if (progress != null)
            {
                progress.SetValueUnit("it", scale: true);
                progress.ShowRate = false;
                progress.ShowValue = false;
                progress.ShowMaximumValue = false;
                progress.ShowElapsedTime = true;
                progress.ShowRemainingTime = true;
                progress.Restart();
            }

            if (_repack)
            {
                progress?.SetTitle("Repacking...");
                archive.Repack(_compressionLevel, progress);
            }
            else
            {
                progress?.SetTitle("Compacting...");
                archive.Compact(progress);
            }
        }
    }
}
