namespace Glacie.Data.Arc
{
    internal static class TestData
    {
        public static string DataEmptyFileBin => TestDataUtilities.GetPath("data/empty-file.bin");
        public static string DataSmallFileBin => TestDataUtilities.GetPath("data/small-file.bin");
        public static string DataGDArchiveToolHelpBin => TestDataUtilities.GetPath("data/GD-ArchiveTool-Help.bin");
        public static string DataTQArchiveToolHelpBin => TestDataUtilities.GetPath("data/TQ-ArchiveTool-Help.bin");
        public static string DataTokensBin => TestDataUtilities.GetPath("data/tokens.bin");

        /// <summary>
        /// 0: Empty Archive
        /// </summary>
        public static string Gd0Arc => TestDataUtilities.GetPath("gd-0.arc");
        public static string Tq0Arc => TestDataUtilities.GetPath("tq-0.arc");

        /// <summary>
        /// 1: Single Empty File
        /// <c>"data/empty-file.bin"</c>
        /// </summary>
        public static string Gd1Arc => TestDataUtilities.GetPath("gd-1.arc");
        public static string Tq1Arc => TestDataUtilities.GetPath("tq-1.arc");

        /// <summary>
        /// 2: Single Small File (Uncompressed)
        /// <c>"data/small-file.bin"</c>
        /// </summary>
        public static string Gd2Arc => TestDataUtilities.GetPath("gd-2.arc");
        public static string Tq2Arc => TestDataUtilities.GetPath("tq-2.arc");

        /// <summary>
        /// 3: Two Medium Files (Compressed)
        /// <c>"data/tq-archivetool-help.bin", "data/gd-archivetool-help.bin"</c>
        /// </summary>
        public static string Gd3Arc => TestDataUtilities.GetPath("gd-3.arc");
        public static string Tq3Arc => TestDataUtilities.GetPath("tq-3.arc");

        /// <summary>
        /// 4: Many Files With Removals
        /// <c>"data/tq-archivetool-help.bin"</c>
        /// Holds removed entries.
        /// </summary>
        public static string Gd4Arc => TestDataUtilities.GetPath("gd-4.arc");
        public static string Tq4Arc => TestDataUtilities.GetPath("tq-4.arc");

        /// <summary>
        /// 5: File With MaybeStore Chunk
        /// </summary>
        // public static string Gd5Arc => TestDataUtilities.GetPath("gd-5.arc");
        public static string Tq5Arc => TestDataUtilities.GetPath("tq-5.arc");

    }
}
