namespace Glacie.CommandLine.IO
{
    public interface IStandardIn
    {
        // TODO: IInputStreamReader In
        bool IsInputRedirected { get; }
    }
}
