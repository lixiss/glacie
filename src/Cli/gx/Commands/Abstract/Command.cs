using System;

using Glacie.Cli.Logging;
using Glacie.CommandLine;
using Glacie.CommandLine.UI;
using Glacie.Logging;

namespace Glacie.Cli.Commands
{
    internal abstract class Command : IDisposable
    {
        private Logger? _log;

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
