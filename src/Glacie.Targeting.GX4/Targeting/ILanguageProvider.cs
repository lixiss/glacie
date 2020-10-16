using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Glacie.Localization;

namespace Glacie.Targeting
{
    // TODO: Doesn't like how it named.
    public interface ILanguageProvider
    {
        IEnumerable<LanguageSymbol> GetLanguages();

        /// <summary>
        /// When returns <see langword="true"/> then suffix (like EN, BR) is
        /// recognized and associated language symbol will be used:
        /// It might return <see cref="LanguageSymbol.Invariant"/> what means
        /// suffix is recognized, and there is not a language suffix.
        /// When returns <see langword="false"/> then suffix is not recognized
        /// and error should emitted.
        /// </summary>
        bool TryGetLanguageFromResourceBundleSuffix(ReadOnlySpan<char> suffix,
            [NotNullWhen(true)] out LanguageSymbol language);
    }
}
