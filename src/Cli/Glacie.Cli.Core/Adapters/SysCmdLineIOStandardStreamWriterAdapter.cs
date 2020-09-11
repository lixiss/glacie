using Glacie.CommandLine.IO;

namespace Glacie.Cli.Adapters
{
    internal sealed class SysCmdLineIOStandardStreamWriterAdapter
        : System.CommandLine.IO.IStandardStreamWriter
    {
        private readonly IStandardStreamWriter _target;

        public SysCmdLineIOStandardStreamWriterAdapter(IStandardStreamWriter target)
        {
            _target = target;
        }

        public void Write(string value)
        {
            _target.Write(value);
        }
    }
}
