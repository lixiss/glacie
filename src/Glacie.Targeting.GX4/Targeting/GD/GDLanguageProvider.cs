using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Glacie.Localization;
using Glacie.Localization.Builder;

namespace Glacie.Targeting.GD
{
    public sealed class GDLanguageProvider : ILanguageProvider
    {
        private static readonly List<LanguageInfo> _languages;

        static GDLanguageProvider()
        {
            _languages = new List<LanguageInfo>
            {
                GetOrAddLanguageInfo("EN", "English"),
            };
        }

        public IEnumerable<LanguageSymbol> GetLanguages()
            => _languages.Select(x => x.Symbol);

        public bool TryGetLanguageFromResourceBundleSuffix(ReadOnlySpan<char> suffix,
            [NotNullWhen(true)] out LanguageSymbol language)
        {
            if (suffix.Length != 2)
            {
                language = default;
                return false;
            }

            foreach (var x in _languages)
            {
                if (suffix.Equals(x.Code, StringComparison.OrdinalIgnoreCase))
                {
                    language = x.Symbol;
                    return true;
                }
            }

            language = default;
            return false;
        }

        private static LanguageInfo GetOrAddLanguageInfo(string code, string name)
        {
            var lib = new LanguageInfoBuilder(code, name);
            lib.TryRegister(out var languageInfo);
            if (languageInfo != null) return languageInfo;
            throw Error.Unreachable();
        }
    }
}
