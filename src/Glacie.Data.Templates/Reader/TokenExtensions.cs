using Glacie.Diagnostics;

namespace Glacie.Data.Templates
{
    internal static class TokenExtensions
    {
        public static Location ToLocation(this in Token token, in Path1 path)
        {
            return Location.File(path.ToString(), token.LineNumber, token.LinePosition);
        }
    }
}
