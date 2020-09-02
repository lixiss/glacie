namespace Glacie.Data.Arc
{
    public enum ArcArchiveMode
    {
        /// <summary>
        /// Only reading archive entries is permitted.
        /// </summary>
        Read = 0,

        /// <summary>
        /// Only creating new archive entries is permitted.
        /// </summary>
        Create = 1,

        /// <summary>
        /// Both read and write operations are permitted for archive entries.
        /// </summary>
        Update = 2,
    }
}
