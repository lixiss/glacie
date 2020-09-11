namespace Glacie.CommandLine.UI
{
    internal static class StringUtilities
    {

        /// <summary>
        /// Create excerpt string with number of characters.
        /// </summary>
        public static string? Excerpt(string? value, int count)
        {
            if (string.IsNullOrEmpty(value) || value.Length < count)
                return value;

            return value.Substring(0, count - 3) + "...";
        }
    }
}
