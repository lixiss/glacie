using Glacie.Localization;

namespace Glacie.Discovery_Modules
{
    public sealed class ResourceBundleInfo
    {
        public ResourceBundleKind Kind { get; }
        public short Priority { get; }
        public string PhysicalPath { get; }
        public string RelativePath { get; }
        public string Prefix { get; }
        public Language Language => LanguageSymbol.Language;
        public LanguageSymbol LanguageSymbol { get; }

        // TODO: add special types, e.g. Text is most likely handled in special way,
        // as they doesn't expose resource files directly, or Video bundles

        internal ResourceBundleInfo(
            string physicalPath,
            string relativePath,
            ResourceBundleKind kind,
            short priority,
            string prefix,
            LanguageSymbol languageSymbol)
        {
            Check.Argument.NotNullNorEmpty(physicalPath, nameof(physicalPath));
            Check.Argument.NotNullNorEmpty(relativePath, nameof(relativePath));

            PhysicalPath = physicalPath;
            RelativePath = relativePath;
            Kind = kind;
            Priority = priority;
            Prefix = prefix;
            LanguageSymbol = languageSymbol;
        }
    }
}
