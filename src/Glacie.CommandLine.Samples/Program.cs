using Glacie.CommandLine.UI;

namespace Glacie.CommandLine.Samples
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            using var terminal = new Terminal();
            new ProgressViewSample(terminal).Run();
            return;
        }
    }
}
