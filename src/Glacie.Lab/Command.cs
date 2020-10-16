using Glacie.Cli.Logging;
using Glacie.CommandLine;
using Glacie.CommandLine.UI;
using Glacie.Logging;

namespace Glacie.Lab
{
    public abstract class Command
    {
        private Logger? _log;

        public static T Create<T>(IConsole console) where T : Command, new()
        {
            var command = new T();
            command.Terminal = console as Terminal;
            command.Console = console;
            command.LogLevel = LogLevel.Trace;
            return command;
        }

        private Terminal? Terminal { get; set; }

        protected IConsole Console { get; private set; } = default!;

        private LogLevel LogLevel { get; set; }

        protected Logger Log => _log ?? (_log = LoggerFactory.CreateLogger(LogLevel, Console));


        protected abstract void RunCore();

        public void Run()
        {
            RunCore();
        }


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
