using Glacie.Data.Resources;

namespace Glacie.Resources
{
    partial class ResourceManager
    {
        private sealed class BundleRegistration
        {
            private readonly PrefixRegistration _prefix;

            private readonly string? _language;

            private readonly string? _bundleName;

            private readonly ushort _sourceId;
            private readonly ushort _localPriority;

            private readonly ResourceBundle _bundle;

            public BundleRegistration(
                PrefixRegistration prefix,
                string? language,
                string? bundleName,
                ushort sourceId,
                ResourceBundle bundle,
                bool disposeBundle)
            {
                Check.Argument.NotNull(prefix, nameof(prefix));
                _prefix = prefix;
                _language = NormalizeLanguage(language);
                _bundleName = bundleName;
                _sourceId = sourceId;
                _localPriority = GetLocalPriorityFromLanguage(_language);
                _bundle = bundle;
                if (disposeBundle) { prefix.Manager.AddDisposable(bundle); }
            }

            public PrefixRegistration Prefix => _prefix;

            public string? Language => _language;

            public string? Name => _bundleName;

            public uint Priority => GetGlobalPriority(_sourceId, _localPriority);

            public ushort SourceId => _sourceId;

            public ushort LocalPriority => _localPriority;

            public ResourceBundle GetBundle() => _bundle;

            public bool IsLanguageNeutral => IsLanguageNeutral(_language);

            private ushort GetLocalPriorityFromLanguage(string? language)
            {
                // Neutral resources uses max local priority.
                if (IsLanguageNeutral(language)) return ushort.MaxValue;

                // Other languages uses same priority (so we currently doesn't
                // allow use multiple language-dependent resources).
                return 0;
            }
        }
    }
}
