using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Glacie.Localization.Builder;

namespace Glacie.Localization
{
    internal static class LanguageRegistry
    {
        public static readonly LanguageInfo Invariant;
        public static readonly LanguageInfo English;

        private static readonly List<LanguageInfo> _map = new List<LanguageInfo>();

        static LanguageRegistry()
        {
            Invariant = new LanguageInfo(0, "", "Invariant");
            English = new LanguageInfo(1, "EN", "English");
            _map.Add(Invariant);
            _map.Add(English);
            Check.That(Invariant.Symbol == LanguageSymbol.Invariant);
            Check.That(English.Symbol == LanguageSymbol.English);
        }

        public static LanguageInfo GetLanguageInfo(short ordinal)
        {
            return _map[ordinal];
        }

        public static IReadOnlyCollection<LanguageInfo> SelectAll()
            => _map;

        public static bool TryRegister(LanguageInfoBuilder builder, [NotNullWhen(true)] out LanguageInfo? languageInfo)
        {
            lock (_map)
            {
                var result = _map
                    .Where(x => builder.Code.Equals(x.Code, StringComparison.OrdinalIgnoreCase)
                        && builder.Name.Equals(x.Name, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();
                if (result != null)
                {
                    languageInfo = result;
                    return false;
                }

                var ordinal = _map.Count;
                var li = new LanguageInfo(checked((short)ordinal), builder.Code, builder.Name);
                _map.Add(li);
                languageInfo = li;
                return true;
            }
        }
    }
}
