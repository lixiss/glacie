namespace Glacie.Data.Metadata.V1
{
    internal static class DescriptionUtilities
    {
        public static string? Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return value.Trim();
        }
    }
}
