namespace Glacie.CommandLine.Rendering
{
    public interface ITerminal : IConsole
    {
        // Might implement some terminal-related API, like control cursor / etc,
        // but this is better to model from/to VirtualTerminal (from ANSI codes).
    }
}
