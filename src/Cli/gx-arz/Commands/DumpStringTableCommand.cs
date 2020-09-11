using Glacie.CommandLine.IO;
using Glacie.Data.Arz;
using Glacie.Data.Arz.Infrastructure;

namespace Glacie.Cli.Arz.Commands
{
    internal sealed class DumpStringTableCommand : DatabaseCommand
    {
        private string Database { get; }

        public DumpStringTableCommand(string database)
        {
            Database = database;
        }

        public int Run()
        {
            // Use Lazy mode, because we doesn't want read records content.
            using ArzDatabase database = ReadDatabase(Database,
                CreateReaderOptions(ArzReadingMode.Lazy));

            var stringTable = database.GetContext().StringTable;
            for (var i = 0; i < stringTable.Count; i++)
            {
                Console.Out.WriteLine(stringTable[(arz_string_id)i]);
            }

            return 0;
        }
    }
}
