using Glacie.Localization;

namespace Glacie.Discovery_Modules.Builder
{
    internal struct ResourceBundleInfoBuilder
    {
        private ResourceBundleKind _kind;
        private short _priority;
        private string _path;
        private string _prefix;
        private LanguageSymbol _languageSymbol;

        public ResourceBundleInfoBuilder(ResourceBundleKind kind, short priority, string path, string prefix, LanguageSymbol languageSymbol)
        {
            _kind = kind;
            _priority = priority;
            _path = path;
            _prefix = prefix;
            _languageSymbol = languageSymbol;
        }

        public ResourceBundleInfo Build(string? physicalPath)
        {
            return new ResourceBundleInfo(
                physicalPath: PathUtilities.GetPhysicalPath(_path),
                relativePath: PathUtilities.GetRelativePath(physicalPath, _path),
                kind: _kind,
                priority: _priority,
                prefix: _prefix,
                languageSymbol: _languageSymbol);
        }
    }
}
