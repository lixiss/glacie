using Glacie.CommandLine.IO;

namespace Glacie.CommandLine
{
    public interface IConsole :
        IStandardOut,
        IStandardError,
        IStandardIn
    { }
}
