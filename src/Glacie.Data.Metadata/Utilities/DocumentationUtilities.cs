namespace Glacie.Metadata
{
    internal static class DocumentationUtilities
    {
        public static string? Normalize(string? value)
        {
            value = value?.Trim();
            if (string.IsNullOrWhiteSpace(value)) return null;
            return value;
        }
    }
}
