using System;

namespace Glacie.Data.Arz
{
    [Flags]
    public enum ArzRecordOptions
    {
        None = 0,

        /// <summary>
        /// Don't create field map.
        /// </summary>
        NoFieldMap = 1 << 0,
    }
}
