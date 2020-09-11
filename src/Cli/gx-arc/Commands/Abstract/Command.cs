using Glacie.CommandLine;
using Glacie.CommandLine.UI;
using Glacie.Data.Arc;

namespace Glacie.Cli.Arc.Commands
{
    internal abstract class Command
    {
        // Injected.
        public bool? UseLibDeflate { get; internal set; } = null;

        // Injected by command factory.
        public Terminal Terminal { get; internal set; } = default!;

        protected IConsole Console => Terminal;

        protected ProgressView StartProgress(string? title = null)
        {
            var progressView = new ProgressView();
            progressView.Title = title;
            if (Terminal != null && !Terminal.IsOutputRedirected)
            {
                Terminal.Add(progressView);
            }
            progressView.Start();
            return progressView;
        }

        protected ArcArchiveOptions CreateArchiveOptions(ArcArchiveMode mode = ArcArchiveMode.Read)
        {
            return new ArcArchiveOptions
            {
                Mode = mode,
                UseLibDeflate = UseLibDeflate,
            };
        }
    }
}
