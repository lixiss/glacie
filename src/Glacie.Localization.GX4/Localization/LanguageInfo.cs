using System.Collections.Generic;

namespace Glacie.Localization
{
    public sealed class LanguageInfo
    {
        #region Static

        public static LanguageInfo GetLanguageInfo(LanguageSymbol languageSymbol)
            => LanguageRegistry.GetLanguageInfo(languageSymbol._ordinal);

        public static IReadOnlyCollection<LanguageInfo> GetLanguages()
            => LanguageRegistry.SelectAll();

        public static LanguageInfo Invariant => LanguageRegistry.Invariant;

        public static LanguageInfo English => LanguageRegistry.English;

        #endregion

        private readonly short _ordinal;
        private readonly string _name;
        private readonly string _code;

        internal LanguageInfo(short ordinal, string code, string name)
        {
            _ordinal = ordinal;
            _code = code;
            _name = name;
        }

        public LanguageSymbol Symbol => new LanguageSymbol(_ordinal);

        public string Code => _code;

        public string Name => _name;
    }
}
