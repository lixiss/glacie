namespace Glacie.Diagnostics
{
    public enum LocationKind : byte
    {
        /// <summary>
        /// Unspecified location.
        /// </summary>
        None = 0,

        /// <summary>
        /// The location represents a position in a file.
        /// </summary>
        File = 1,

        Record = 2,

        RecordField = 3,
    }
}
