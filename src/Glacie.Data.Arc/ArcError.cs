namespace Glacie.Data.Arc
{
    /// <summary>
    /// ArcException factory.
    /// </summary>
    internal static class ArcError
    {
        public static ArcException EntryNotFound(string entryName)
        {
            return new ArcException("EntryNotFound", "Entry \"{0}\" not found.".FormatWith(entryName));
        }

        public static ArcException EntryAlreadyExist(string entryName)
        {
            return new ArcException("EntryAlreadyExist", "Entry \"{0}\" already exist.".FormatWith(entryName));
        }
    }
}
