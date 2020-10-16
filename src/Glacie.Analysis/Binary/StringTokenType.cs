namespace Glacie.Analysis.Binary
{
    public enum StringTokenType
    {
        EndOfStream = 0,

        /// <summary>
        /// Raw ASCII-like data.
        /// </summary>
        RawAsciiString,

        /// <summary>
        /// Int32 Little Endinan Length Encoded.
        /// </summary>
        Int32LeleAsciiString,

    }
}
