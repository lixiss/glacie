using System.Text;

namespace Glacie.Cli.Arz.Dbr
{
    internal static class DbrUtility
    {
        // Encoding in which all characters in range 0..255 mapped unchanged.
        private static readonly Encoding s_encoding = Encoding.GetEncoding("iso-8859-1");

        public static Encoding Encoding => s_encoding;
    }
}
