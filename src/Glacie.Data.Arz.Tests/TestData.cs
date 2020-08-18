namespace Glacie.Data.Arz.Tests
{
    internal static class TestData
    {
        /// <summary>
        /// Empty TQ/GD ARZ file with no-checksum calculated.
        /// Because this file has no rows, it is impossible determine complete file layout for this file.
        /// </summary>
        public static string GtdTqgd0NC => TestDataUtilities.GetPath("gtd-tqgd-0-nc.arz");

        /// <summary>
        /// Empty TQ/GD ARZ file with checksum calculated.
        /// Because this file has no rows, it is also impossible to determine complete file layout for this file.
        /// </summary>
        public static string GtdTqgd0 => TestDataUtilities.GetPath("gtd-tqgd-0.arz");

        /// <summary>
        /// Empty TQAE ARZ file.
        /// </summary>
        public static string GtdTqae0 => TestDataUtilities.GetPath("gtd-tqae-0.arz");

        /// <summary>
        /// TQAE ARZ file with 1 record:
        /// <c>@"records\xpack\game\gameengine.dbr"</c>.
        /// </summary>
        public static string GtdTqae1 => TestDataUtilities.GetPath("gtd-tqae-1.arz");

        /// <summary>
        /// TQAE ARZ file with 3 records:
        /// <c>@"records\xpack\game\gameengine.dbr"</c>,
        /// <c>@"records\creature\pc\femalepc01.dbr"</c>,
        /// <c>@"records\creature\pc\playerlevels.dbr"</c>.
        /// </summary>
        public static string GtdTqae2 => TestDataUtilities.GetPath("gtd-tqae-2.arz");

        // Exact name of record which stored in GtdTqae1.
        public static string GtdTqae1RawRecordName = @"records\xpack\game\gameengine.dbr";

        // Normalized name to use only slashes.
        public static string GtdTqae1NormalizedRecordName = @"records/xpack/game/gameengine.dbr";

        public static string[] GtdTqae2RawRecordNames = new[]
        {
            @"records\xpack\game\gameengine.dbr",
            @"records\creature\pc\femalepc01.dbr",
            @"records\creature\pc\playerlevels.dbr"
        };
    }
}
