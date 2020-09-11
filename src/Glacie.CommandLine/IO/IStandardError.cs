namespace Glacie.CommandLine.IO
{
    public interface IStandardError
    {
        IStandardStreamWriter Error { get; }

        bool IsErrorRedirected { get; }
    }
}
