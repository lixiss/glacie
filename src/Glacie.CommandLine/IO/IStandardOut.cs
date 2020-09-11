namespace Glacie.CommandLine.IO
{
    public interface IStandardOut
    {
        IStandardStreamWriter Out { get; }

        bool IsOutputRedirected { get; }
    }
}
