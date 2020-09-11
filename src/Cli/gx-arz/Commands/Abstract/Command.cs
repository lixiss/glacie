using System;

using Glacie.CommandLine;
using Glacie.CommandLine.UI;
using Glacie.Logging;
using Glacie.Cli.Logging;

namespace Glacie.Cli.Arz.Commands
{
    internal abstract class Command : IDisposable
    {
        private Logger? _log;

        // Injected.
        public bool? UseLibDeflate { get; internal set; } = null;

        public bool? Parallelize { get; internal set; } = null;

        public int? MaxDegreeOfParallelism { get; internal set; } = null;

        public LogLevel LogLevel { get; internal set; } = 0;

        // Injected by command factory.
        public Terminal Terminal { get; internal set; } = default!;

        protected IConsole Console => Terminal;

        protected Logger Log => _log ?? (_log = LoggerFactory.CreateLogger(LogLevel, Console));

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }

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

        protected ProgressView? StartChildProgress(ProgressView? parent = null, string? title = null)
        {
            if (parent == null) return StartProgress(title);
            else return new ProgressView();
        }
    }
}
