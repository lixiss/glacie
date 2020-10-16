namespace Glacie.GX1.Discovery
{
    public sealed class ResourceBundleInfo
    {
        public ResourceBundleKind Kind { get; }
        public short Priority { get; }
        public string Path { get; }
        public string Prefix { get; }
        public string? Language { get; }

        // TODO: add special types, e.g. Text is most likely handled in special way,
        // as they doesn't expose resource files directly

        internal ResourceBundleInfo(
            ResourceBundleKind kind,
            short priority,
            string path,
            string prefix,
            string? language)
        {
            Kind = kind;
            Priority = priority;
            Path = path;
            Prefix = prefix;
            Language = language;
        }
    }
}
