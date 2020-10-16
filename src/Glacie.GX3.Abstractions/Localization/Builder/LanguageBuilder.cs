namespace Glacie.Localization.Builder
{
    public struct LanguageBuilder
    {
        private readonly string _code;
        private readonly string _name;

        public LanguageBuilder(string code, string name)
        {
            Check.Argument.NotNullNorEmpty(code, nameof(code));
            Check.Argument.NotNullNorEmpty(name, nameof(name));

            _code = code;
            _name = name;
        }

        public Language Build()
        {
            Check.That(!string.IsNullOrEmpty(_code));
            Check.That(!string.IsNullOrEmpty(_name));

            return LanguageRegistry.GetOrAdd(_code, _name);
        }
    }
}
