using System;
using System.Collections.Generic;
using System.Linq;

namespace Glacie.Localization
{
    internal static class LanguageRegistry
    {
        internal static readonly Language Invariant;

        private static readonly List<Language> _map = new List<Language>();

        static LanguageRegistry()
        {
            Invariant = Add("", "Invariant");
            Check.That(Invariant.Symbol == LanguageSymbol.Invariant);

            var englishLanguage = Add("EN", "English");
            Check.That(englishLanguage.Symbol == LanguageSymbol.English);

            if (true)
            {
                // TODO: (Medium) (Localization) (Engine) Move this into engine:
                // engine should able to map archive name to language,
                // and as result be able to provide supported language list
                // (if it fixed), or may be able to perform language registrations.

                // TQAE
                Add("BR", "Portuguese (Brazilian)");
                Add("CH", "Chinese");
                Add("CZ", "Czech");
                Add("DE", "German");
                //Add("EN", "English");
                Add("ES", "Spanish");
                Add("FR", "French");
                Add("IT", "Italian");
                Add("JA", "Japanese");
                Add("KO", "Korean");
                Add("PL", "Polish");
                Add("RU", "Russian");
                Add("UK", "Ukrainian");

                if (false)
                {
                    // GD
                    GetOrAdd("", "Chinese");
                    GetOrAdd("", "Chinese Alt");
                    GetOrAdd("", "Chinese Modified");
                    GetOrAdd("", "Czech");
                    GetOrAdd("", "French");
                    GetOrAdd("", "German");
                    GetOrAdd("", "German + E");
                    GetOrAdd("", "Italian");
                    GetOrAdd("", "Japanese");
                    GetOrAdd("", "Japanese + E");
                    GetOrAdd("", "Korean");
                    GetOrAdd("", "Polish");
                    GetOrAdd("", "Portuguese");
                    GetOrAdd("", "Russian");
                    GetOrAdd("", "Spanish");
                    GetOrAdd("", "Vietnamese");
                }
            }
        }

        internal static IReadOnlyCollection<Language> SelectAll()
        {
            return _map;
        }

        internal static Language GetLanguage(int ordinal)
        {
            return _map[ordinal];
        }

        internal static Language GetOrAdd(string code, string name)
        {
            lock (_map)
            {
                var result = _map
                    .Where(x => code.Equals(x.Code, StringComparison.OrdinalIgnoreCase)
                        && name.Equals(x.Name, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();
                if (result != null) return result;
                return Add(code, name);
            }
        }

        private static Language Add(string code, string name)
        {
            var ordinal = _map.Count;
            var result = new Language(checked((short)ordinal), code, name);
            _map.Add(result);
            return result;
        }
    }
}
