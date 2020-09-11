using Glacie.CommandLine.UI;

namespace Glacie.Cli
{
    public static class TerminalFactory
    {
        private static Terminal? s_terminal = null;

        public static Terminal GetDefaultTerminal()
        {
            if (s_terminal != null) return s_terminal;
            return s_terminal = new Terminal();
        }
    }
}
