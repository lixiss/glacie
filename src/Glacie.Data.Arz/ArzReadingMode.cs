namespace Glacie.Data.Arz
{
    public enum ArzReadingMode
    {
        /// <summary>
        /// Doesn't load field data immediately.
        /// Data will be loaded lazily when needed.
        /// </summary>
        Lazy = 0,

        /// <summary>
        /// Load raw field data.
        /// </summary>
        Raw = 1,

        /// <summary>
        /// Load raw field data and decompress them immediately.
        /// </summary>
        Full = 2,
    }
}
