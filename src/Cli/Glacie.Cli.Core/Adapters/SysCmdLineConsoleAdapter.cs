using Glacie.CommandLine;

namespace Glacie.Cli.Adapters
{
    internal sealed class SysCmdLineConsoleAdapter : System.CommandLine.IConsole
    {
        private readonly IConsole _target;
        private readonly SysCmdLineIOStandardStreamWriterAdapter _out;
        private readonly SysCmdLineIOStandardStreamWriterAdapter _error;

        public SysCmdLineConsoleAdapter(IConsole target)
        {
            _target = target;
            _out = new SysCmdLineIOStandardStreamWriterAdapter(target.Out);
            _error = new SysCmdLineIOStandardStreamWriterAdapter(target.Error);
        }

        public System.CommandLine.IO.IStandardStreamWriter Out => _out;

        public bool IsOutputRedirected => _target.IsOutputRedirected;

        public System.CommandLine.IO.IStandardStreamWriter Error => _error;

        public bool IsErrorRedirected => _target.IsErrorRedirected;

        public bool IsInputRedirected => _target.IsInputRedirected;
    }
}
