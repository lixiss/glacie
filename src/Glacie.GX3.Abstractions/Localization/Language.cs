using System.Collections.Generic;

namespace Glacie.Localization
{
    public sealed class Language
    {
        public static Language GetLanguage(LanguageSymbol languageSymbol)
            => LanguageRegistry.GetLanguage(languageSymbol._ordinal);

        public static IReadOnlyCollection<Language> GetLanguages()
            => LanguageRegistry.SelectAll();

        public static Language Invariant => LanguageRegistry.Invariant;


        private readonly short _ordinal;
        private readonly string _code;
        private readonly string _name;

        internal Language(short ordinal, string code, string name)
        {
            _ordinal = ordinal;
            _code = code;
            _name = name;
        }

        public LanguageSymbol Symbol => new LanguageSymbol(_ordinal);

        /// <summary>
        /// Returns alpha-2 language code.
        /// </summary>
        /// <remarks>
        /// This value is engine-specific, and doesn't match to ISO 639-1:2002 language codes.
        /// </remarks>
        public string Code => _code;

        public string Name => _name;
    }
}
