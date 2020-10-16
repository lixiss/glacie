using System.Diagnostics.CodeAnalysis;

namespace Glacie.Localization.Builder
{
    public sealed class LanguageInfoBuilder
    {
        public LanguageInfoBuilder() { }

        public LanguageInfoBuilder(string code, string name)
        {
            Code = code;
            Name = name;
        }

        public string Code { get; set; } = default!;

        public string Name { get; set; } = default!;

        public bool TryRegister([NotNullWhen(true)] out LanguageInfo? languageInfo)
        {
            return LanguageRegistry.TryRegister(this, out languageInfo);
        }

        public LanguageInfo Register()
        {
            if (TryRegister(out var result)) return result;
            throw Error.InvalidOperation($"Can't register {nameof(LanguageInfo)}.");
        }
    }
}
