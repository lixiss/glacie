namespace Glacie.GX1.Abstractions.Localization
{
    // TODO: Note, that engine uses different language code, than ISO codes,
    // so this should be correctly reflected, so LanguageInfo can't be
    // freely interchanged. So, let universal language info, and let engine type
    // handle rest of things.

    // Implement https://en.wikipedia.org/wiki/ISO_639-1 codes.
    // [ISO639-1] International Organization for Standardization, "ISO
    //                639-1:2002.  Codes for the representation of names
    //                of languages -- Part 1: Alpha-2 code", July 2002.
    //
    // https://tools.ietf.org/html/rfc5646
    //
    // Engine may provide specific information,
    // for example EN will be encoded by UTF8, while many other with UTF16 in GD,
    // but in UTF8 in TQAE (however need check if TQAE support UTF16).
    // Some languages may be better (smaller result) to encoded in UTF16, while
    // some in UTF8, or even in UTF32.

    public sealed class LanguageInfo
    {
        public LanguageSymbol Symbol => throw Error.NotImplemented();

        /// <summary>
        /// Returns ISO 639-1:2002 alpha-2 language code.
        /// </summary>
        /// <remarks>
        /// This code different from language code used by engine.
        /// </remarks>
        public string IsoAlpha2Code => throw Error.NotImplemented();
    }
}
