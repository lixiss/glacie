using System;

using Glacie.Localization;

namespace Glacie.Resources
{
    // TODO: ResourceBundle should be able to configured directly with provider
    public sealed class ResourceBundle : IResourceBundleInfo
    {
        private readonly ResourceBundleKind _kind;
        private readonly string _physicalPath;
        private readonly short _priority;
        private readonly string _prefix;
        private readonly LanguageSymbol _language;

        public ResourceBundle(ResourceBundleKind kind, string physicalPath, short priority, string prefix, LanguageSymbol language)
        {
            _kind = kind;
            _physicalPath = physicalPath;
            _priority = priority;
            _prefix = prefix;
            _language = language;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }


        public ResourceBundleKind Kind => _kind;

        public string? PhysicalPath => _physicalPath;

        public short Priority => _priority;

        public string Prefix => _prefix;

        public LanguageSymbol Language => _language;
    }
}
