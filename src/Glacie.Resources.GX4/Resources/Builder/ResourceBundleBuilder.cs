using Glacie.Localization;
using Glacie.Resources;

namespace Glacie.Modules.Builder
{
    public sealed class ResourceBundleBuilder : IResourceBundleInfo
    {
        private readonly ResourceBundleKind _kind;
        private readonly string _physicalPath;
        private readonly short _priority;
        private readonly string _prefix;
        private readonly LanguageSymbol _language;

        public ResourceBundleBuilder(ResourceBundleKind kind, string physicalPath, short priority, string prefix, LanguageSymbol language)
        {
            _kind = kind;
            _physicalPath = physicalPath;
            _priority = priority;
            _prefix = prefix;
            _language = language;
        }

        public ResourceBundleKind Kind => _kind;

        public string? PhysicalPath => _physicalPath;

        public short Priority => _priority;

        public string Prefix => _prefix;

        public LanguageSymbol Language => _language;

        public ResourceBundle Build()
        {
            return new ResourceBundle(_kind, _physicalPath, _priority, _prefix, _language);
        }
    }
}
